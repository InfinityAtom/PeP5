// PeP Monaco Editor Integration
window.pepMonaco = {
    editors: {},
    
    // Cross-file symbol tracking
    projectFiles: {},  // { filePath: content }
    projectSymbols: [], // Extracted symbols from all files
    projectClasses: {}, // { className: { methods: [], fields: [], constructors: [] } }
    completionProviders: {}, // Registered completion providers by language
    signatureProviders: {}, // Registered signature help providers
    hoverProviders: {}, // Registered hover providers
    
    // Initialize cross-file intelligence for a language
    initializeLanguageSupport: function(language) {
        if (this.completionProviders[language]) {
            return; // Already registered
        }
        
        const self = this;
        
        // Register completion provider for cross-file symbols
        this.completionProviders[language] = monaco.languages.registerCompletionItemProvider(language, {
            triggerCharacters: ['.', '(', ' '],
            provideCompletionItems: function(model, position) {
                const textUntilPosition = model.getValueInRange({
                    startLineNumber: position.lineNumber,
                    startColumn: 1,
                    endLineNumber: position.lineNumber,
                    endColumn: position.column
                });
                
                const word = model.getWordUntilPosition(position);
                const range = {
                    startLineNumber: position.lineNumber,
                    endLineNumber: position.lineNumber,
                    startColumn: word.startColumn,
                    endColumn: word.endColumn
                };
                
                // Check if we're typing after a dot (member access)
                const dotMatch = textUntilPosition.match(/(\w+)\.\s*(\w*)$/);
                if (dotMatch) {
                    const varName = dotMatch[1];
                    const partial = dotMatch[2] || '';
                    return self.getMemberCompletions(model, varName, partial, range, position);
                }
                
                // Check if we're in a constructor call: new ClassName(
                const constructorMatch = textUntilPosition.match(/new\s+(\w+)\s*\(\s*$/);
                if (constructorMatch) {
                    return { suggestions: [] }; // Let signature help handle this
                }
                
                // Regular completions - symbols + keywords + snippets
                return self.getStandardCompletions(model, range, language, textUntilPosition);
            }
        });
        
        // Register signature help provider (method parameter hints)
        this.signatureProviders[language] = monaco.languages.registerSignatureHelpProvider(language, {
            signatureHelpTriggerCharacters: ['(', ','],
            provideSignatureHelp: function(model, position) {
                const textUntilPosition = model.getValueInRange({
                    startLineNumber: position.lineNumber,
                    startColumn: 1,
                    endLineNumber: position.lineNumber,
                    endColumn: position.column
                });
                
                return self.getSignatureHelp(textUntilPosition, language);
            }
        });
        
        // Register hover provider (show info on hover)
        this.hoverProviders[language] = monaco.languages.registerHoverProvider(language, {
            provideHover: function(model, position) {
                const word = model.getWordAtPosition(position);
                if (!word) return null;
                
                return self.getHoverInfo(word.word, language);
            }
        });
    },
    
    // Get member completions (after dot)
    getMemberCompletions: function(model, varName, partial, range, position) {
        const suggestions = [];
        const content = model.getValue();
        
        // Try to determine the type of the variable
        const varType = this.inferVariableType(content, varName);
        
        if (varType && this.projectClasses[varType]) {
            const classInfo = this.projectClasses[varType];
            
            // Add methods
            (classInfo.methods || []).forEach(method => {
                suggestions.push({
                    label: method.name,
                    kind: monaco.languages.CompletionItemKind.Method,
                    detail: `${method.returnType || 'void'} ${method.name}(${method.params || ''})`,
                    documentation: method.documentation || `Method from ${varType}`,
                    insertText: method.name + (method.params ? '($0)' : '()'),
                    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                    range: range,
                    sortText: '0' + method.name // Prioritize methods
                });
            });
            
            // Add fields
            (classInfo.fields || []).forEach(field => {
                suggestions.push({
                    label: field.name,
                    kind: monaco.languages.CompletionItemKind.Field,
                    detail: `${field.type} ${field.name}`,
                    documentation: `Field from ${varType}`,
                    insertText: field.name,
                    range: range,
                    sortText: '1' + field.name
                });
            });
        }
        
        // Add common methods for known types
        suggestions.push(...this.getBuiltInMethods(varName, varType, range));
        
        return { suggestions: suggestions };
    },
    
    // Infer variable type from code
    inferVariableType: function(content, varName) {
        // Pattern: ClassName varName = 
        // Pattern: ClassName varName;
        // Pattern: for (ClassName varName : 
        const patterns = [
            new RegExp(`(\\w+)(?:<[^>]+>)?\\s+${varName}\\s*[=;]`, 'g'),
            new RegExp(`(\\w+)(?:<[^>]+>)?\\s+${varName}\\s*:`, 'g'),
            new RegExp(`\\(\\s*(\\w+)(?:<[^>]+>)?\\s+${varName}\\s*\\)`, 'g'),
            new RegExp(`${varName}\\s*=\\s*new\\s+(\\w+)`, 'g')
        ];
        
        for (const pattern of patterns) {
            const match = pattern.exec(content);
            if (match && match[1]) {
                const type = match[1];
                // Skip primitive types and common non-class keywords
                if (!['int', 'double', 'float', 'boolean', 'char', 'byte', 'short', 'long', 'void', 'var', 'if', 'for', 'while'].includes(type)) {
                    return type;
                }
            }
        }
        
        return null;
    },
    
    // Get built-in methods for common types
    getBuiltInMethods: function(varName, varType, range) {
        const suggestions = [];
        
        // String methods
        if (varType === 'String' || varName.toLowerCase().includes('str') || varName.toLowerCase().includes('name') || varName.toLowerCase().includes('text')) {
            const stringMethods = [
                { name: 'length', detail: 'int length()', doc: 'Returns the length of the string' },
                { name: 'charAt', detail: 'char charAt(int index)', doc: 'Returns the character at the specified index', snippet: 'charAt($0)' },
                { name: 'substring', detail: 'String substring(int beginIndex, int endIndex)', doc: 'Returns a substring', snippet: 'substring($1, $2)' },
                { name: 'toLowerCase', detail: 'String toLowerCase()', doc: 'Converts to lowercase' },
                { name: 'toUpperCase', detail: 'String toUpperCase()', doc: 'Converts to uppercase' },
                { name: 'trim', detail: 'String trim()', doc: 'Removes leading and trailing whitespace' },
                { name: 'equals', detail: 'boolean equals(Object obj)', doc: 'Compares strings for equality', snippet: 'equals($0)' },
                { name: 'equalsIgnoreCase', detail: 'boolean equalsIgnoreCase(String str)', doc: 'Case-insensitive comparison', snippet: 'equalsIgnoreCase($0)' },
                { name: 'contains', detail: 'boolean contains(CharSequence s)', doc: 'Checks if string contains sequence', snippet: 'contains($0)' },
                { name: 'startsWith', detail: 'boolean startsWith(String prefix)', doc: 'Checks if string starts with prefix', snippet: 'startsWith($0)' },
                { name: 'endsWith', detail: 'boolean endsWith(String suffix)', doc: 'Checks if string ends with suffix', snippet: 'endsWith($0)' },
                { name: 'indexOf', detail: 'int indexOf(String str)', doc: 'Returns index of first occurrence', snippet: 'indexOf($0)' },
                { name: 'replace', detail: 'String replace(char old, char new)', doc: 'Replaces characters', snippet: 'replace($1, $2)' },
                { name: 'split', detail: 'String[] split(String regex)', doc: 'Splits string by regex', snippet: 'split($0)' },
                { name: 'isEmpty', detail: 'boolean isEmpty()', doc: 'Checks if string is empty' }
            ];
            
            stringMethods.forEach(m => {
                suggestions.push({
                    label: m.name,
                    kind: monaco.languages.CompletionItemKind.Method,
                    detail: m.detail,
                    documentation: m.doc,
                    insertText: m.snippet || m.name + '()',
                    insertTextRules: m.snippet ? monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet : undefined,
                    range: range,
                    sortText: '2' + m.name
                });
            });
        }
        
        // Array/List methods  
        if (varType === 'ArrayList' || varType === 'List' || varName.toLowerCase().includes('list') || varName.toLowerCase().includes('array')) {
            const listMethods = [
                { name: 'add', detail: 'boolean add(E element)', doc: 'Adds element to list', snippet: 'add($0)' },
                { name: 'get', detail: 'E get(int index)', doc: 'Returns element at index', snippet: 'get($0)' },
                { name: 'set', detail: 'E set(int index, E element)', doc: 'Sets element at index', snippet: 'set($1, $2)' },
                { name: 'remove', detail: 'E remove(int index)', doc: 'Removes element at index', snippet: 'remove($0)' },
                { name: 'size', detail: 'int size()', doc: 'Returns number of elements' },
                { name: 'isEmpty', detail: 'boolean isEmpty()', doc: 'Checks if list is empty' },
                { name: 'contains', detail: 'boolean contains(Object o)', doc: 'Checks if list contains element', snippet: 'contains($0)' },
                { name: 'clear', detail: 'void clear()', doc: 'Removes all elements' },
                { name: 'indexOf', detail: 'int indexOf(Object o)', doc: 'Returns index of element', snippet: 'indexOf($0)' },
                { name: 'toArray', detail: 'Object[] toArray()', doc: 'Converts to array' }
            ];
            
            listMethods.forEach(m => {
                suggestions.push({
                    label: m.name,
                    kind: monaco.languages.CompletionItemKind.Method,
                    detail: m.detail,
                    documentation: m.doc,
                    insertText: m.snippet || m.name + '()',
                    insertTextRules: m.snippet ? monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet : undefined,
                    range: range,
                    sortText: '2' + m.name
                });
            });
        }
        
        // Scanner methods
        if (varType === 'Scanner' || varName.toLowerCase().includes('scanner') || varName.toLowerCase() === 'sc' || varName.toLowerCase() === 'input') {
            const scannerMethods = [
                { name: 'nextLine', detail: 'String nextLine()', doc: 'Reads a line of text' },
                { name: 'next', detail: 'String next()', doc: 'Reads next token' },
                { name: 'nextInt', detail: 'int nextInt()', doc: 'Reads next integer' },
                { name: 'nextDouble', detail: 'double nextDouble()', doc: 'Reads next double' },
                { name: 'nextBoolean', detail: 'boolean nextBoolean()', doc: 'Reads next boolean' },
                { name: 'hasNext', detail: 'boolean hasNext()', doc: 'Checks if there is more input' },
                { name: 'hasNextLine', detail: 'boolean hasNextLine()', doc: 'Checks if there is another line' },
                { name: 'hasNextInt', detail: 'boolean hasNextInt()', doc: 'Checks if next token is int' },
                { name: 'close', detail: 'void close()', doc: 'Closes the scanner' }
            ];
            
            scannerMethods.forEach(m => {
                suggestions.push({
                    label: m.name,
                    kind: monaco.languages.CompletionItemKind.Method,
                    detail: m.detail,
                    documentation: m.doc,
                    insertText: m.name + '()',
                    range: range,
                    sortText: '2' + m.name
                });
            });
        }
        
        return suggestions;
    },
    
    // Get standard completions (symbols, keywords, snippets)
    getStandardCompletions: function(model, range, language, textUntilPosition) {
        const suggestions = [];
        
        // Add project symbols
        this.projectSymbols.forEach(symbol => {
            let kind;
            switch(symbol.kind) {
                case 'class': kind = monaco.languages.CompletionItemKind.Class; break;
                case 'interface': kind = monaco.languages.CompletionItemKind.Interface; break;
                case 'method': kind = monaco.languages.CompletionItemKind.Method; break;
                case 'function': kind = monaco.languages.CompletionItemKind.Function; break;
                case 'variable': kind = monaco.languages.CompletionItemKind.Variable; break;
                case 'field': kind = monaco.languages.CompletionItemKind.Field; break;
                case 'constant': kind = monaco.languages.CompletionItemKind.Constant; break;
                case 'enum': kind = monaco.languages.CompletionItemKind.Enum; break;
                case 'property': kind = monaco.languages.CompletionItemKind.Property; break;
                case 'constructor': kind = monaco.languages.CompletionItemKind.Constructor; break;
                default: kind = monaco.languages.CompletionItemKind.Text;
            }
            
            suggestions.push({
                label: symbol.name,
                kind: kind,
                detail: symbol.detail || `${symbol.kind} from ${symbol.file}`,
                documentation: symbol.documentation || '',
                insertText: symbol.insertText || symbol.name,
                insertTextRules: symbol.insertText && symbol.insertText.includes('$') 
                    ? monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet 
                    : undefined,
                range: range,
                sortText: symbol.kind === 'class' ? '0' + symbol.name : '1' + symbol.name
            });
        });
        
        // Add language-specific keywords and snippets
        if (language === 'java') {
            suggestions.push(...this.getJavaKeywordsAndSnippets(range));
        } else if (language === 'python') {
            suggestions.push(...this.getPythonKeywordsAndSnippets(range));
        } else if (language === 'csharp') {
            suggestions.push(...this.getCSharpKeywordsAndSnippets(range));
        } else if (language === 'javascript' || language === 'typescript') {
            suggestions.push(...this.getJavaScriptKeywordsAndSnippets(range));
        } else if (language === 'cpp' || language === 'c') {
            suggestions.push(...this.getCppKeywordsAndSnippets(range));
        }
        
        return { suggestions: suggestions };
    },
    
    // Get Java keywords and code snippets
    getJavaKeywordsAndSnippets: function(range) {
        const snippets = [
            // Control structures
            { label: 'if', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'if (${1:condition}) {\n\t$0\n}', detail: 'if statement' },
            { label: 'ifelse', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'if (${1:condition}) {\n\t$2\n} else {\n\t$0\n}', detail: 'if-else statement' },
            { label: 'for', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'for (int ${1:i} = 0; $1 < ${2:length}; $1++) {\n\t$0\n}', detail: 'for loop' },
            { label: 'foreach', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'for (${1:Type} ${2:item} : ${3:collection}) {\n\t$0\n}', detail: 'enhanced for loop' },
            { label: 'while', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'while (${1:condition}) {\n\t$0\n}', detail: 'while loop' },
            { label: 'dowhile', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'do {\n\t$0\n} while (${1:condition});', detail: 'do-while loop' },
            { label: 'switch', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'switch (${1:key}) {\n\tcase ${2:value}:\n\t\t$0\n\t\tbreak;\n\tdefault:\n\t\tbreak;\n}', detail: 'switch statement' },
            { label: 'try', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'try {\n\t$0\n} catch (${1:Exception} ${2:e}) {\n\t${3:e.printStackTrace();}\n}', detail: 'try-catch block' },
            
            // Class structures
            { label: 'class', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'public class ${1:ClassName} {\n\t$0\n}', detail: 'class declaration' },
            { label: 'main', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'public static void main(String[] args) {\n\t$0\n}', detail: 'main method' },
            { label: 'method', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'public ${1:void} ${2:methodName}(${3:}) {\n\t$0\n}', detail: 'method declaration' },
            { label: 'constructor', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'public ${1:ClassName}(${2:}) {\n\t$0\n}', detail: 'constructor' },
            
            // Output
            { label: 'sout', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'System.out.println($0);', detail: 'System.out.println()' },
            { label: 'soutf', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'System.out.printf("${1:%s}\\n", $0);', detail: 'System.out.printf()' },
            { label: 'serr', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'System.err.println($0);', detail: 'System.err.println()' },
            
            // Common declarations
            { label: 'scanner', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'Scanner ${1:scanner} = new Scanner(System.in);', detail: 'Scanner declaration' },
            { label: 'arraylist', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'ArrayList<${1:Type}> ${2:list} = new ArrayList<>();', detail: 'ArrayList declaration' },
            { label: 'hashmap', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'HashMap<${1:Key}, ${2:Value}> ${3:map} = new HashMap<>();', detail: 'HashMap declaration' }
        ];
        
        return snippets.map(s => ({
            ...s,
            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
            range: range,
            sortText: '3' + s.label
        }));
    },
    
    // Get Python keywords and snippets
    getPythonKeywordsAndSnippets: function(range) {
        const snippets = [
            { label: 'if', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'if ${1:condition}:\n\t$0', detail: 'if statement' },
            { label: 'ifelse', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'if ${1:condition}:\n\t$2\nelse:\n\t$0', detail: 'if-else statement' },
            { label: 'elif', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'elif ${1:condition}:\n\t$0', detail: 'elif statement' },
            { label: 'for', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'for ${1:item} in ${2:iterable}:\n\t$0', detail: 'for loop' },
            { label: 'forrange', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'for ${1:i} in range(${2:n}):\n\t$0', detail: 'for range loop' },
            { label: 'while', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'while ${1:condition}:\n\t$0', detail: 'while loop' },
            { label: 'def', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'def ${1:function_name}(${2:}):\n\t$0', detail: 'function definition' },
            { label: 'class', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'class ${1:ClassName}:\n\tdef __init__(self${2:}):\n\t\t$0', detail: 'class definition' },
            { label: 'try', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'try:\n\t$1\nexcept ${2:Exception} as ${3:e}:\n\t$0', detail: 'try-except block' },
            { label: 'with', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'with ${1:expression} as ${2:var}:\n\t$0', detail: 'with statement' },
            { label: 'lambda', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'lambda ${1:x}: ${0:x}', detail: 'lambda expression' },
            { label: 'list', kind: monaco.languages.CompletionItemKind.Snippet, insertText: '[${1:item} for ${1:item} in ${2:iterable}]', detail: 'list comprehension' },
            { label: 'print', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'print($0)', detail: 'print function' },
            { label: 'input', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'input("${1:prompt}")', detail: 'input function' }
        ];
        
        return snippets.map(s => ({
            ...s,
            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
            range: range,
            sortText: '3' + s.label
        }));
    },
    
    // Get C# keywords and snippets
    getCSharpKeywordsAndSnippets: function(range) {
        const snippets = [
            { label: 'if', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'if (${1:condition})\n{\n\t$0\n}', detail: 'if statement' },
            { label: 'ifelse', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'if (${1:condition})\n{\n\t$2\n}\nelse\n{\n\t$0\n}', detail: 'if-else statement' },
            { label: 'for', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'for (int ${1:i} = 0; $1 < ${2:length}; $1++)\n{\n\t$0\n}', detail: 'for loop' },
            { label: 'foreach', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'foreach (var ${1:item} in ${2:collection})\n{\n\t$0\n}', detail: 'foreach loop' },
            { label: 'while', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'while (${1:condition})\n{\n\t$0\n}', detail: 'while loop' },
            { label: 'switch', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'switch (${1:key})\n{\n\tcase ${2:value}:\n\t\t$0\n\t\tbreak;\n\tdefault:\n\t\tbreak;\n}', detail: 'switch statement' },
            { label: 'try', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'try\n{\n\t$0\n}\ncatch (${1:Exception} ${2:ex})\n{\n\t${3:throw;}\n}', detail: 'try-catch block' },
            { label: 'class', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'public class ${1:ClassName}\n{\n\t$0\n}', detail: 'class declaration' },
            { label: 'method', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'public ${1:void} ${2:MethodName}(${3:})\n{\n\t$0\n}', detail: 'method declaration' },
            { label: 'prop', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'public ${1:string} ${2:PropertyName} { get; set; }', detail: 'auto property' },
            { label: 'cw', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'Console.WriteLine($0);', detail: 'Console.WriteLine()' },
            { label: 'cr', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'Console.ReadLine()', detail: 'Console.ReadLine()' }
        ];
        
        return snippets.map(s => ({
            ...s,
            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
            range: range,
            sortText: '3' + s.label
        }));
    },
    
    // Get JavaScript keywords and snippets
    getJavaScriptKeywordsAndSnippets: function(range) {
        const snippets = [
            { label: 'if', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'if (${1:condition}) {\n\t$0\n}', detail: 'if statement' },
            { label: 'ifelse', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'if (${1:condition}) {\n\t$2\n} else {\n\t$0\n}', detail: 'if-else statement' },
            { label: 'for', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'for (let ${1:i} = 0; $1 < ${2:length}; $1++) {\n\t$0\n}', detail: 'for loop' },
            { label: 'forof', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'for (const ${1:item} of ${2:iterable}) {\n\t$0\n}', detail: 'for-of loop' },
            { label: 'forin', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'for (const ${1:key} in ${2:object}) {\n\t$0\n}', detail: 'for-in loop' },
            { label: 'while', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'while (${1:condition}) {\n\t$0\n}', detail: 'while loop' },
            { label: 'function', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'function ${1:name}(${2:params}) {\n\t$0\n}', detail: 'function declaration' },
            { label: 'arrow', kind: monaco.languages.CompletionItemKind.Snippet, insertText: '(${1:params}) => {\n\t$0\n}', detail: 'arrow function' },
            { label: 'class', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'class ${1:ClassName} {\n\tconstructor(${2:}) {\n\t\t$0\n\t}\n}', detail: 'class declaration' },
            { label: 'try', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'try {\n\t$1\n} catch (${2:error}) {\n\t$0\n}', detail: 'try-catch block' },
            { label: 'async', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'async function ${1:name}(${2:params}) {\n\t$0\n}', detail: 'async function' },
            { label: 'log', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'console.log($0);', detail: 'console.log()' }
        ];
        
        return snippets.map(s => ({
            ...s,
            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
            range: range,
            sortText: '3' + s.label
        }));
    },
    
    // Get C/C++ keywords and snippets
    getCppKeywordsAndSnippets: function(range) {
        const snippets = [
            { label: 'if', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'if (${1:condition}) {\n\t$0\n}', detail: 'if statement' },
            { label: 'ifelse', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'if (${1:condition}) {\n\t$2\n} else {\n\t$0\n}', detail: 'if-else statement' },
            { label: 'for', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'for (int ${1:i} = 0; $1 < ${2:n}; $1++) {\n\t$0\n}', detail: 'for loop' },
            { label: 'while', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'while (${1:condition}) {\n\t$0\n}', detail: 'while loop' },
            { label: 'switch', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'switch (${1:expression}) {\n\tcase ${2:value}:\n\t\t$0\n\t\tbreak;\n\tdefault:\n\t\tbreak;\n}', detail: 'switch statement' },
            { label: 'main', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'int main() {\n\t$0\n\treturn 0;\n}', detail: 'main function' },
            { label: 'mainargs', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'int main(int argc, char *argv[]) {\n\t$0\n\treturn 0;\n}', detail: 'main with arguments' },
            { label: 'func', kind: monaco.languages.CompletionItemKind.Snippet, insertText: '${1:void} ${2:functionName}(${3:}) {\n\t$0\n}', detail: 'function declaration' },
            { label: 'struct', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'struct ${1:Name} {\n\t$0\n};', detail: 'struct declaration' },
            { label: 'class', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'class ${1:ClassName} {\npublic:\n\t$0\nprivate:\n};', detail: 'class declaration' },
            { label: 'cout', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'std::cout << $0 << std::endl;', detail: 'cout statement' },
            { label: 'cin', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'std::cin >> $0;', detail: 'cin statement' },
            { label: 'printf', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'printf("${1:%s}\\n", $0);', detail: 'printf statement' },
            { label: 'scanf', kind: monaco.languages.CompletionItemKind.Snippet, insertText: 'scanf("${1:%d}", &$0);', detail: 'scanf statement' }
        ];
        
        return snippets.map(s => ({
            ...s,
            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
            range: range,
            sortText: '3' + s.label
        }));
    },
    
    // Get signature help for method calls
    getSignatureHelp: function(textUntilPosition, language) {
        // Find the method being called
        const methodMatch = textUntilPosition.match(/(\w+)\s*\(\s*([^)]*)?$/);
        if (!methodMatch) return null;
        
        const methodName = methodMatch[1];
        const argsText = methodMatch[2] || '';
        const activeParameter = (argsText.match(/,/g) || []).length;
        
        // Check if it's a constructor call
        const constructorMatch = textUntilPosition.match(/new\s+(\w+)\s*\(/);
        const className = constructorMatch ? constructorMatch[1] : null;
        
        // Find signature from project classes
        if (className && this.projectClasses[className]) {
            const classInfo = this.projectClasses[className];
            if (classInfo.constructors && classInfo.constructors.length > 0) {
                return {
                    value: {
                        signatures: classInfo.constructors.map(c => ({
                            label: `${className}(${c.params || ''})`,
                            parameters: this.parseParameters(c.params || ''),
                            documentation: c.documentation || `Constructor for ${className}`
                        })),
                        activeSignature: 0,
                        activeParameter: activeParameter
                    }
                };
            }
        }
        
        // Find method signature from project symbols
        const methodSymbol = this.projectSymbols.find(s => 
            s.name === methodName && (s.kind === 'method' || s.kind === 'function' || s.kind === 'constructor')
        );
        
        if (methodSymbol && methodSymbol.params !== undefined) {
            return {
                value: {
                    signatures: [{
                        label: `${methodName}(${methodSymbol.params || ''})`,
                        parameters: this.parseParameters(methodSymbol.params || ''),
                        documentation: methodSymbol.documentation || ''
                    }],
                    activeSignature: 0,
                    activeParameter: activeParameter
                }
            };
        }
        
        return null;
    },
    
    // Parse parameters from a parameter string
    parseParameters: function(paramsString) {
        if (!paramsString || !paramsString.trim()) return [];
        
        return paramsString.split(',').map(param => {
            const trimmed = param.trim();
            return {
                label: trimmed,
                documentation: trimmed
            };
        });
    },
    
    // Get hover information
    getHoverInfo: function(word, language) {
        // Check project symbols
        const symbol = this.projectSymbols.find(s => s.name === word);
        if (symbol) {
            return {
                contents: [{
                    value: `**${symbol.kind}** \`${symbol.name}\`\n\n${symbol.detail || ''}\n\n_from ${symbol.file}_`
                }]
            };
        }
        
        // Check project classes
        if (this.projectClasses[word]) {
            const classInfo = this.projectClasses[word];
            let content = `**class** \`${word}\`\n\n`;
            
            if (classInfo.constructors && classInfo.constructors.length > 0) {
                content += '**Constructors:**\n';
                classInfo.constructors.forEach(c => {
                    content += `- \`${word}(${c.params || ''})\`\n`;
                });
            }
            
            if (classInfo.methods && classInfo.methods.length > 0) {
                content += '\n**Methods:**\n';
                classInfo.methods.slice(0, 5).forEach(m => {
                    content += `- \`${m.returnType || 'void'} ${m.name}(${m.params || ''})\`\n`;
                });
                if (classInfo.methods.length > 5) {
                    content += `- _...and ${classInfo.methods.length - 5} more_\n`;
                }
            }
            
            if (classInfo.fields && classInfo.fields.length > 0) {
                content += '\n**Fields:**\n';
                classInfo.fields.slice(0, 5).forEach(f => {
                    content += `- \`${f.type} ${f.name}\`\n`;
                });
            }
            
            return { contents: [{ value: content }] };
        }
        
        return null;
    },
    
    // Update project files and extract symbols
    updateProjectFiles: function(files) {
        // files is an object: { "src/Main.java": "content...", "src/Student.java": "content..." }
        this.projectFiles = files || {};
        this.extractAllSymbols();
        console.log('[PeP IDE] Updated project files:', Object.keys(this.projectFiles));
        console.log('[PeP IDE] Extracted symbols:', this.projectSymbols.length);
        console.log('[PeP IDE] Project classes:', Object.keys(this.projectClasses));
    },
    
    // Add or update a single file
    updateFile: function(filePath, content) {
        this.projectFiles[filePath] = content;
        this.extractAllSymbols();
    },
    
    // Extract symbols from all project files
    extractAllSymbols: function() {
        this.projectSymbols = [];
        this.projectClasses = {}; // Reset class info
        
        for (const filePath in this.projectFiles) {
            const content = this.projectFiles[filePath];
            const fileName = filePath.split('/').pop();
            const extension = fileName.split('.').pop().toLowerCase();
            
            let symbols = [];
            let classInfo = {};
            
            switch(extension) {
                case 'java':
                    const javaResult = this.extractJavaSymbolsEnhanced(content, fileName);
                    symbols = javaResult.symbols;
                    classInfo = javaResult.classes;
                    break;
                case 'py':
                    const pyResult = this.extractPythonSymbolsEnhanced(content, fileName);
                    symbols = pyResult.symbols;
                    classInfo = pyResult.classes;
                    break;
                case 'cs':
                    symbols = this.extractCSharpSymbols(content, fileName);
                    break;
                case 'js':
                case 'ts':
                    symbols = this.extractJavaScriptSymbols(content, fileName);
                    break;
                case 'cpp':
                case 'c':
                case 'h':
                    symbols = this.extractCppSymbols(content, fileName);
                    break;
            }
            
            this.projectSymbols = this.projectSymbols.concat(symbols);
            
            // Merge class info
            for (const className in classInfo) {
                this.projectClasses[className] = classInfo[className];
            }
        }
    },
    
    // Enhanced Java symbol extraction with class member tracking
    extractJavaSymbolsEnhanced: function(content, fileName) {
        const symbols = [];
        const classes = {};
        const lines = content.split('\n');
        
        // First pass: Find all classes and their positions
        const classRegex = /(?:public\s+|private\s+|protected\s+)?(?:abstract\s+|final\s+)?(?:class|interface|enum)\s+(\w+)(?:\s+extends\s+(\w+))?(?:\s+implements\s+([^{]+))?/g;
        let match;
        const classPositions = [];
        
        while ((match = classRegex.exec(content)) !== null) {
            const className = match[1];
            const extendsClass = match[2] || null;
            const implementsInterfaces = match[3] ? match[3].split(',').map(s => s.trim()) : [];
            
            symbols.push({
                name: className,
                kind: content.includes('interface ' + className) ? 'interface' : 
                      content.includes('enum ' + className) ? 'enum' : 'class',
                file: fileName,
                detail: `class ${className}${extendsClass ? ' extends ' + extendsClass : ''}`,
                insertText: className
            });
            
            classes[className] = {
                name: className,
                extends: extendsClass,
                implements: implementsInterfaces,
                constructors: [],
                methods: [],
                fields: [],
                file: fileName
            };
            
            classPositions.push({
                name: className,
                startIndex: match.index
            });
        }
        
        // For each class, extract its members
        classPositions.forEach((classPos, idx) => {
            const className = classPos.name;
            const startIdx = classPos.startIndex;
            const endIdx = idx < classPositions.length - 1 ? classPositions[idx + 1].startIndex : content.length;
            const classContent = content.substring(startIdx, endIdx);
            
            // Extract constructors
            const constructorRegex = new RegExp(`(?:public|private|protected)\\s+${className}\\s*\\(([^)]*)\\)`, 'g');
            while ((match = constructorRegex.exec(classContent)) !== null) {
                const params = match[1].trim();
                classes[className].constructors.push({
                    name: className,
                    params: params,
                    documentation: `Constructor for ${className}`
                });
                
                symbols.push({
                    name: className,
                    kind: 'constructor',
                    file: fileName,
                    detail: `new ${className}(${params})`,
                    insertText: `new ${className}(${params ? '$0' : ''})`,
                    params: params,
                    documentation: `Constructor for ${className}`
                });
            }
            
            // Extract methods
            const methodRegex = /(?:public|private|protected)\s+(?:static\s+)?(?:final\s+)?(\w+(?:<[^>]+>)?)\s+(\w+)\s*\(([^)]*)\)/g;
            while ((match = methodRegex.exec(classContent)) !== null) {
                const returnType = match[1];
                const methodName = match[2];
                const params = match[3].trim();
                
                if (!['if', 'while', 'for', 'switch', className].includes(methodName)) {
                    classes[className].methods.push({
                        name: methodName,
                        returnType: returnType,
                        params: params,
                        documentation: `${returnType} ${methodName}(${params})`
                    });
                    
                    symbols.push({
                        name: methodName,
                        kind: 'method',
                        file: fileName,
                        detail: `${returnType} ${methodName}(${params}) - ${className}`,
                        insertText: methodName + (params ? '($0)' : '()'),
                        params: params,
                        returnType: returnType,
                        className: className,
                        documentation: `Method from ${className}`
                    });
                }
            }
            
            // Extract fields
            const fieldRegex = /(?:public|private|protected)\s+(?:static\s+)?(?:final\s+)?(\w+(?:<[^>]+>)?)\s+(\w+)\s*[;=]/g;
            while ((match = fieldRegex.exec(classContent)) !== null) {
                const type = match[1];
                const name = match[2];
                
                if (!['class', 'interface', 'enum', 'void', 'return', 'new', 'if', 'for', 'while'].includes(type)) {
                    classes[className].fields.push({
                        name: name,
                        type: type
                    });
                    
                    symbols.push({
                        name: name,
                        kind: classContent.includes('final ' + type) ? 'constant' : 'field',
                        file: fileName,
                        detail: `${type} ${name} - ${className}`,
                        insertText: name,
                        className: className
                    });
                }
            }
        });
        
        // If no classes found, still extract top-level elements
        if (classPositions.length === 0) {
            // Extract methods at top level
            const methodRegex = /(?:public|private|protected)\s+(?:static\s+)?(?:final\s+)?(\w+(?:<[^>]+>)?)\s+(\w+)\s*\(([^)]*)\)/g;
            while ((match = methodRegex.exec(content)) !== null) {
                const returnType = match[1];
                const methodName = match[2];
                const params = match[3].trim();
                
                if (!['if', 'while', 'for', 'switch'].includes(methodName)) {
                    symbols.push({
                        name: methodName,
                        kind: 'method',
                        file: fileName,
                        detail: `${returnType} ${methodName}(${params})`,
                        insertText: methodName + (params ? '($0)' : '()'),
                        params: params
                    });
                }
            }
        }
        
        return { symbols, classes };
    },
    
    // Enhanced Python symbol extraction
    extractPythonSymbolsEnhanced: function(content, fileName) {
        const symbols = [];
        const classes = {};
        
        // Find all classes
        const classRegex = /^class\s+(\w+)(?:\(([^)]*)\))?:/gm;
        let match;
        const classPositions = [];
        
        while ((match = classRegex.exec(content)) !== null) {
            const className = match[1];
            const parentClass = match[2] || null;
            
            symbols.push({
                name: className,
                kind: 'class',
                file: fileName,
                detail: `class ${className}${parentClass ? '(' + parentClass + ')' : ''}`,
                insertText: className
            });
            
            classes[className] = {
                name: className,
                extends: parentClass,
                constructors: [],
                methods: [],
                fields: [],
                file: fileName
            };
            
            classPositions.push({
                name: className,
                startIndex: match.index
            });
        }
        
        // Extract class members
        classPositions.forEach((classPos, idx) => {
            const className = classPos.name;
            const startIdx = classPos.startIndex;
            const endIdx = idx < classPositions.length - 1 ? classPositions[idx + 1].startIndex : content.length;
            const classContent = content.substring(startIdx, endIdx);
            
            // Find __init__ (constructor)
            const initMatch = classContent.match(/def\s+__init__\s*\(self(?:,\s*)?([^)]*)\)/);
            if (initMatch) {
                const params = initMatch[1].trim();
                classes[className].constructors.push({
                    name: className,
                    params: params,
                    documentation: `Constructor for ${className}`
                });
                
                symbols.push({
                    name: className,
                    kind: 'constructor',
                    file: fileName,
                    detail: `${className}(${params})`,
                    insertText: `${className}(${params ? '$0' : ''})`,
                    params: params
                });
            }
            
            // Find methods
            const methodRegex = /def\s+(\w+)\s*\(self(?:,\s*)?([^)]*)\)/g;
            while ((match = methodRegex.exec(classContent)) !== null) {
                const methodName = match[1];
                const params = match[2].trim();
                
                if (methodName !== '__init__') {
                    classes[className].methods.push({
                        name: methodName,
                        params: params,
                        documentation: `def ${methodName}(${params})`
                    });
                    
                    symbols.push({
                        name: methodName,
                        kind: 'method',
                        file: fileName,
                        detail: `def ${methodName}(${params}) - ${className}`,
                        insertText: methodName + (params ? '($0)' : '()'),
                        params: params,
                        className: className
                    });
                }
            }
            
            // Find instance fields (self.xxx = )
            const fieldRegex = /self\.(\w+)\s*=/g;
            const foundFields = new Set();
            while ((match = fieldRegex.exec(classContent)) !== null) {
                const fieldName = match[1];
                if (!foundFields.has(fieldName)) {
                    foundFields.add(fieldName);
                    classes[className].fields.push({
                        name: fieldName,
                        type: 'any'
                    });
                }
            }
        });
        
        // Extract top-level functions
        const funcRegex = /^def\s+(\w+)\s*\(([^)]*)\):/gm;
        while ((match = funcRegex.exec(content)) !== null) {
            const funcName = match[1];
            const params = match[2].trim();
            
            // Skip if this is inside a class (indented)
            const lineStart = content.lastIndexOf('\n', match.index) + 1;
            const indent = match.index - lineStart;
            
            if (indent === 0 && funcName !== '__init__') {
                symbols.push({
                    name: funcName,
                    kind: 'function',
                    file: fileName,
                    detail: `def ${funcName}(${params})`,
                    insertText: funcName + (params ? '($0)' : '()'),
                    params: params
                });
            }
        }
        
        // Extract global constants
        const constRegex = /^([A-Z_][A-Z0-9_]*)\s*=/gm;
        while ((match = constRegex.exec(content)) !== null) {
            symbols.push({
                name: match[1],
                kind: 'constant',
                file: fileName,
                detail: `constant ${match[1]}`,
                insertText: match[1]
            });
        }
        
        return { symbols, classes };
    },
    
    // Extract C# symbols
    extractCSharpSymbols: function(content, fileName) {
        const symbols = [];
        
        // Class/Interface/Struct/Enum detection
        const classRegex = /(?:public|private|protected|internal)\s+(?:partial\s+)?(?:static\s+)?(?:abstract\s+|sealed\s+)?(?:class|interface|struct|enum)\s+(\w+)/g;
        let match;
        while ((match = classRegex.exec(content)) !== null) {
            symbols.push({
                name: match[1],
                kind: content.includes('interface ' + match[1]) ? 'interface' : 
                      content.includes('enum ' + match[1]) ? 'enum' : 'class',
                file: fileName,
                detail: `type ${match[1]}`,
                insertText: match[1]
            });
        }
        
        // Method detection
        const methodRegex = /(?:public|private|protected|internal)\s+(?:static\s+)?(?:virtual\s+|override\s+|async\s+)?(?:\w+(?:<[^>]+>)?)\s+(\w+)\s*\(([^)]*)\)/g;
        while ((match = methodRegex.exec(content)) !== null) {
            const methodName = match[1];
            const params = match[2];
            if (!['if', 'while', 'for', 'switch', 'foreach', 'using'].includes(methodName)) {
                symbols.push({
                    name: methodName,
                    kind: 'method',
                    file: fileName,
                    detail: `method ${methodName}(${params})`,
                    insertText: methodName + '($0)'
                });
            }
        }
        
        // Property detection
        const propRegex = /(?:public|private|protected|internal)\s+(?:static\s+)?(\w+(?:<[^>]+>)?)\s+(\w+)\s*{\s*get/g;
        while ((match = propRegex.exec(content)) !== null) {
            symbols.push({
                name: match[2],
                kind: 'property',
                file: fileName,
                detail: `${match[1]} ${match[2]}`,
                insertText: match[2]
            });
        }
        
        return symbols;
    },
    
    // Extract JavaScript/TypeScript symbols
    extractJavaScriptSymbols: function(content, fileName) {
        const symbols = [];
        
        // Class detection
        const classRegex = /(?:export\s+)?class\s+(\w+)/g;
        let match;
        while ((match = classRegex.exec(content)) !== null) {
            symbols.push({
                name: match[1],
                kind: 'class',
                file: fileName,
                detail: `class ${match[1]}`,
                insertText: match[1]
            });
        }
        
        // Function detection
        const funcRegex = /(?:export\s+)?(?:async\s+)?function\s+(\w+)\s*\(([^)]*)\)/g;
        while ((match = funcRegex.exec(content)) !== null) {
            symbols.push({
                name: match[1],
                kind: 'function',
                file: fileName,
                detail: `function ${match[1]}(${match[2]})`,
                insertText: match[1] + '($0)'
            });
        }
        
        // Arrow function / const function detection
        const arrowRegex = /(?:export\s+)?(?:const|let|var)\s+(\w+)\s*=\s*(?:async\s+)?(?:\([^)]*\)|[^=])\s*=>/g;
        while ((match = arrowRegex.exec(content)) !== null) {
            symbols.push({
                name: match[1],
                kind: 'function',
                file: fileName,
                detail: `const ${match[1]}`,
                insertText: match[1] + '($0)'
            });
        }
        
        // Interface detection (TypeScript)
        const interfaceRegex = /(?:export\s+)?interface\s+(\w+)/g;
        while ((match = interfaceRegex.exec(content)) !== null) {
            symbols.push({
                name: match[1],
                kind: 'interface',
                file: fileName,
                detail: `interface ${match[1]}`,
                insertText: match[1]
            });
        }
        
        return symbols;
    },
    
    // Extract C/C++ symbols
    extractCppSymbols: function(content, fileName) {
        const symbols = [];
        
        // Class/Struct detection
        const classRegex = /(?:class|struct)\s+(\w+)/g;
        let match;
        while ((match = classRegex.exec(content)) !== null) {
            symbols.push({
                name: match[1],
                kind: 'class',
                file: fileName,
                detail: `class ${match[1]}`,
                insertText: match[1]
            });
        }
        
        // Function detection
        const funcRegex = /(?:[\w*&]+\s+)+(\w+)\s*\(([^)]*)\)\s*(?:const\s*)?(?:override\s*)?(?:=\s*0\s*)?[{;]/g;
        while ((match = funcRegex.exec(content)) !== null) {
            const funcName = match[1];
            if (!['if', 'while', 'for', 'switch', 'return', 'else'].includes(funcName)) {
                symbols.push({
                    name: funcName,
                    kind: 'function',
                    file: fileName,
                    detail: `${funcName}(${match[2]})`,
                    insertText: funcName + '($0)'
                });
            }
        }
        
        // Macro/Define detection
        const defineRegex = /#define\s+(\w+)/g;
        while ((match = defineRegex.exec(content)) !== null) {
            symbols.push({
                name: match[1],
                kind: 'constant',
                file: fileName,
                detail: `#define ${match[1]}`,
                insertText: match[1]
            });
        }
        
        return symbols;
    },
    
    // ========================================
    // REAL-TIME DIAGNOSTICS SYSTEM
    // ========================================
    
    diagnosticsTimers: {}, // Debounce timers per editor
    
    // Run diagnostics on content change (debounced)
    runDiagnostics: function(elementId, content, language) {
        // Clear existing timer
        if (this.diagnosticsTimers[elementId]) {
            clearTimeout(this.diagnosticsTimers[elementId]);
        }
        
        const self = this;
        // Debounce: wait 500ms after typing stops
        this.diagnosticsTimers[elementId] = setTimeout(function() {
            const diagnostics = self.analyzeSyntax(content, language);
            self.applyDiagnostics(elementId, diagnostics);
        }, 500);
    },
    
    // Apply diagnostics to editor
    applyDiagnostics: function(elementId, diagnostics) {
        const editor = this.editors[elementId];
        if (!editor) return;
        
        const markers = diagnostics.map(d => ({
            severity: d.severity === 'error' ? monaco.MarkerSeverity.Error :
                      d.severity === 'warning' ? monaco.MarkerSeverity.Warning :
                      d.severity === 'info' ? monaco.MarkerSeverity.Info :
                      monaco.MarkerSeverity.Hint,
            startLineNumber: d.line,
            startColumn: d.column || 1,
            endLineNumber: d.endLine || d.line,
            endColumn: d.endColumn || 1000,
            message: d.message,
            source: 'PeP'
        }));
        
        monaco.editor.setModelMarkers(editor.getModel(), 'pep-diagnostics', markers);
    },
    
    // Main syntax analyzer - dispatches to language-specific analyzers
    analyzeSyntax: function(content, language) {
        const diagnostics = [];
        
        switch(language) {
            case 'java':
                return this.analyzeJava(content);
            case 'python':
                return this.analyzePython(content);
            case 'csharp':
                return this.analyzeCSharp(content);
            case 'javascript':
            case 'typescript':
                return this.analyzeJavaScript(content);
            case 'cpp':
            case 'c':
                return this.analyzeCpp(content);
            default:
                return this.analyzeGeneric(content);
        }
    },
    
    // ========================================
    // JAVA DIAGNOSTICS
    // ========================================
    analyzeJava: function(content) {
        const diagnostics = [];
        const lines = content.split('\n');
        
        let braceCount = 0;
        let parenCount = 0;
        let bracketCount = 0;
        let inString = false;
        let inChar = false;
        let inMultiComment = false;
        let classCount = 0;
        let hasMainMethod = false;
        
        lines.forEach((line, index) => {
            const lineNum = index + 1;
            const trimmed = line.trim();
            
            // Skip empty lines
            if (!trimmed) return;
            
            // Track multi-line comments
            if (trimmed.includes('/*')) inMultiComment = true;
            if (trimmed.includes('*/')) inMultiComment = false;
            if (inMultiComment || trimmed.startsWith('//')) return;
            
            // Check for common Java errors
            
            // Missing semicolon (not for control structures, class/method declarations)
            if (!trimmed.endsWith('{') && !trimmed.endsWith('}') && !trimmed.endsWith(';') &&
                !trimmed.endsWith(':') && !trimmed.startsWith('//') && !trimmed.startsWith('*') &&
                !trimmed.startsWith('import') && !trimmed.startsWith('package') &&
                !trimmed.match(/^\s*(if|else|for|while|switch|try|catch|finally|do)\s*[\({]?/) &&
                !trimmed.match(/^(public|private|protected|class|interface|enum|void|static)/) &&
                trimmed.length > 0 && !trimmed.startsWith('@')) {
                // Check if it looks like a statement
                if (trimmed.match(/[a-zA-Z0-9_)\]"']\s*$/) && !trimmed.endsWith(',')) {
                    diagnostics.push({
                        line: lineNum,
                        column: line.length,
                        severity: 'error',
                        message: 'Missing semicolon'
                    });
                }
            }
            
            // Count braces
            for (let i = 0; i < line.length; i++) {
                const char = line[i];
                if (char === '"' && line[i-1] !== '\\') inString = !inString;
                if (char === "'" && line[i-1] !== '\\') inChar = !inChar;
                if (inString || inChar) continue;
                
                if (char === '{') braceCount++;
                if (char === '}') braceCount--;
                if (char === '(') parenCount++;
                if (char === ')') parenCount--;
                if (char === '[') bracketCount++;
                if (char === ']') bracketCount--;
            }
            
            // Check for unmatched braces on this line
            if (braceCount < 0) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'error',
                    message: 'Unexpected closing brace'
                });
                braceCount = 0;
            }
            
            // Check class declaration
            if (trimmed.match(/\bclass\s+\w+/)) {
                classCount++;
            }
            
            // Check for main method
            if (trimmed.includes('public static void main')) {
                hasMainMethod = true;
            }
            
            // Check for common typos/errors
            if (trimmed.match(/\bSystem\.out\.printl\b[^n]/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'error',
                    message: "Did you mean 'System.out.println'?"
                });
            }
            
            if (trimmed.match(/\bpubic\b/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'error',
                    message: "Did you mean 'public'?"
                });
            }
            
            if (trimmed.match(/\bvodi\b|\bviod\b/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'error',
                    message: "Did you mean 'void'?"
                });
            }
            
            // Check for string comparison with ==
            if (trimmed.match(/==\s*".*"/) || trimmed.match(/".*"\s*==/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'warning',
                    message: 'Use .equals() for string comparison instead of =='
                });
            }
            
            // Empty catch block
            if (trimmed.match(/catch\s*\([^)]+\)\s*\{\s*\}/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'warning',
                    message: 'Empty catch block - consider logging the exception'
                });
            }
            
            // TODO comment
            if (trimmed.includes('TODO')) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'info',
                    message: 'TODO comment found'
                });
            }
            
            // FIXME comment
            if (trimmed.includes('FIXME')) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'warning',
                    message: 'FIXME comment found'
                });
            }
        });
        
        // Check unmatched braces at end
        if (braceCount > 0) {
            diagnostics.push({
                line: lines.length,
                severity: 'error',
                message: `Missing ${braceCount} closing brace(s)`
            });
        }
        
        if (parenCount > 0) {
            diagnostics.push({
                line: lines.length,
                severity: 'error',
                message: `Missing ${parenCount} closing parenthesis`
            });
        }
        
        if (bracketCount > 0) {
            diagnostics.push({
                line: lines.length,
                severity: 'error',
                message: `Missing ${bracketCount} closing bracket(s)`
            });
        }
        
        return diagnostics;
    },
    
    // ========================================
    // PYTHON DIAGNOSTICS
    // ========================================
    analyzePython: function(content) {
        const diagnostics = [];
        const lines = content.split('\n');
        
        let expectedIndent = 0;
        let inMultiString = false;
        
        lines.forEach((line, index) => {
            const lineNum = index + 1;
            const trimmed = line.trim();
            
            // Skip empty lines and comments
            if (!trimmed || trimmed.startsWith('#')) return;
            
            // Track multi-line strings
            const tripleQuoteCount = (line.match(/"""/g) || []).length + (line.match(/'''/g) || []).length;
            if (tripleQuoteCount % 2 === 1) inMultiString = !inMultiString;
            if (inMultiString) return;
            
            // Check indentation (should be multiple of 4)
            const indent = line.match(/^(\s*)/)[1].length;
            if (indent % 4 !== 0 && line.trim().length > 0) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'warning',
                    message: 'Inconsistent indentation (should be multiple of 4 spaces)'
                });
            }
            
            // Check for tabs mixed with spaces
            if (line.match(/^ *\t/) || line.match(/^\t* /)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'warning',
                    message: 'Mixed tabs and spaces in indentation'
                });
            }
            
            // Missing colon after control statements
            if (trimmed.match(/^(if|elif|else|for|while|def|class|try|except|finally|with)\b/) &&
                !trimmed.endsWith(':') && !trimmed.includes(':')) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'error',
                    message: 'Missing colon at end of statement'
                });
            }
            
            // Common typos
            if (trimmed.match(/\bpritn\b/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'error',
                    message: "Did you mean 'print'?"
                });
            }
            
            if (trimmed.match(/\bdefn\b/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'error',
                    message: "Did you mean 'def'?"
                });
            }
            
            // Using == with None
            if (trimmed.match(/==\s*None/) || trimmed.match(/None\s*==/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'warning',
                    message: "Use 'is None' instead of '== None'"
                });
            }
            
            // Mutable default argument
            if (trimmed.match(/def\s+\w+\s*\([^)]*=\s*(\[\]|\{\})/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'warning',
                    message: 'Mutable default argument - can cause unexpected behavior'
                });
            }
            
            // TODO/FIXME
            if (trimmed.includes('TODO')) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'info',
                    message: 'TODO comment found'
                });
            }
            
            if (trimmed.includes('FIXME')) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'warning',
                    message: 'FIXME comment found'
                });
            }
            
            // Check for print without parentheses (Python 2 style)
            if (trimmed.match(/^print\s+[^(]/) && !trimmed.match(/^print\s*$/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'error',
                    message: "Python 3 requires parentheses: print(...)"
                });
            }
        });
        
        // Check bracket matching
        let parenCount = 0, bracketCount = 0, braceCount = 0;
        content.split('').forEach(char => {
            if (char === '(') parenCount++;
            if (char === ')') parenCount--;
            if (char === '[') bracketCount++;
            if (char === ']') bracketCount--;
            if (char === '{') braceCount++;
            if (char === '}') braceCount--;
        });
        
        if (parenCount !== 0) {
            diagnostics.push({
                line: lines.length,
                severity: 'error',
                message: `Unmatched parentheses (${parenCount > 0 ? 'missing )' : 'extra )'})`
            });
        }
        
        if (bracketCount !== 0) {
            diagnostics.push({
                line: lines.length,
                severity: 'error',
                message: `Unmatched brackets (${bracketCount > 0 ? 'missing ]' : 'extra ]'})`
            });
        }
        
        return diagnostics;
    },
    
    // ========================================
    // C# DIAGNOSTICS
    // ========================================
    analyzeCSharp: function(content) {
        const diagnostics = [];
        const lines = content.split('\n');
        
        let braceCount = 0;
        let inString = false;
        let inMultiComment = false;
        
        lines.forEach((line, index) => {
            const lineNum = index + 1;
            const trimmed = line.trim();
            
            if (!trimmed) return;
            
            // Track comments
            if (trimmed.includes('/*')) inMultiComment = true;
            if (trimmed.includes('*/')) inMultiComment = false;
            if (inMultiComment || trimmed.startsWith('//')) return;
            
            // Missing semicolon
            if (!trimmed.endsWith('{') && !trimmed.endsWith('}') && !trimmed.endsWith(';') &&
                !trimmed.endsWith(':') && !trimmed.startsWith('//') &&
                !trimmed.match(/^\s*(if|else|for|foreach|while|switch|try|catch|finally|do|using|namespace|class|interface|struct|enum)\s*[\({]?/) &&
                !trimmed.match(/^(public|private|protected|internal|static|abstract|virtual|override|async)/) &&
                !trimmed.startsWith('#') && !trimmed.startsWith('[') &&
                trimmed.length > 0) {
                if (trimmed.match(/[a-zA-Z0-9_)\]"']\s*$/) && !trimmed.endsWith(',')) {
                    diagnostics.push({
                        line: lineNum,
                        column: line.length,
                        severity: 'error',
                        message: 'Missing semicolon'
                    });
                }
            }
            
            // Count braces
            for (let char of line) {
                if (char === '"') inString = !inString;
                if (inString) continue;
                if (char === '{') braceCount++;
                if (char === '}') braceCount--;
            }
            
            // Common typos
            if (trimmed.match(/\bConsoel\b/i)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'error',
                    message: "Did you mean 'Console'?"
                });
            }
            
            // String comparison
            if (trimmed.match(/==\s*".*"/) || trimmed.match(/".*"\s*==/)) {
                if (!trimmed.includes('string.') && !trimmed.includes('String.')) {
                    diagnostics.push({
                        line: lineNum,
                        severity: 'info',
                        message: 'Consider using string.Equals() for culture-aware comparison'
                    });
                }
            }
            
            // TODO/FIXME
            if (trimmed.includes('TODO')) {
                diagnostics.push({ line: lineNum, severity: 'info', message: 'TODO comment found' });
            }
            if (trimmed.includes('FIXME')) {
                diagnostics.push({ line: lineNum, severity: 'warning', message: 'FIXME comment found' });
            }
        });
        
        if (braceCount > 0) {
            diagnostics.push({
                line: lines.length,
                severity: 'error',
                message: `Missing ${braceCount} closing brace(s)`
            });
        }
        
        return diagnostics;
    },
    
    // ========================================
    // JAVASCRIPT/TYPESCRIPT DIAGNOSTICS
    // ========================================
    analyzeJavaScript: function(content) {
        const diagnostics = [];
        const lines = content.split('\n');
        
        let braceCount = 0;
        let parenCount = 0;
        let bracketCount = 0;
        
        lines.forEach((line, index) => {
            const lineNum = index + 1;
            const trimmed = line.trim();
            
            if (!trimmed || trimmed.startsWith('//')) return;
            
            // Count brackets
            for (let char of line) {
                if (char === '{') braceCount++;
                if (char === '}') braceCount--;
                if (char === '(') parenCount++;
                if (char === ')') parenCount--;
                if (char === '[') bracketCount++;
                if (char === ']') bracketCount--;
            }
            
            // Using var instead of let/const
            if (trimmed.match(/\bvar\s+\w+/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'warning',
                    message: "Consider using 'let' or 'const' instead of 'var'"
                });
            }
            
            // == instead of ===
            if (trimmed.match(/[^=!]==[^=]/) && !trimmed.includes('===')) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'warning',
                    message: "Consider using '===' for strict equality"
                });
            }
            
            // != instead of !==
            if (trimmed.match(/!=[^=]/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'warning',
                    message: "Consider using '!==' for strict inequality"
                });
            }
            
            // console.log left in code
            if (trimmed.match(/console\.(log|warn|error)\s*\(/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'info',
                    message: 'Console statement found - remember to remove for production'
                });
            }
            
            // Common typos
            if (trimmed.match(/\bfunciton\b/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'error',
                    message: "Did you mean 'function'?"
                });
            }
            
            if (trimmed.match(/\bretrun\b/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'error',
                    message: "Did you mean 'return'?"
                });
            }
            
            // TODO/FIXME
            if (trimmed.includes('TODO')) {
                diagnostics.push({ line: lineNum, severity: 'info', message: 'TODO comment found' });
            }
            if (trimmed.includes('FIXME')) {
                diagnostics.push({ line: lineNum, severity: 'warning', message: 'FIXME comment found' });
            }
        });
        
        if (braceCount !== 0) {
            diagnostics.push({
                line: lines.length,
                severity: 'error',
                message: `Unmatched braces (${braceCount > 0 ? 'missing }' : 'extra }'})`
            });
        }
        
        if (parenCount !== 0) {
            diagnostics.push({
                line: lines.length,
                severity: 'error',
                message: `Unmatched parentheses`
            });
        }
        
        return diagnostics;
    },
    
    // ========================================
    // C/C++ DIAGNOSTICS
    // ========================================
    analyzeCpp: function(content) {
        const diagnostics = [];
        const lines = content.split('\n');
        
        let braceCount = 0;
        let hasInclude = false;
        let hasMain = false;
        
        lines.forEach((line, index) => {
            const lineNum = index + 1;
            const trimmed = line.trim();
            
            if (!trimmed || trimmed.startsWith('//')) return;
            
            // Track includes
            if (trimmed.startsWith('#include')) {
                hasInclude = true;
                
                // Check include syntax
                if (!trimmed.match(/#include\s*[<"][^>"]+[>"]/)) {
                    diagnostics.push({
                        line: lineNum,
                        severity: 'error',
                        message: 'Invalid #include syntax'
                    });
                }
            }
            
            // Check for main
            if (trimmed.match(/int\s+main\s*\(/)) {
                hasMain = true;
            }
            
            // Missing semicolon
            if (!trimmed.endsWith('{') && !trimmed.endsWith('}') && !trimmed.endsWith(';') &&
                !trimmed.endsWith(':') && !trimmed.endsWith(',') &&
                !trimmed.startsWith('#') && !trimmed.startsWith('//') &&
                !trimmed.match(/^\s*(if|else|for|while|switch|do)\s*[\({]?/) &&
                trimmed.length > 0) {
                if (trimmed.match(/[a-zA-Z0-9_)\]"']\s*$/)) {
                    diagnostics.push({
                        line: lineNum,
                        severity: 'error',
                        message: 'Missing semicolon'
                    });
                }
            }
            
            // Count braces
            for (let char of line) {
                if (char === '{') braceCount++;
                if (char === '}') braceCount--;
            }
            
            // Common typos
            if (trimmed.match(/\bpritnf\b/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'error',
                    message: "Did you mean 'printf'?"
                });
            }
            
            if (trimmed.match(/\bscnaf\b/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'error',
                    message: "Did you mean 'scanf'?"
                });
            }
            
            // Using gets (unsafe)
            if (trimmed.match(/\bgets\s*\(/)) {
                diagnostics.push({
                    line: lineNum,
                    severity: 'warning',
                    message: "gets() is unsafe - use fgets() instead"
                });
            }
            
            // TODO/FIXME
            if (trimmed.includes('TODO')) {
                diagnostics.push({ line: lineNum, severity: 'info', message: 'TODO comment found' });
            }
            if (trimmed.includes('FIXME')) {
                diagnostics.push({ line: lineNum, severity: 'warning', message: 'FIXME comment found' });
            }
        });
        
        if (braceCount > 0) {
            diagnostics.push({
                line: lines.length,
                severity: 'error',
                message: `Missing ${braceCount} closing brace(s)`
            });
        }
        
        return diagnostics;
    },
    
    // ========================================
    // GENERIC DIAGNOSTICS (for any language)
    // ========================================
    analyzeGeneric: function(content) {
        const diagnostics = [];
        const lines = content.split('\n');
        
        let braceCount = 0;
        let parenCount = 0;
        let bracketCount = 0;
        
        lines.forEach((line, index) => {
            const lineNum = index + 1;
            const trimmed = line.trim();
            
            // Count brackets
            for (let char of line) {
                if (char === '{') braceCount++;
                if (char === '}') braceCount--;
                if (char === '(') parenCount++;
                if (char === ')') parenCount--;
                if (char === '[') bracketCount++;
                if (char === ']') bracketCount--;
            }
            
            // TODO/FIXME
            if (trimmed.includes('TODO')) {
                diagnostics.push({ line: lineNum, severity: 'info', message: 'TODO comment found' });
            }
            if (trimmed.includes('FIXME')) {
                diagnostics.push({ line: lineNum, severity: 'warning', message: 'FIXME comment found' });
            }
        });
        
        if (braceCount !== 0) {
            diagnostics.push({
                line: lines.length,
                severity: 'error',
                message: `Unmatched braces`
            });
        }
        
        if (parenCount !== 0) {
            diagnostics.push({
                line: lines.length,
                severity: 'error',
                message: `Unmatched parentheses`
            });
        }
        
        if (bracketCount !== 0) {
            diagnostics.push({
                line: lines.length,
                severity: 'error',
                message: `Unmatched brackets`
            });
        }
        
        return diagnostics;
    },

    createEditor: function (elementId, content, language, theme, readOnly, dotNetRef) {
        // Wait for Monaco to be available
        if (typeof monaco === 'undefined') {
            console.error('Monaco editor not loaded');
            return;
        }

        const container = document.getElementById(elementId);
        if (!container) {
            console.error('Editor container not found:', elementId);
            return;
        }

        // Dispose existing editor if any
        if (this.editors[elementId]) {
            this.editors[elementId].dispose();
        }

        // Create editor with context menu enabled for copy/paste
        const editor = monaco.editor.create(container, {
            value: content || '',
            language: language || 'plaintext',
            theme: theme || 'vs-dark',
            readOnly: readOnly || false,
            automaticLayout: true,
            minimap: { enabled: true },
            fontSize: 14,
            lineNumbers: 'on',
            scrollBeyondLastLine: false,
            wordWrap: 'on',
            tabSize: 4,
            insertSpaces: true,
            folding: true,
            renderWhitespace: 'selection',
            bracketPairColorization: { enabled: true },
            guides: {
                bracketPairs: true,
                indentation: true
            },
            suggest: {
                showKeywords: true,
                showSnippets: true
            },
            quickSuggestions: {
                other: true,
                comments: false,
                strings: false
            },
            // Enable context menu for copy/paste
            contextmenu: true,
            // Clipboard permissions
            copyWithSyntaxHighlighting: true
        });

        // Store editor instance
        this.editors[elementId] = editor;

        // Add custom context menu actions (these don't trigger violations)
        editor.addAction({
            id: 'pep-copy',
            label: 'Copy',
            keybindings: [monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyC],
            contextMenuGroupId: 'cutcopypaste',
            contextMenuOrder: 1,
            run: function(ed) {
                const selection = ed.getSelection();
                const text = ed.getModel().getValueInRange(selection);
                if (text) {
                    navigator.clipboard.writeText(text).catch(err => {
                        console.log('Copy failed, using fallback');
                        // Fallback for older browsers
                        ed.trigger('keyboard', 'editor.action.clipboardCopyAction');
                    });
                }
            }
        });

        editor.addAction({
            id: 'pep-cut',
            label: 'Cut',
            keybindings: [monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyX],
            contextMenuGroupId: 'cutcopypaste',
            contextMenuOrder: 2,
            run: function(ed) {
                if (readOnly) return;
                const selection = ed.getSelection();
                const text = ed.getModel().getValueInRange(selection);
                if (text) {
                    navigator.clipboard.writeText(text).then(() => {
                        ed.executeEdits('cut', [{
                            range: selection,
                            text: '',
                            forceMoveMarkers: true
                        }]);
                    }).catch(err => {
                        console.log('Cut failed, using fallback');
                        ed.trigger('keyboard', 'editor.action.clipboardCutAction');
                    });
                }
            }
        });

        editor.addAction({
            id: 'pep-paste',
            label: 'Paste',
            keybindings: [monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyV],
            contextMenuGroupId: 'cutcopypaste',
            contextMenuOrder: 3,
            run: function(ed) {
                if (readOnly) return;
                navigator.clipboard.readText().then(text => {
                    if (text) {
                        const selection = ed.getSelection();
                        ed.executeEdits('paste', [{
                            range: selection,
                            text: text,
                            forceMoveMarkers: true
                        }]);
                    }
                }).catch(err => {
                    console.log('Paste failed - clipboard access denied');
                    // Try fallback
                    ed.trigger('keyboard', 'editor.action.clipboardPasteAction');
                });
            }
        });

        editor.addAction({
            id: 'pep-selectall',
            label: 'Select All',
            keybindings: [monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyA],
            contextMenuGroupId: 'cutcopypaste',
            contextMenuOrder: 4,
            run: function(ed) {
                const model = ed.getModel();
                const lineCount = model.getLineCount();
                const lastLineLength = model.getLineContent(lineCount).length;
                ed.setSelection({
                    startLineNumber: 1,
                    startColumn: 1,
                    endLineNumber: lineCount,
                    endColumn: lastLineLength + 1
                });
            }
        });

        // Set up content change listener
        const self = this;
        editor.onDidChangeModelContent(function () {
            const newContent = editor.getValue();
            
            // Notify Blazor of content change
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnEditorContentChanged', newContent);
            }
            
            // Run real-time diagnostics
            self.runDiagnostics(elementId, newContent, language);
        });

        // Set up Ctrl+S save handler
        editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, function () {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnEditorSaveRequested');
            }
        });
        
        // Initialize cross-file symbol support for this language
        this.initializeLanguageSupport(language);
        
        // Run initial diagnostics
        this.runDiagnostics(elementId, content, language);

        return editor;
    },

    getContent: function (elementId) {
        const editor = this.editors[elementId];
        if (editor) {
            return editor.getValue();
        }
        return '';
    },

    setContent: function (elementId, content) {
        const editor = this.editors[elementId];
        if (editor) {
            const currentPosition = editor.getPosition();
            editor.setValue(content || '');
            if (currentPosition) {
                editor.setPosition(currentPosition);
            }
        }
    },

    setLanguage: function (elementId, language) {
        const editor = this.editors[elementId];
        if (editor) {
            monaco.editor.setModelLanguage(editor.getModel(), language);
        }
    },

    setTheme: function (theme) {
        monaco.editor.setTheme(theme);
    },

    focus: function (elementId) {
        const editor = this.editors[elementId];
        if (editor) {
            editor.focus();
        }
    },

    undo: function (elementId) {
        const editor = this.editors[elementId];
        if (editor) {
            editor.trigger('keyboard', 'undo');
        }
    },

    redo: function (elementId) {
        const editor = this.editors[elementId];
        if (editor) {
            editor.trigger('keyboard', 'redo');
        }
    },

    dispose: function (elementId) {
        const editor = this.editors[elementId];
        if (editor) {
            editor.dispose();
            delete this.editors[elementId];
        }
    },

    disposeAll: function () {
        for (const key in this.editors) {
            this.editors[key].dispose();
        }
        this.editors = {};
    },

    // Resize editor to fit container
    layout: function (elementId) {
        const editor = this.editors[elementId];
        if (editor) {
            editor.layout();
        }
    },

    // Get selected text
    getSelection: function (elementId) {
        const editor = this.editors[elementId];
        if (editor) {
            const selection = editor.getSelection();
            return editor.getModel().getValueInRange(selection);
        }
        return '';
    },

    // Insert text at cursor
    insertText: function (elementId, text) {
        const editor = this.editors[elementId];
        if (editor) {
            const selection = editor.getSelection();
            const id = { major: 1, minor: 1 };
            const op = { identifier: id, range: selection, text: text, forceMoveMarkers: true };
            editor.executeEdits('insert', [op]);
        }
    },

    // Go to line
    goToLine: function (elementId, lineNumber) {
        const editor = this.editors[elementId];
        if (editor) {
            editor.revealLineInCenter(lineNumber);
            editor.setPosition({ lineNumber: lineNumber, column: 1 });
            editor.focus();
        }
    },

    // Add error markers
    setMarkers: function (elementId, markers) {
        const editor = this.editors[elementId];
        if (editor) {
            monaco.editor.setModelMarkers(editor.getModel(), 'pep', markers.map(m => ({
                severity: m.severity === 'error' ? monaco.MarkerSeverity.Error : monaco.MarkerSeverity.Warning,
                startLineNumber: m.startLine,
                startColumn: m.startColumn,
                endLineNumber: m.endLine,
                endColumn: m.endColumn,
                message: m.message
            })));
        }
    },

    clearMarkers: function (elementId) {
        const editor = this.editors[elementId];
        if (editor) {
            monaco.editor.setModelMarkers(editor.getModel(), 'pep', []);
        }
    }
};

// Console helper functions
window.pepConsole = {
    scrollToBottom: function (element) {
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    }
};

// Handle window resize
window.addEventListener('resize', function () {
    for (const key in window.pepMonaco.editors) {
        window.pepMonaco.editors[key].layout();
    }
});
