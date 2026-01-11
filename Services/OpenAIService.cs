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
        Task<GeneratedProgrammingExam> GenerateProgrammingTasksAsync(string topic, ProgrammingLanguage language, int taskCount, string difficulty);
        Task<AICodeEvaluation> EvaluateCodeAsync(ProgrammingLanguage language, string taskTitle, string taskInstructions, decimal maxPoints, Dictionary<string, string> studentFiles, string? targetFiles);
        Task<List<AICodeEvaluation>> EvaluateAllTasksAsync(ProgrammingExamAttempt attempt);
    }

    // AI Code Evaluation Result
    public class AICodeEvaluation
    {
        public int TaskId { get; set; }
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public decimal CodeQualityScore { get; set; } // 0-100
        public decimal CorrectnessScore { get; set; } // 0-100
        public decimal EfficiencyScore { get; set; } // 0-100
        public decimal CompletionPercentage { get; set; } // 0-100
        public string Feedback { get; set; } = string.Empty;
        public string Strengths { get; set; } = string.Empty;
        public string AreasForImprovement { get; set; } = string.Empty;
        public string SolutionSuggestion { get; set; } = string.Empty;
        public List<CodeSnippetFeedback> CodeSnippets { get; set; } = new();
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class CodeSnippetFeedback
    {
        public string FileName { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string Type { get; set; } = "info"; // info, warning, error, success
        public int? LineNumber { get; set; }
    }

    public class GeneratedProgrammingExam
    {
        public string ProjectName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<GeneratedProjectFile> StarterFiles { get; set; } = new();
        public List<GeneratedTask> Tasks { get; set; } = new();
    }

    public class GeneratedProjectFile
    {
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsEntryPoint { get; set; }
        public bool IsReadOnly { get; set; }
    }

    public class GeneratedTask
    {
        public int Order { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public decimal Points { get; set; }
        public string? Hint { get; set; }
        public string? TargetFiles { get; set; }
        public List<GeneratedTestCase> TestCases { get; set; } = new();
    }

    public class GeneratedTestCase
    {
        public string Name { get; set; } = string.Empty;
        public string? Input { get; set; }
        public string? ExpectedOutput { get; set; }
        public bool IsHidden { get; set; }
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
            var model = string.IsNullOrWhiteSpace(modelSetting?.Value) ? "gpt-5.2" : modelSetting!.Value;

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

        public async Task<GeneratedProgrammingExam> GenerateProgrammingTasksAsync(string topic, ProgrammingLanguage language, int taskCount, string difficulty)
        {
            if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentException("Topic is required.", nameof(topic));
            if (taskCount <= 0 || taskCount > 10) throw new ArgumentOutOfRangeException(nameof(taskCount), "Task count must be between 1 and 10");

            var apiKeySetting = await _context.PlatformSettings.FirstOrDefaultAsync(s => s.Key == "OpenAIApiKey");
            var apiKey = apiKeySetting?.Value;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is not configured in Platform Settings.");
            }

            var modelSetting = await _context.PlatformSettings.FirstOrDefaultAsync(s => s.Key == "OpenAIModel");
            var model = string.IsNullOrWhiteSpace(modelSetting?.Value) ? "gpt-4o-mini" : modelSetting!.Value;

            var languageName = GetLanguageName(language);
            var extension = GetFileExtension(language);

            var systemPrompt = $@"You are an expert programming exam designer. Create a programming exam project for students.
Language: {languageName}
Topic: {topic}
Difficulty: {difficulty}
Number of tasks: {taskCount}

CRITICAL: Return ONLY valid JSON. No markdown, no code fences, no comments, no trailing commas, no semicolons.
Escape all special characters in strings properly (use \n for newlines, \"" for quotes).

Schema:
{{
  ""projectName"": ""PascalCaseNoSpaces"",
  ""description"": ""exam overview"",
  ""starterFiles"": [
    {{
      ""filePath"": ""src/Main{extension}"",
      ""content"": ""starter code with TODO comments (escape newlines as \\n)"",
      ""isEntryPoint"": true,
      ""isReadOnly"": false
    }},
    {{
      ""filePath"": ""data/input.csv"",
      ""content"": ""id,name,value\\n1,Item1,100\\n2,Item2,200"",
      ""isEntryPoint"": false,
      ""isReadOnly"": true
    }}
  ],
  ""tasks"": [
    {{
      ""order"": 1,
      ""title"": ""Task Title"",
      ""instructions"": ""detailed instructions"",
      ""points"": 25,
      ""hint"": ""optional hint or null"",
      ""targetFiles"": ""Main{extension}"",
      ""testCases"": [
        {{
          ""name"": ""Test 1"",
          ""input"": ""test input or null"",
          ""expectedOutput"": ""expected output"",
          ""isHidden"": false
        }}
      ]
    }}
  ]
}}

Rules:
- Starter code should compile but have TODO sections
- Include 2-3 test cases per task (mix visible/hidden)
- Points should total ~100
- Use proper {languageName} naming conventions
- Keep code content short and escape all special characters
- CREATE DATA FILES when the topic involves data processing:
  * For file I/O topics: include CSV, TXT, or JSON data files in starterFiles
  * Data files should have realistic sample data (5-20 rows for CSVs)
  * Mark data files as isReadOnly: true so students don't modify them
  * Use paths like ""data/input.csv"", ""data/students.txt"", ""data/config.json""
  * Tasks should reference these data files in their instructions
  * Example data file types: CSV (comma-separated), TXT (plain text), JSON (structured data)
- If the topic is NOT about file I/O, you can skip data files";

            int maxRetries = 3;
            int retryCount = 0;
            Exception? lastException = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    retryCount++;
                    _logger.LogInformation("Generating programming exam, attempt {Attempt}/{MaxRetries}", retryCount, maxRetries);

                    var client = new OpenAIClient(apiKey);
                    var messages = new List<ChatMessage>
                    {
                        new SystemChatMessage(systemPrompt),
                        new UserChatMessage($"Generate a {difficulty} level {languageName} programming exam about \"{topic}\" with exactly {taskCount} tasks. Return ONLY valid JSON, no markdown.")
                    };

                    var chatCompletion = await client.GetChatClient(model).CompleteChatAsync(messages);
                    var raw = chatCompletion.Value.Content[0].Text?.Trim();

                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        throw new InvalidOperationException("AI returned empty response");
                    }

                    // Clean up the response
                    raw = SanitizeJsonResponse(raw);
                    
                    _logger.LogDebug("Sanitized JSON response (first 500 chars): {Json}", raw.Length > 500 ? raw.Substring(0, 500) : raw);

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer.Deserialize<GeneratedProgrammingExam>(raw, options);

                    if (result == null || string.IsNullOrWhiteSpace(result.ProjectName))
                    {
                        throw new InvalidOperationException("Failed to parse AI response into valid exam structure");
                    }

                    // Validate and fix ordering
                    for (int i = 0; i < result.Tasks.Count; i++)
                    {
                        result.Tasks[i].Order = i + 1;
                    }

                    // Post-process content to ensure proper newlines
                    // Sometimes the AI outputs literal \n instead of actual newlines
                    foreach (var file in result.StarterFiles)
                    {
                        if (!string.IsNullOrEmpty(file.Content))
                        {
                            // Replace literal \n with actual newlines (only if not already a real newline)
                            file.Content = file.Content.Replace("\\n", "\n");
                            // Normalize Windows line endings to Unix
                            file.Content = file.Content.Replace("\r\n", "\n");
                        }
                    }

                    // Ensure we have the requested number of tasks
                    if (result.Tasks.Count < taskCount)
                    {
                        _logger.LogWarning("AI generated {Generated} tasks instead of {Requested}", result.Tasks.Count, taskCount);
                    }

                    _logger.LogInformation("Successfully generated programming exam: {ProjectName} with {TaskCount} tasks", 
                        result.ProjectName, result.Tasks.Count);

                    return result;
                }
                catch (JsonException ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "JSON parsing failed on attempt {Attempt}, will retry", retryCount);
                    
                    if (retryCount < maxRetries)
                    {
                        await Task.Delay(500); // Brief delay before retry
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogError(ex, "Failed to generate programming exam on attempt {Attempt}", retryCount);
                    throw;
                }
            }

            _logger.LogError(lastException, "Failed to parse AI response after {MaxRetries} attempts", maxRetries);
            throw new InvalidOperationException($"AI failed to generate valid JSON after {maxRetries} attempts. Please try again with a simpler topic.", lastException);
        }

        private static string SanitizeJsonResponse(string raw)
        {
            // Remove markdown code fences
            if (raw.StartsWith("```"))
            {
                var idx = raw.IndexOf('\n');
                if (idx > -1) raw = raw.Substring(idx + 1);
            }
            if (raw.EndsWith("```"))
            {
                raw = raw.Substring(0, raw.Length - 3);
            }
            raw = raw.Trim();

            // Find JSON boundaries
            var jsonStart = raw.IndexOf('{');
            var jsonEnd = raw.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                raw = raw.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            // Fix common JSON issues from AI responses
            // Replace semicolons used instead of commas (very common AI mistake)
            // Pattern: value; "nextKey" -> value, "nextKey"
            raw = System.Text.RegularExpressions.Regex.Replace(raw, @";\s*""", ", \"");
            // Pattern: value; } or value; ] -> value } or value ]
            raw = System.Text.RegularExpressions.Regex.Replace(raw, @";\s*([}\]])", "$1");
            // Pattern: }; -> },  or ]; -> ],
            raw = System.Text.RegularExpressions.Regex.Replace(raw, @"([}\]]);\s*", "$1, ");
            // Clean up double commas that might result
            raw = System.Text.RegularExpressions.Regex.Replace(raw, @",\s*,", ",");
            
            // Remove trailing commas before closing brackets (invalid JSON)
            raw = System.Text.RegularExpressions.Regex.Replace(raw, @",\s*([}\]])", "$1");

            return raw;
        }

        private static string GetLanguageName(ProgrammingLanguage language) => language switch
        {
            ProgrammingLanguage.Java => "Java",
            ProgrammingLanguage.Python => "Python",
            ProgrammingLanguage.CSharp => "C#",
            ProgrammingLanguage.CPlusPlus => "C++",
            ProgrammingLanguage.JavaScript => "JavaScript",
            ProgrammingLanguage.TypeScript => "TypeScript",
            ProgrammingLanguage.C => "C",
            _ => "Java"
        };

        private static string GetFileExtension(ProgrammingLanguage language) => language switch
        {
            ProgrammingLanguage.Java => ".java",
            ProgrammingLanguage.Python => ".py",
            ProgrammingLanguage.CSharp => ".cs",
            ProgrammingLanguage.CPlusPlus => ".cpp",
            ProgrammingLanguage.JavaScript => ".js",
            ProgrammingLanguage.TypeScript => ".ts",
            ProgrammingLanguage.C => ".c",
            _ => ".java"
        };

        #region AI Code Evaluation

        public async Task<AICodeEvaluation> EvaluateCodeAsync(
            ProgrammingLanguage language, 
            string taskTitle, 
            string taskInstructions, 
            decimal maxPoints, 
            Dictionary<string, string> studentFiles,
            string? targetFiles)
        {
            try
            {
                var apiKeySetting = await _context.PlatformSettings.FirstOrDefaultAsync(s => s.Key == "OpenAIApiKey");
                var apiKey = apiKeySetting?.Value;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return new AICodeEvaluation
                    {
                        Success = false,
                        ErrorMessage = "OpenAI API key is not configured in Platform Settings."
                    };
                }

                var modelSetting = await _context.PlatformSettings.FirstOrDefaultAsync(s => s.Key == "OpenAIModel");
                var model = string.IsNullOrWhiteSpace(modelSetting?.Value) ? "gpt-4o-mini" : modelSetting!.Value;

                var languageName = GetLanguageName(language);

                // Build the files content string
                var filesContent = new System.Text.StringBuilder();
                
                // Filter to target files if specified
                var filesToEvaluate = studentFiles;
                if (!string.IsNullOrWhiteSpace(targetFiles))
                {
                    var targetFileList = targetFiles.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(f => f.Trim())
                        .ToList();
                    filesToEvaluate = studentFiles
                        .Where(f => targetFileList.Any(t => f.Key.EndsWith(t, StringComparison.OrdinalIgnoreCase) || f.Key.Contains(t, StringComparison.OrdinalIgnoreCase)))
                        .ToDictionary(f => f.Key, f => f.Value);
                    
                    // If no matches, fall back to all files
                    if (!filesToEvaluate.Any())
                        filesToEvaluate = studentFiles;
                }

                foreach (var file in filesToEvaluate)
                {
                    filesContent.AppendLine($"=== File: {file.Key} ===");
                    filesContent.AppendLine(file.Value);
                    filesContent.AppendLine();
                }

                var systemPrompt = $@"You are a fair but thorough {languageName} code reviewer and programming instructor. 
Your task is to evaluate student code based on the given task instructions.

GRADING PRINCIPLES:
1. EMPTY FUNCTIONS/METHODS = 0 POINTS. If a function body is empty, contains only comments, or just returns null/default without logic, it gets zero.
2. PLACEHOLDER CODE = 0 POINTS. Code like 'throw new NotImplementedException()', 'pass', 'TODO' = zero points.
3. Give partial credit for partial solutions that show understanding and effort.
4. Code that attempts the task but has bugs should get proportional credit based on how close it is to working.
5. Consider the student's approach and logic, not just whether it produces perfect output.

SCORING GUIDELINES:
- 0%: Empty, placeholder, or code that makes no attempt at the task
- 10-30%: Code attempts the task but has fundamental misunderstandings or major bugs
- 31-50%: Partially working code with significant issues but shows understanding
- 51-70%: Working code that addresses most requirements but has bugs or missing features
- 71-85%: Good working code with minor issues or small missing features
- 86-100%: Excellent, fully working code that meets all requirements

EVALUATION CRITERIA:
1. Correctness (40%) - Does the code implement what the task asks? Does the logic make sense?
2. Code Quality (25%) - Is the code well-structured, readable, properly named?
3. Efficiency (15%) - Is the solution reasonably efficient?
4. Completion (20%) - How much of the requirements were addressed?

Be fair and constructive. Award partial credit where the student shows understanding.

Return your evaluation as JSON with this exact structure:
{{
    ""score"": <number between 0 and {maxPoints}>,
    ""codeQualityScore"": <number 0-100>,
    ""correctnessScore"": <number 0-100>,
    ""efficiencyScore"": <number 0-100>,
    ""completionPercentage"": <number 0-100>,
    ""feedback"": ""<detailed constructive feedback about the code>"",
    ""strengths"": ""<what the student did well>"",
    ""areasForImprovement"": ""<specific suggestions for improvement>"",
    ""solutionSuggestion"": ""<brief explanation of how this task could be improved or solved better>"",
    ""codeSnippets"": [
        {{
            ""fileName"": ""<file where snippet is from>"",
            ""code"": ""<relevant code snippet, max 10 lines>"",
            ""comment"": ""<feedback about this specific code>"",
            ""type"": ""success|warning|error|info"",
            ""lineNumber"": <optional line number>
        }}
    ]
}}

Include 2-4 code snippets highlighting noteworthy parts of the code (good or bad).
IMPORTANT: Empty functions with no implementation = 0 points. But give fair partial credit for genuine attempts.";

                var userPrompt = $@"## Task: {taskTitle}

## Instructions:
{taskInstructions}

## Maximum Points: {maxPoints}

## Student's Code:
{filesContent}

GRADING REMINDER: 
- Empty functions or placeholder code only → 0 points
- Give partial credit for genuine attempts that show understanding
- Be fair but maintain standards

Please evaluate this code and provide your assessment as JSON.";

                var client = new OpenAIClient(apiKey);
                var chatClient = client.GetChatClient(model);

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                };

                var response = await chatClient.CompleteChatAsync(messages);
                var raw = response.Value.Content[0].Text;
                raw = SanitizeJsonResponse(raw);

                _logger.LogInformation("AI Evaluation response: {Response}", raw);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AIEvaluationResponse>(raw, options);

                if (result == null)
                {
                    return new AICodeEvaluation
                    {
                        Success = false,
                        ErrorMessage = "Failed to parse AI evaluation response"
                    };
                }

                return new AICodeEvaluation
                {
                    Score = Math.Min(Math.Max(result.Score, 0), maxPoints),
                    MaxScore = maxPoints,
                    CodeQualityScore = Math.Min(Math.Max(result.CodeQualityScore, 0), 100),
                    CorrectnessScore = Math.Min(Math.Max(result.CorrectnessScore, 0), 100),
                    EfficiencyScore = Math.Min(Math.Max(result.EfficiencyScore, 0), 100),
                    CompletionPercentage = Math.Min(Math.Max(result.CompletionPercentage, 0), 100),
                    Feedback = result.Feedback ?? string.Empty,
                    Strengths = result.Strengths ?? string.Empty,
                    AreasForImprovement = result.AreasForImprovement ?? string.Empty,
                    SolutionSuggestion = result.SolutionSuggestion ?? string.Empty,
                    CodeSnippets = result.CodeSnippets?.Select(s => new CodeSnippetFeedback
                    {
                        FileName = s.FileName ?? string.Empty,
                        Code = s.Code ?? string.Empty,
                        Comment = s.Comment ?? string.Empty,
                        Type = s.Type ?? "info",
                        LineNumber = s.LineNumber
                    }).ToList() ?? new List<CodeSnippetFeedback>(),
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AI code evaluation");
                return new AICodeEvaluation
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<List<AICodeEvaluation>> EvaluateAllTasksAsync(ProgrammingExamAttempt attempt)
        {
            var results = new List<AICodeEvaluation>();

            if (attempt?.ProgrammingExam?.Tasks == null || !attempt.StudentFiles.Any())
            {
                return results;
            }

            // Build student files dictionary
            var studentFiles = attempt.StudentFiles.ToDictionary(f => f.FilePath, f => f.Content);

            foreach (var task in attempt.ProgrammingExam.Tasks.OrderBy(t => t.Order))
            {
                var evaluation = await EvaluateCodeAsync(
                    attempt.ProgrammingExam.Language,
                    task.Title,
                    task.Instructions,
                    task.Points,
                    studentFiles,
                    task.TargetFiles
                );
                
                evaluation.TaskId = task.Id;
                results.Add(evaluation);
            }

            return results;
        }

        private class AIEvaluationResponse
        {
            public decimal Score { get; set; }
            public decimal CodeQualityScore { get; set; }
            public decimal CorrectnessScore { get; set; }
            public decimal EfficiencyScore { get; set; }
            public decimal CompletionPercentage { get; set; }
            public string? Feedback { get; set; }
            public string? Strengths { get; set; }
            public string? AreasForImprovement { get; set; }
            public string? SolutionSuggestion { get; set; }
            public List<CodeSnippetResponse>? CodeSnippets { get; set; }
        }

        private class CodeSnippetResponse
        {
            public string? FileName { get; set; }
            public string? Code { get; set; }
            public string? Comment { get; set; }
            public string? Type { get; set; }
            public int? LineNumber { get; set; }
        }

        #endregion
    }
}