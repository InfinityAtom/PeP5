// PeP Monaco Editor Integration
window.pepMonaco = {
    editors: {},

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

        // Create editor
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
            }
        });

        // Store editor instance
        this.editors[elementId] = editor;

        // Set up content change listener
        editor.onDidChangeModelContent(function () {
            if (dotNetRef) {
                const newContent = editor.getValue();
                dotNetRef.invokeMethodAsync('OnEditorContentChanged', newContent);
            }
        });

        // Set up Ctrl+S save handler
        editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, function () {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnEditorSaveRequested');
            }
        });

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
