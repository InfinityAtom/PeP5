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

            _logger.LogInformation("=== ExecuteCodeAsync START === Language: {Language}, Files: {Files}", 
                language, string.Join(", ", files.Keys));

            try
            {
                var (pistonLanguage, pistonVersion, entryPoint) = GetPistonLanguageConfig(language);

                // Prepare files for Piston API
                var pistonFiles = new List<PistonFile>();
                
                // Language-specific multi-file handling
                if (language == ProgrammingLanguage.Java)
                {
                    _logger.LogInformation("Using Java multi-file handler for {Count} files", files.Count);
                    pistonFiles = PrepareJavaFiles(files, entryPoint);
                }
                else if (language == ProgrammingLanguage.Python)
                {
                    pistonFiles = PreparePythonFiles(files, entryPoint);
                }
                else if (language == ProgrammingLanguage.CSharp)
                {
                    pistonFiles = PrepareCSharpFiles(files, entryPoint);
                }
                else
                {
                    // Default handling for other languages
                    pistonFiles = PrepareDefaultFiles(files, entryPoint, language);
                }

                _logger.LogInformation("=== Piston files prepared === Count: {Count}, Names: {Names}",
                    pistonFiles.Count, string.Join(", ", pistonFiles.Select(f => f.Name)));
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
                
                _logger.LogInformation("Piston API response: {Response}", responseContent);

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

        /// <summary>
        /// Prepares Java files for Piston execution.
        /// IMPORTANT: Piston only compiles the FIRST file for Java, so we must combine all classes
        /// into a single file. For this to work:
        /// 1. Remove package declarations (Piston runs in flat directory)
        /// 2. Remove local imports (classes will be in same file)
        /// 3. Make non-main classes package-private (no public modifier)
        /// 4. Keep main class public and put it FIRST in the file
        /// </summary>
        private List<PistonFile> PrepareJavaFiles(Dictionary<string, string> files, string entryPoint)
        {
            var javaFiles = files.Where(f => f.Key.EndsWith(".java", StringComparison.OrdinalIgnoreCase)).ToList();
            
            _logger.LogInformation("PrepareJavaFiles: {Count} Java files found. Files: {Files}", 
                javaFiles.Count, string.Join(", ", javaFiles.Select(f => f.Key)));
            
            // If only one file, just process it normally
            if (javaFiles.Count == 1)
            {
                var singleFile = javaFiles.First();
                var content = RemovePackageDeclaration(singleFile.Value);
                var fileName = Path.GetFileName(singleFile.Key);
                
                _logger.LogInformation("Single Java file mode: {FileName}", fileName);
                
                return new List<PistonFile>
                {
                    new PistonFile
                    {
                        Name = fileName,
                        Content = content
                    }
                };
            }
            
            // Multiple files - MUST combine into single file because Piston only compiles first file
            var processedClasses = new List<(string ClassName, string Content, bool HasMain)>();
            string? mainClassName = null;
            
            // Get all class names for removing local imports
            var allClassNames = javaFiles.Select(f => Path.GetFileNameWithoutExtension(Path.GetFileName(f.Key))).ToList();
            
            foreach (var file in javaFiles)
            {
                var fileName = Path.GetFileName(file.Key);
                var className = Path.GetFileNameWithoutExtension(fileName);
                var content = file.Value;
                
                // Remove package declarations
                content = RemovePackageDeclaration(content);
                
                // Remove imports for classes within the same project
                content = RemoveLocalImports(content, allClassNames);
                
                // Check if this file has main method
                bool hasMain = HasJavaMainMethod(content);
                
                _logger.LogInformation("Processing Java class: {ClassName}, HasMain: {HasMain}", className, hasMain);
                
                if (hasMain)
                {
                    mainClassName = className;
                    _logger.LogInformation("MAIN CLASS DETECTED: {ClassName}", className);
                }
                
                processedClasses.Add((className, content, hasMain));
            }
            
            // Determine main class: 1) Has main method, 2) Named "Main", 3) First class
            string finalMainClassName;
            if (!string.IsNullOrEmpty(mainClassName))
            {
                finalMainClassName = mainClassName;
            }
            else
            {
                var mainNamedClass = processedClasses.FirstOrDefault(c => 
                    c.ClassName.Equals("Main", StringComparison.OrdinalIgnoreCase));
                finalMainClassName = mainNamedClass.ClassName ?? processedClasses.First().ClassName;
                _logger.LogWarning("No main method found, using class: {ClassName}", finalMainClassName);
            }
            
            // Build combined file - main class MUST be first and public
            var combinedContent = new StringBuilder();
            combinedContent.AppendLine("// Combined Java file for Piston execution");
            combinedContent.AppendLine("import java.util.*;");
            combinedContent.AppendLine("import java.io.*;");
            combinedContent.AppendLine("import java.math.*;");
            combinedContent.AppendLine("import java.text.*;");
            combinedContent.AppendLine("import java.time.*;");
            combinedContent.AppendLine("import java.util.stream.*;");
            combinedContent.AppendLine();
            
            // Add main class FIRST - ensure it's public
            var mainClassInfo = processedClasses.First(c => c.ClassName == finalMainClassName);
            var mainContent = EnsureClassIsPublic(mainClassInfo.Content, finalMainClassName);
            // Remove any duplicate import statements from the main class content
            mainContent = RemoveImportStatements(mainContent);
            combinedContent.AppendLine("// === Main class: " + finalMainClassName + " ===");
            combinedContent.AppendLine(mainContent);
            combinedContent.AppendLine();
            
            // Add all other classes (make them package-private)
            foreach (var classInfo in processedClasses)
            {
                if (classInfo.ClassName != finalMainClassName)
                {
                    var classContent = MakeClassNonPublic(classInfo.Content);
                    classContent = RemoveImportStatements(classContent);
                    combinedContent.AppendLine("// === Class: " + classInfo.ClassName + " ===");
                    combinedContent.AppendLine(classContent);
                    combinedContent.AppendLine();
                }
            }
            
            // File MUST be named after the public class
            var outputFileName = $"{finalMainClassName}.java";
            
            _logger.LogInformation("Combined {Count} Java files into: {FileName}", javaFiles.Count, outputFileName);
            
            return new List<PistonFile>
            {
                new PistonFile
                {
                    Name = outputFileName,
                    Content = combinedContent.ToString()
                }
            };
        }
        
        /// <summary>
        /// Removes import statements from Java code (used when combining files to avoid duplicates)
        /// </summary>
        private string RemoveImportStatements(string javaCode)
        {
            var lines = javaCode.Split('\n');
            var result = new List<string>();
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("import "))
                {
                    result.Add(line);
                }
            }
            
            return string.Join("\n", result);
        }
        
        /// <summary>
        /// Checks if Java code contains a main method using multiple detection patterns.
        /// </summary>
        private bool HasJavaMainMethod(string javaCode)
        {
            if (string.IsNullOrEmpty(javaCode))
                return false;
            
            // Multiple patterns to detect main method
            var patterns = new[]
            {
                @"public\s+static\s+void\s+main\s*\(\s*String\s*\[\s*\]\s*\w*\s*\)",  // String[] args
                @"public\s+static\s+void\s+main\s*\(\s*String\s+\w+\s*\[\s*\]\s*\)",  // String args[]
                @"public\s+static\s+void\s+main\s*\(\s*String\s*\.\.\.\s*\w*\s*\)",   // String... args (varargs)
            };
            
            foreach (var pattern in patterns)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(javaCode, pattern, 
                    System.Text.RegularExpressions.RegexOptions.Singleline))
                {
                    return true;
                }
            }
            
            // Also do a simple string check as fallback
            if (javaCode.Contains("public static void main") && javaCode.Contains("String"))
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Ensures a Java class has the public modifier.
        /// This is needed for the main class in a combined file.
        /// </summary>
        private string EnsureClassIsPublic(string javaCode, string className)
        {
            var lines = javaCode.Split('\n');
            var result = new List<string>();
            bool classFound = false;
            
            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                
                // Look for class declaration that matches our class name
                if (!classFound)
                {
                    // Check for various class declaration patterns
                    if (trimmed.StartsWith($"class {className}") ||
                        trimmed.StartsWith($"final class {className}") ||
                        trimmed.StartsWith($"abstract class {className}"))
                    {
                        // Add public modifier
                        var modifiedLine = line.Replace($"class {className}", $"public class {className}")
                                               .Replace($"final class {className}", $"public final class {className}")
                                               .Replace($"abstract class {className}", $"public abstract class {className}");
                        result.Add(modifiedLine);
                        classFound = true;
                        continue;
                    }
                    // Already has public - leave as is
                    else if (trimmed.StartsWith($"public class {className}") ||
                             trimmed.StartsWith($"public final class {className}") ||
                             trimmed.StartsWith($"public abstract class {className}"))
                    {
                        classFound = true;
                    }
                }
                
                result.Add(line);
            }
            
            return string.Join("\n", result);
        }
        
        /// <summary>
        /// Makes a Java class non-public by removing the 'public' modifier from the class declaration.
        /// This allows multiple classes to exist in a single .java file.
        /// </summary>
        private string MakeClassNonPublic(string javaCode)
        {
            // Replace "public class ClassName" with "class ClassName"
            // But preserve "public static" methods and "public" fields
            var lines = javaCode.Split('\n');
            var result = new List<string>();
            
            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                // Check if this is a class declaration line (not method/field)
                if (trimmed.StartsWith("public class ") || trimmed.StartsWith("public final class ") ||
                    trimmed.StartsWith("public abstract class "))
                {
                    // Remove "public " from the class declaration
                    result.Add(line.Replace("public class ", "class ")
                                   .Replace("public final class ", "final class ")
                                   .Replace("public abstract class ", "abstract class "));
                }
                else
                {
                    result.Add(line);
                }
            }
            
            return string.Join("\n", result);
        }
        
        /// <summary>
        /// Removes import statements for classes that are defined in the same project.
        /// When combining files, these imports are not needed and may cause errors.
        /// </summary>
        private string RemoveLocalImports(string javaCode, List<string> projectClassNames)
        {
            var lines = javaCode.Split('\n');
            var result = new List<string>();
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // Check if it's an import statement
                if (trimmed.StartsWith("import "))
                {
                    // Check if it imports one of our project classes
                    bool isLocalImport = projectClassNames.Any(className => 
                        trimmed.EndsWith($".{className};") || 
                        trimmed.Equals($"import {className};"));
                    
                    if (!isLocalImport)
                    {
                        result.Add(line);
                    }
                    // Skip local imports
                }
                else
                {
                    result.Add(line);
                }
            }
            
            return string.Join("\n", result);
        }
        
        /// <summary>
        /// Removes package declaration from Java source code.
        /// Piston executes files in a flat directory structure, so package declarations
        /// cause compilation errors when classes reference each other.
        /// </summary>
        private string RemovePackageDeclaration(string javaCode)
        {
            if (string.IsNullOrEmpty(javaCode))
                return javaCode;
            
            // Remove package declaration line (e.g., "package com.example;")
            var lines = javaCode.Split('\n');
            var result = new List<string>();
            bool packageRemoved = false;
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                // Skip package declaration (only the first one)
                if (!packageRemoved && trimmed.StartsWith("package ") && trimmed.EndsWith(";"))
                {
                    packageRemoved = true;
                    continue;
                }
                // Also skip package with multiline or different formatting
                if (!packageRemoved && System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^package\s+[\w.]+\s*;"))
                {
                    packageRemoved = true;
                    continue;
                }
                result.Add(line);
            }
            
            return string.Join("\n", result).TrimStart('\n');
        }

        /// <summary>
        /// Prepares Python files for Piston execution.
        /// Since Piston may not handle imports between files well,
        /// we combine all Python files into a single file.
        /// </summary>
        private List<PistonFile> PreparePythonFiles(Dictionary<string, string> files, string entryPoint)
        {
            var pythonFiles = files.Where(f => f.Key.EndsWith(".py", StringComparison.OrdinalIgnoreCase)).ToList();
            
            _logger.LogInformation("Preparing {Count} Python files for execution", pythonFiles.Count);
            
            // If only one file, just return it
            if (pythonFiles.Count == 1)
            {
                var singleFile = pythonFiles.First();
                return new List<PistonFile>
                {
                    new PistonFile
                    {
                        Name = Path.GetFileName(singleFile.Key),
                        Content = singleFile.Value
                    }
                };
            }
            
            // Multiple files - combine them
            var moduleNames = pythonFiles.Select(f => Path.GetFileNameWithoutExtension(f.Key)).ToList();
            var combinedContent = new StringBuilder();
            combinedContent.AppendLine("# Combined Python file for execution");
            combinedContent.AppendLine();
            
            // Find main file
            var mainFile = pythonFiles.FirstOrDefault(f =>
            {
                var fileName = Path.GetFileName(f.Key);
                return fileName.Equals(entryPoint, StringComparison.OrdinalIgnoreCase) ||
                       fileName.Equals("main.py", StringComparison.OrdinalIgnoreCase) ||
                       f.Value.Contains("if __name__");
            });
            
            // Add non-main files first (as their content will be available to main)
            foreach (var file in pythonFiles)
            {
                if (mainFile.Key == null || file.Key != mainFile.Key)
                {
                    var moduleName = Path.GetFileNameWithoutExtension(file.Key);
                    var content = RemovePythonLocalImports(file.Value, moduleNames);
                    
                    combinedContent.AppendLine($"# === Content from {moduleName}.py ===");
                    combinedContent.AppendLine(content);
                    combinedContent.AppendLine();
                }
            }
            
            // Add main file content last
            if (mainFile.Key != null)
            {
                var mainContent = RemovePythonLocalImports(mainFile.Value, moduleNames);
                // Remove "if __name__ == '__main__':" guard since we want it to run
                mainContent = RemovePythonMainGuard(mainContent);
                
                combinedContent.AppendLine("# === Main execution ===");
                combinedContent.AppendLine(mainContent);
            }
            
            var result = new List<PistonFile>
            {
                new PistonFile
                {
                    Name = "main.py",
                    Content = combinedContent.ToString()
                }
            };
            
            _logger.LogInformation("Combined {Count} Python files into single file", pythonFiles.Count);
            
            return result;
        }
        
        /// <summary>
        /// Removes import statements for local modules (files in the same project).
        /// </summary>
        private string RemovePythonLocalImports(string pythonCode, List<string> moduleNames)
        {
            var lines = pythonCode.Split('\n');
            var result = new List<string>();
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // Check various import patterns
                bool isLocalImport = false;
                
                // "import modulename" or "import modulename as x"
                if (trimmed.StartsWith("import "))
                {
                    var importPart = trimmed.Substring(7).Split(new[] { " as " }, StringSplitOptions.None)[0].Trim();
                    isLocalImport = moduleNames.Contains(importPart);
                }
                // "from modulename import ..."
                else if (trimmed.StartsWith("from "))
                {
                    var fromMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"^from\s+(\w+)\s+import");
                    if (fromMatch.Success)
                    {
                        isLocalImport = moduleNames.Contains(fromMatch.Groups[1].Value);
                    }
                }
                
                if (!isLocalImport)
                {
                    result.Add(line);
                }
            }
            
            return string.Join("\n", result);
        }
        
        /// <summary>
        /// Removes or processes the if __name__ == '__main__' guard.
        /// </summary>
        private string RemovePythonMainGuard(string pythonCode)
        {
            // Find and remove the if __name__ guard, keeping the indented code
            var lines = pythonCode.Split('\n').ToList();
            var result = new List<string>();
            bool inMainBlock = false;
            int mainIndent = 0;
            
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();
                
                if (trimmed.StartsWith("if __name__") && trimmed.Contains("__main__"))
                {
                    inMainBlock = true;
                    // Calculate the indentation of the if statement
                    mainIndent = line.Length - line.TrimStart().Length;
                    continue; // Skip the if line itself
                }
                
                if (inMainBlock)
                {
                    // Check if we're still in the main block
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        result.Add(line);
                        continue;
                    }
                    
                    int currentIndent = line.Length - line.TrimStart().Length;
                    if (currentIndent > mainIndent)
                    {
                        // Remove one level of indentation (typically 4 spaces)
                        var dedentedLine = line.Length > 4 ? line.Substring(4) : line.TrimStart();
                        result.Add(dedentedLine);
                    }
                    else
                    {
                        // We've exited the main block
                        inMainBlock = false;
                        result.Add(line);
                    }
                }
                else
                {
                    result.Add(line);
                }
            }
            
            return string.Join("\n", result);
        }

        /// <summary>
        /// Prepares C# files for Piston execution.
        /// Since Piston may not compile multiple C# files together properly,
        /// we combine all files into a single file.
        /// </summary>
        private List<PistonFile> PrepareCSharpFiles(Dictionary<string, string> files, string entryPoint)
        {
            var csharpFiles = files.Where(f => f.Key.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToList();
            
            _logger.LogInformation("Preparing {Count} C# files for execution", csharpFiles.Count);
            
            // If only one file, just return it
            if (csharpFiles.Count == 1)
            {
                var singleFile = csharpFiles.First();
                return new List<PistonFile>
                {
                    new PistonFile
                    {
                        Name = Path.GetFileName(singleFile.Key),
                        Content = singleFile.Value
                    }
                };
            }
            
            // Multiple files - combine them
            var combinedContent = new StringBuilder();
            combinedContent.AppendLine("// Combined C# file for execution");
            
            // Collect all unique using statements
            var usings = new HashSet<string>();
            var classContents = new List<(string ClassName, string Content, bool HasMain)>();
            
            foreach (var file in csharpFiles)
            {
                var content = file.Value;
                var lines = content.Split('\n');
                var classContent = new StringBuilder();
                bool inClass = false;
                int braceCount = 0;
                bool hasMain = content.Contains("static void Main") || content.Contains("static async Task Main");
                string className = Path.GetFileNameWithoutExtension(file.Key);
                
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    
                    // Collect using statements
                    if (trimmed.StartsWith("using ") && trimmed.EndsWith(";") && !trimmed.Contains("("))
                    {
                        usings.Add(trimmed);
                        continue;
                    }
                    
                    // Skip namespace declarations (we'll put everything in global namespace)
                    if (trimmed.StartsWith("namespace "))
                    {
                        continue;
                    }
                    
                    // Skip standalone braces that are part of namespace
                    if (!inClass && (trimmed == "{" || trimmed == "}"))
                    {
                        continue;
                    }
                    
                    // Detect class/struct/record/interface declarations
                    if (!inClass && (trimmed.Contains("class ") || trimmed.Contains("struct ") || 
                                     trimmed.Contains("record ") || trimmed.Contains("interface ") ||
                                     trimmed.Contains("enum ")))
                    {
                        inClass = true;
                    }
                    
                    if (inClass)
                    {
                        classContent.AppendLine(line);
                        braceCount += line.Count(c => c == '{') - line.Count(c => c == '}');
                        
                        if (braceCount <= 0)
                        {
                            inClass = false;
                        }
                    }
                }
                
                if (classContent.Length > 0)
                {
                    classContents.Add((className, classContent.ToString().Trim(), hasMain));
                }
            }
            
            // Add using statements
            foreach (var usingStatement in usings.OrderBy(u => u))
            {
                combinedContent.AppendLine(usingStatement);
            }
            combinedContent.AppendLine();
            
            // Add classes without Main first
            foreach (var (className, content, hasMain) in classContents.Where(c => !c.HasMain))
            {
                combinedContent.AppendLine($"// From {className}.cs");
                combinedContent.AppendLine(content);
                combinedContent.AppendLine();
            }
            
            // Add class with Main last
            foreach (var (className, content, hasMain) in classContents.Where(c => c.HasMain))
            {
                combinedContent.AppendLine($"// From {className}.cs (Main)");
                combinedContent.AppendLine(content);
                combinedContent.AppendLine();
            }
            
            var result = new List<PistonFile>
            {
                new PistonFile
                {
                    Name = "Program.cs",
                    Content = combinedContent.ToString()
                }
            };
            
            _logger.LogInformation("Combined {Count} C# files into single file", csharpFiles.Count);
            
            return result;
        }

        /// <summary>
        /// Default file preparation for other languages.
        /// </summary>
        private List<PistonFile> PrepareDefaultFiles(Dictionary<string, string> files, string entryPoint, ProgrammingLanguage language)
        {
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

            // Add remaining code files
            foreach (var file in files)
            {
                if (mainFile.Key == null || file.Key != mainFile.Key)
                {
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
            
            return pistonFiles;
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
                    // For Java, append DataFiles class to the combined file (Piston only compiles first file)
                    dataContent.AppendLine("");
                    dataContent.AppendLine("// === Auto-generated DataFiles helper class ===");
                    dataContent.AppendLine("class DataFiles {");
                    dataContent.AppendLine("    private static final java.util.Map<String, String> DATA = new java.util.HashMap<>();");
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
                    
                    // Append to the combined Java file
                    if (codeFiles.Any())
                    {
                        var mainFile = codeFiles[0];
                        mainFile.Content = mainFile.Content.TrimEnd() + "\n\n" + dataContent.ToString();
                        _logger.LogInformation("Appended DataFiles class to combined Java file");
                    }
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
