using Microsoft.EntityFrameworkCore;
using PeP.Data;
using PeP.Models;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

namespace PeP.Services
{
    public interface IOpenAIService
    {
        Task<List<Question>> GenerateQuestionsAsync(string prompt, int count, string difficulty, string questionType, decimal pointsPerQuestion);
    }

    public class OpenAIService : IOpenAIService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OpenAIService> _logger;

        public OpenAIService(ApplicationDbContext context, ILogger<OpenAIService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Question>> GenerateQuestionsAsync(string prompt, int count, string difficulty, string questionType, decimal pointsPerQuestion)
        {
            if (string.IsNullOrWhiteSpace(prompt)) throw new ArgumentException("Prompt is required.", nameof(prompt));
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

            var apiKeySetting = await _context.PlatformSettings.FirstOrDefaultAsync(s => s.Key == "OpenAIApiKey");
            var apiKey = apiKeySetting?.Value;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is not configured in Platform Settings.");
            }

            var modelSetting = await _context.PlatformSettings.FirstOrDefaultAsync(s => s.Key == "OpenAIModel");
            var model = string.IsNullOrWhiteSpace(modelSetting?.Value) ? "gpt-4o-mini" : modelSetting!.Value;

            var allQuestions = new List<Question>();
            const int batchSize = 10; // Generate in smaller batches
            int remaining = count;
            int attempt = 0;
            const int maxAttempts = 5;

            try
            {
                var client = new OpenAIClient(apiKey);

                while (remaining > 0 && attempt < maxAttempts)
                {
                    attempt++;
                    var requestCount = Math.Min(batchSize, remaining);

                    _logger.LogInformation("Generating batch {Attempt}/{MaxAttempts}: requesting {Count} questions", attempt, maxAttempts, requestCount);

                    var systemMessage = $"You are an expert exam question generator. Generate exactly {requestCount} {difficulty.ToLower()} level programming questions. " +
                        $"Return ONLY strict JSON with this exact schema: " +
                        $"[ {{ \"questionText\": string, \"choices\": [ {{ \"text\": string, \"isCorrect\": boolean }} ] }} ]" +
                        $". No code fences, no explanations, no markdown. Topic: {prompt}. Difficulty: {difficulty}. Type: {questionType}. " +
                        $"Each question must have exactly 4 choices with at least one correct answer.";

                    var messages = new List<ChatMessage>
                    {
                        new SystemChatMessage(systemMessage),
                        new UserChatMessage($"Generate exactly {requestCount} questions as valid JSON array now.")
                    };

                    var chatCompletion = await client.GetChatClient(model).CompleteChatAsync(messages);
                    var raw = chatCompletion.Value.Content[0].Text?.Trim();
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        _logger.LogWarning("OpenAI returned empty content on attempt {Attempt}", attempt);
                        continue;
                    }

                    // Remove common code fences if present
                    if (raw.StartsWith("```"))
                    {
                        var idx = raw.IndexOf('\n');
                        if (idx > -1) raw = raw.Substring(idx + 1);
                        if (raw.EndsWith("```")) raw = raw.Substring(0, raw.Length - 3);
                        raw = raw.Trim();
                    }

                    var batchQuestions = new List<Question>();
                    try
                    {
                        using var doc = JsonDocument.Parse(raw);
                        JsonElement arr;
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            arr = doc.RootElement;
                        }
                        else if (doc.RootElement.TryGetProperty("questions", out var qProp) && qProp.ValueKind == JsonValueKind.Array)
                        {
                            arr = qProp;
                        }
                        else
                        {
                            _logger.LogWarning("JSON does not contain a questions array on attempt {Attempt}", attempt);
                            continue;
                        }

                        foreach (var item in arr.EnumerateArray())
                        {
                            if (!item.TryGetProperty("questionText", out var qt) || qt.ValueKind != JsonValueKind.String) continue;
                            if (!item.TryGetProperty("choices", out var ch) || ch.ValueKind != JsonValueKind.Array) continue;

                            var q = new Question
                            {
                                QuestionText = qt.GetString() ?? string.Empty,
                                Points = pointsPerQuestion,
                                Order = allQuestions.Count + 1,
                                QuestionType = questionType == "MultipleAnswer" ? QuestionType.MultipleAnswer : QuestionType.MultipleChoice,
                                Choices = new List<QuestionChoice>()
                            };

                            int order = 0;
                            foreach (var c in ch.EnumerateArray())
                            {
                                var text = c.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() ?? string.Empty : string.Empty;
                                var isCorrect = c.TryGetProperty("isCorrect", out var ic) && ic.ValueKind == JsonValueKind.True;
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    q.Choices.Add(new QuestionChoice { ChoiceText = text, IsCorrect = isCorrect, Order = ++order });
                                }
                            }

                            // Only add if has at least 2 choices and one correct
                            if (q.Choices.Count >= 2 && q.Choices.Any(x => x.IsCorrect))
                            {
                                batchQuestions.Add(q);
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogError(parseEx, "Failed to parse OpenAI JSON on attempt {Attempt}: {Raw}", attempt, raw);
                        continue;
                    }

                    if (batchQuestions.Count > 0)
                    {
                        allQuestions.AddRange(batchQuestions);
                        remaining -= batchQuestions.Count;
                        _logger.LogInformation("Successfully generated {Count} questions, {Remaining} remaining", batchQuestions.Count, remaining);
                    }
                    else
                    {
                        _logger.LogWarning("Batch {Attempt} produced no valid questions", attempt);
                    }

                    // Small delay between batches to avoid rate limits
                    if (remaining > 0)
                    {
                        await Task.Delay(500);
                    }
                }

                if (allQuestions.Count == 0)
                {
                    throw new InvalidOperationException("AI failed to generate any valid questions after multiple attempts.");
                }

                if (allQuestions.Count < count)
                {
                    _logger.LogWarning("Only generated {Generated} out of {Requested} questions", allQuestions.Count, count);
                    throw new InvalidOperationException($"AI only generated {allQuestions.Count} out of {count} requested questions. Try reducing the count or simplifying the prompt.");
                }

                // Re-order the questions sequentially
                for (int i = 0; i < allQuestions.Count; i++)
                {
                    allQuestions[i].Order = i + 1;
                }

                return allQuestions.Take(count).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI generation failed after {Attempts} attempts. Generated {Count} out of {Requested}", attempt, allQuestions.Count, count);
                throw;
            }
        }

        private class QuestionData
        {
            public string QuestionText { get; set; } = string.Empty;
            public List<ChoiceData> Choices { get; set; } = new();
        }

        private class ChoiceData
        {
            public string Text { get; set; } = string.Empty;
            public bool IsCorrect { get; set; }
        }
    }
}