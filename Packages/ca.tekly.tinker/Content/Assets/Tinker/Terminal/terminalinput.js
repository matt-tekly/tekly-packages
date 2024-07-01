class TerminalInput {
    /**
     *
     * @param {Terminal} terminal
     * @param {Commands} commands
     */
    constructor(terminal, commands) {
        this.terminal = terminal;
        this.commands = commands;
        
        this.input = this.terminal.container.querySelector('#input');
        this.input.addEventListener('keydown', this.processInput);
        this.input.addEventListener('blur', this.onBlur);
        
        this.suggestion = new TerminalSuggestion(this.commands, this.terminal.container.querySelector('#suggestion'))

        this.input.addEventListener('input', () => {
            this.suggestion.setText(this.input.value);
        });
    }
    
    focus() {
        this.input.focus();
    }
    
    onBlur = () => {
        this.suggestion.hide();
    }

    processInput = (event) => {
        if (event.key === 'Enter') {
            event.preventDefault();
            let input = event.target.value;

            const active = this.suggestion.getActive();
            if (active) {
                input = active.name;
            }
            
            if (input.trim() !== '') {
                
                this.terminal.addText("> " + input, "terminal-sent-command")
                
                this.processCommandText(input)

                this.terminal.scrollToBottom();
                this.input.value = '';
            }
            
            return;
        }
        
        if (event.key === 'Tab') {
            event.preventDefault();
            this.suggestion.tabPressed(event.shiftKey);

            const active = this.suggestion.getActive();
            if (active) {
                this.input.value = active.name;
            }
            
            return;
        }

        if (event.key === ' ') {
            const active = this.suggestion.getActive();
            if (active) {
                this.input.value = active.name + " ";
                this.suggestion.setText(this.input.value);
                event.preventDefault();
            }
            
            return;
        }

        if (event.key === 'ArrowUp') {
            event.preventDefault();
            this.input.value = this.commands.historyUp();
            return;
        }

        if (event.key === 'ArrowDown') {
            event.preventDefault();
            this.input.value = this.commands.historyDown();
            return;
        }

        if (event.key === "Shift") {
            return;
        }
        
        this.suggestion.setText(this.input.value);
    }

    processCommandText = async (text) => {
        try {
            this.input.disabled = true;
            
            const result = await this.commands.process(text, this.terminal);

            if (result) {
                if (result instanceof Element || result instanceof DocumentFragment) {
                    this.terminal.addContent(result);
                } else {
                    const newEntry = document.createElement('div');
                    newEntry.textContent = result;

                    this.terminal.addContent(newEntry);
                }
            }
        } catch (error) {
            this.terminal.addError(error);
        } finally {
            this.input.disabled = false;

            this.input.blur();
            this.input.focus();

            setTimeout(() => {
                this.terminal.scrollToBottom();
            }, 30)
        }
    }
}
