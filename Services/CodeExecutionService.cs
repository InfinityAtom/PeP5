using PeP.Models;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PeP.Services
{
    public interface ICodeExecutionService
    {
        Task<CodeExecutionResult> ExecuteCodeAsync(
            ProgrammingLanguage language,
            Dictionary<string, string> files,
            string? stdin = null,
            int timeoutSeconds = 30);

        Task<TestCaseExecutionResult> RunTestCaseAsync(
            ProgrammingLanguage language,
            Dictionary<string, string> files,
            TestCase testCase);

        Task<List<TestCaseExecutionResult>> RunAllTestCasesAsync(
            ProgrammingLanguage language,
            Dictionary<string, string> files,
            List<TestCase> testCases);
    }

    public class CodeExecutionResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string ErrorOutput { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public long ExecutionTimeMs { get; set; }
        public bool TimedOut { get; set; }
        public string? CompilationError { get; set; }
    }

    public class TestCaseExecutionResult
    {
        public int TestCaseId { get; set; }
        public string TestCaseName { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string? ExpectedOutput { get; set; }
        public string? ActualOutput { get; set; }
        public string? ErrorMessage { get; set; }
        public long ExecutionTimeMs { get; set; }
        public bool TimedOut { get; set; }
    }

    #region Piston API Models
    public class PistonExecuteRequest
    {
        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("files")]
        public List<PistonFile> Files { get; set; } = new();

        [JsonPropertyName("stdin")]
        public string? Stdin { get; set; }

        [JsonPropertyName("args")]
        public List<string>? Args { get; set; }

        [JsonPropertyName("compile_timeout")]
        public int CompileTimeout { get; set; } = 10000;

        [JsonPropertyName("run_timeout")]
        public int RunTimeout { get; set; } = 30000;

        [JsonPropertyName("compile_memory_limit")]
        public int CompileMemoryLimit { get; set; } = -1;

        [JsonPropertyName("run_memory_limit")]
        public int RunMemoryLimit { get; set; } = -1;
    }

    public class PistonFile
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class PistonExecuteResponse
    {
        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("run")]
        public PistonRunResult? Run { get; set; }

        [JsonPropertyName("compile")]
        public PistonRunResult? Compile { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public class PistonRunResult
    {
        [JsonPropertyName("stdout")]
        public string Stdout { get; set; } = string.Empty;

        [JsonPropertyName("stderr")]
        public string Stderr { get; set; } = string.Empty;

        [JsonPropertyName("output")]
        public string Output { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public int? Code { get; set; }

        [JsonPropertyName("signal")]
        public string? Signal { get; set; }
    }
    #endregion

    public class CodeExecutionService : ICodeExecutionService
    {
        private readonly ILogger<CodeExecutionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private const string PISTON_API_URL = "https://emkc.org/api/v2/piston/execute";

        public CodeExecutionService(
            ILogger<CodeExecutionService> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient("PistonApi");
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<CodeExecutionResult> ExecuteCodeAsync(
            ProgrammingLanguage language,
            Dictionary<string, string> files,
            string? stdin = null,
            int timeoutSeconds = 30)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var (pistonLanguage, pistonVersion, entryPoint) = GetPistonLanguageConfig(language);

                // Prepare files for Piston API
                var pistonFiles = new List<PistonFile>();
                
                // Add the entry point file first (main file)
                var mainFile = files.FirstOrDefault(f => 
                    f.Key.EndsWith(entryPoint, StringComparison.OrdinalIgnoreCase) ||
                    f.Key.Contains("main", StringComparison.OrdinalIgnoreCase) ||
                    f.Key.Contains("Main", StringComparison.OrdinalIgnoreCase));

                if (mainFile.Key != null)
                {
                    pistonFiles.Add(new PistonFile
                    {
                        Name = Path.GetFileName(mainFile.Key),
                        Content = mainFile.Value
                    });
                }

                // Add remaining files
                foreach (var file in files)
                {
                    if (mainFile.Key == null || file.Key != mainFile.Key)
                    {
                        // Only add code files, not data files (CSV, TXT are handled separately)
                        var ext = Path.GetExtension(file.Key).ToLowerInvariant();
                        if (IsCodeFile(ext, language))
                        {
                            pistonFiles.Add(new PistonFile
                            {
                                Name = Path.GetFileName(file.Key),
                                Content = file.Value
                            });
                        }
                    }
                }

                // For languages that can read files, embed data files in the code
                var dataFiles = files.Where(f => IsDataFile(Path.GetExtension(f.Key))).ToList();
                if (dataFiles.Any() && pistonFiles.Any())
                {
                    // Inject data file contents as embedded data in the main file
                    pistonFiles = InjectDataFiles(pistonFiles, dataFiles, language);
                }

                if (!pistonFiles.Any())
                {
                    return new CodeExecutionResult
                    {
                        Success = false,
                        ErrorOutput = "No source code files found to execute.",
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }

                var request = new PistonExecuteRequest
                {
                    Language = pistonLanguage,
                    Version = pistonVersion,
                    Files = pistonFiles,
                    Stdin = stdin,
                    RunTimeout = timeoutSeconds * 1000,
                    CompileTimeout = 30000
                };

                _logger.LogInformation("Executing code via Piston API: {Language} v{Version}, {FileCount} files",
                    pistonLanguage, pistonVersion, pistonFiles.Count);

                var response = await _httpClient.PostAsJsonAsync(PISTON_API_URL, request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Piston API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    return new CodeExecutionResult
                    {
                        Success = false,
                        ErrorOutput = $"Code execution service error: {response.StatusCode}. Please try again.",
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }

                var pistonResponse = JsonSerializer.Deserialize<PistonExecuteResponse>(responseContent);

                if (pistonResponse == null)
                {
                    return new CodeExecutionResult
                    {
                        Success = false,
                        ErrorOutput = "Failed to parse execution response.",
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }

                // Check for error message
                if (!string.IsNullOrEmpty(pistonResponse.Message))
                {
                    return new CodeExecutionResult
                    {
                        Success = false,
                        ErrorOutput = pistonResponse.Message,
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }

                // Check compilation errors
                if (pistonResponse.Compile != null && pistonResponse.Compile.Code != 0)
                {
                    return new CodeExecutionResult
                    {
                        Success = false,
                        CompilationError = !string.IsNullOrEmpty(pistonResponse.Compile.Stderr) 
                            ? pistonResponse.Compile.Stderr 
                            : pistonResponse.Compile.Output,
                        ExitCode = pistonResponse.Compile.Code ?? 1,
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }

                // Get run results
                var runResult = pistonResponse.Run;
                if (runResult == null)
                {
                    return new CodeExecutionResult
                    {
                        Success = false,
                        ErrorOutput = "No execution result returned.",
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                    };
                }

                var timedOut = runResult.Signal == "SIGKILL" || runResult.Signal == "SIGTERM";

                return new CodeExecutionResult
                {
                    Success = runResult.Code == 0 && !timedOut,
                    Output = runResult.Stdout,
                    ErrorOutput = runResult.Stderr,
                    ExitCode = runResult.Code ?? 0,
                    TimedOut = timedOut,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (TaskCanceledException)
            {
                return new CodeExecutionResult
                {
                    Success = false,
                    ErrorOutput = "Execution timed out. Please try again.",
                    TimedOut = true,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling Piston API");
                return new CodeExecutionResult
                {
                    Success = false,
                    ErrorOutput = "Unable to connect to code execution service. Please check your internet connection and try again.",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing code via Piston API");
                return new CodeExecutionResult
                {
                    Success = false,
                    ErrorOutput = $"Execution error: {ex.Message}",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
        }

        public async Task<TestCaseExecutionResult> RunTestCaseAsync(
            ProgrammingLanguage language,
            Dictionary<string, string> files,
            TestCase testCase)
        {
            var result = await ExecuteCodeAsync(language, files, testCase.Input, testCase.TimeoutSeconds);

            var passed = false;
            if (result.Success && !result.TimedOut)
            {
                // Normalize outputs for comparison (trim whitespace, normalize line endings)
                var actualOutput = NormalizeOutput(result.Output);
                var expectedOutput = NormalizeOutput(testCase.ExpectedOutput ?? string.Empty);
                passed = actualOutput == expectedOutput;
            }

            return new TestCaseExecutionResult
            {
                TestCaseId = testCase.Id,
                TestCaseName = testCase.Name,
                Passed = passed,
                ExpectedOutput = testCase.ExpectedOutput,
                ActualOutput = result.Output,
                ErrorMessage = result.Success ? null : (result.CompilationError ?? result.ErrorOutput),
                ExecutionTimeMs = result.ExecutionTimeMs,
                TimedOut = result.TimedOut
            };
        }

        public async Task<List<TestCaseExecutionResult>> RunAllTestCasesAsync(
            ProgrammingLanguage language,
            Dictionary<string, string> files,
            List<TestCase> testCases)
        {
            var results = new List<TestCaseExecutionResult>();

            foreach (var testCase in testCases.OrderBy(tc => tc.Order))
            {
                var result = await RunTestCaseAsync(language, files, testCase);
                results.Add(result);
            }

            return results;
        }

        private (string Language, string Version, string EntryPoint) GetPistonLanguageConfig(ProgrammingLanguage language)
        {
            return language switch
            {
                ProgrammingLanguage.Java => ("java", "15.0.2", "Main.java"),
                ProgrammingLanguage.Python => ("python", "3.10.0", "main.py"),
                ProgrammingLanguage.CSharp => ("csharp", "6.12.0", "Program.cs"),
                ProgrammingLanguage.CPlusPlus => ("c++", "10.2.0", "main.cpp"),
                ProgrammingLanguage.JavaScript => ("javascript", "18.15.0", "main.js"),
                ProgrammingLanguage.TypeScript => ("typescript", "5.0.3", "main.ts"),
                ProgrammingLanguage.C => ("c", "10.2.0", "main.c"),
                ProgrammingLanguage.SQL => ("sqlite3", "3.36.0", "script.sql"),
                _ => throw new NotSupportedException($"Language {language} is not supported")
            };
        }

        private bool IsCodeFile(string extension, ProgrammingLanguage language)
        {
            var codeExtensions = language switch
            {
                ProgrammingLanguage.Java => new[] { ".java" },
                ProgrammingLanguage.Python => new[] { ".py" },
                ProgrammingLanguage.CSharp => new[] { ".cs" },
                ProgrammingLanguage.CPlusPlus => new[] { ".cpp", ".hpp", ".h", ".cc" },
                ProgrammingLanguage.JavaScript => new[] { ".js", ".mjs" },
                ProgrammingLanguage.TypeScript => new[] { ".ts" },
                ProgrammingLanguage.C => new[] { ".c", ".h" },
                ProgrammingLanguage.SQL => new[] { ".sql" },
                _ => Array.Empty<string>()
            };

            return codeExtensions.Contains(extension.ToLowerInvariant());
        }

        private bool IsDataFile(string extension)
        {
            var dataExtensions = new[] { ".csv", ".txt", ".json", ".xml", ".dat", ".tsv" };
            return dataExtensions.Contains(extension.ToLowerInvariant());
        }

        private List<PistonFile> InjectDataFiles(List<PistonFile> codeFiles, List<KeyValuePair<string, string>> dataFiles, ProgrammingLanguage language)
        {
            // For online execution, we need to embed data file contents
            // Create a helper data file that contains all data as strings
            
            if (!dataFiles.Any()) return codeFiles;

            var dataContent = new StringBuilder();
            
            switch (language)
            {
                case ProgrammingLanguage.Python:
                    dataContent.AppendLine("# Auto-generated data file contents");
                    dataContent.AppendLine("DATA_FILES = {");
                    foreach (var df in dataFiles)
                    {
                        var fileName = Path.GetFileName(df.Key);
                        dataContent.AppendLine($"    \"{fileName}\": \"\"\"{df.Value}\"\"\",");
                    }
                    dataContent.AppendLine("}");
                    dataContent.AppendLine();
                    dataContent.AppendLine("def read_data_file(filename):");
                    dataContent.AppendLine("    return DATA_FILES.get(filename, '')");
                    
                    codeFiles.Add(new PistonFile { Name = "data_helper.py", Content = dataContent.ToString() });
                    break;

                case ProgrammingLanguage.Java:
                    dataContent.AppendLine("// Auto-generated data file contents");
                    dataContent.AppendLine("import java.util.HashMap;");
                    dataContent.AppendLine("import java.util.Map;");
                    dataContent.AppendLine();
                    dataContent.AppendLine("public class DataFiles {");
                    dataContent.AppendLine("    private static final Map<String, String> DATA = new HashMap<>();");
                    dataContent.AppendLine("    static {");
                    foreach (var df in dataFiles)
                    {
                        var fileName = Path.GetFileName(df.Key);
                        var escapedContent = df.Value
                            .Replace("\\", "\\\\")
                            .Replace("\"", "\\\"")
                            .Replace("\r\n", "\\n")
                            .Replace("\n", "\\n");
                        dataContent.AppendLine($"        DATA.put(\"{fileName}\", \"{escapedContent}\");");
                    }
                    dataContent.AppendLine("    }");
                    dataContent.AppendLine("    public static String readDataFile(String filename) {");
                    dataContent.AppendLine("        return DATA.getOrDefault(filename, \"\");");
                    dataContent.AppendLine("    }");
                    dataContent.AppendLine("}");
                    
                    codeFiles.Add(new PistonFile { Name = "DataFiles.java", Content = dataContent.ToString() });
                    break;

                case ProgrammingLanguage.JavaScript:
                case ProgrammingLanguage.TypeScript:
                    dataContent.AppendLine("// Auto-generated data file contents");
                    dataContent.AppendLine("const DATA_FILES = {");
                    foreach (var df in dataFiles)
                    {
                        var fileName = Path.GetFileName(df.Key);
                        var escapedContent = df.Value
                            .Replace("\\", "\\\\")
                            .Replace("`", "\\`")
                            .Replace("${", "\\${");
                        dataContent.AppendLine($"    '{fileName}': `{escapedContent}`,");
                    }
                    dataContent.AppendLine("};");
                    dataContent.AppendLine();
                    dataContent.AppendLine("function readDataFile(filename) {");
                    dataContent.AppendLine("    return DATA_FILES[filename] || '';");
                    dataContent.AppendLine("}");
                    dataContent.AppendLine();
                    dataContent.AppendLine("module.exports = { DATA_FILES, readDataFile };");
                    
                    var ext = language == ProgrammingLanguage.TypeScript ? "ts" : "js";
                    codeFiles.Add(new PistonFile { Name = $"data_helper.{ext}", Content = dataContent.ToString() });
                    break;

                case ProgrammingLanguage.CSharp:
                    dataContent.AppendLine("// Auto-generated data file contents");
                    dataContent.AppendLine("using System.Collections.Generic;");
                    dataContent.AppendLine();
                    dataContent.AppendLine("public static class DataFiles");
                    dataContent.AppendLine("{");
                    dataContent.AppendLine("    private static readonly Dictionary<string, string> Data = new()");
                    dataContent.AppendLine("    {");
                    foreach (var df in dataFiles)
                    {
                        var fileName = Path.GetFileName(df.Key);
                        var escapedContent = df.Value
                            .Replace("\\", "\\\\")
                            .Replace("\"", "\\\"")
                            .Replace("\r\n", "\\n")
                            .Replace("\n", "\\n");
                        dataContent.AppendLine($"        {{ \"{fileName}\", \"{escapedContent}\" }},");
                    }
                    dataContent.AppendLine("    };");
                    dataContent.AppendLine();
                    dataContent.AppendLine("    public static string ReadDataFile(string filename)");
                    dataContent.AppendLine("    {");
                    dataContent.AppendLine("        return Data.TryGetValue(filename, out var content) ? content : string.Empty;");
                    dataContent.AppendLine("    }");
                    dataContent.AppendLine("}");
                    
                    codeFiles.Add(new PistonFile { Name = "DataFiles.cs", Content = dataContent.ToString() });
                    break;

                case ProgrammingLanguage.CPlusPlus:
                case ProgrammingLanguage.C:
                    dataContent.AppendLine("// Auto-generated data file contents");
                    dataContent.AppendLine("#ifndef DATA_FILES_H");
                    dataContent.AppendLine("#define DATA_FILES_H");
                    dataContent.AppendLine("#include <string>");
                    dataContent.AppendLine("#include <map>");
                    dataContent.AppendLine();
                    dataContent.AppendLine("inline std::map<std::string, std::string> getDataFiles() {");
                    dataContent.AppendLine("    return {");
                    foreach (var df in dataFiles)
                    {
                        var fileName = Path.GetFileName(df.Key);
                        var escapedContent = df.Value
                            .Replace("\\", "\\\\")
                            .Replace("\"", "\\\"")
                            .Replace("\r\n", "\\n")
                            .Replace("\n", "\\n");
                        dataContent.AppendLine($"        {{\"{fileName}\", \"{escapedContent}\"}},");
                    }
                    dataContent.AppendLine("    };");
                    dataContent.AppendLine("}");
                    dataContent.AppendLine();
                    dataContent.AppendLine("inline std::string readDataFile(const std::string& filename) {");
                    dataContent.AppendLine("    auto files = getDataFiles();");
                    dataContent.AppendLine("    auto it = files.find(filename);");
                    dataContent.AppendLine("    return it != files.end() ? it->second : \"\";");
                    dataContent.AppendLine("}");
                    dataContent.AppendLine("#endif");
                    
                    codeFiles.Add(new PistonFile { Name = "data_files.h", Content = dataContent.ToString() });
                    break;
            }

            return codeFiles;
        }

        private static string NormalizeOutput(string output)
        {
            if (string.IsNullOrEmpty(output)) return string.Empty;

            // Normalize line endings and trim
            return output
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Trim();
        }
    }
}
