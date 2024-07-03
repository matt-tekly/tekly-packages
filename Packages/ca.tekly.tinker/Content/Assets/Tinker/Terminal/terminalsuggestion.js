
class Suggestion {
    /**
     * 
     * @param {Command} command
     * @param {HTMLElement} container
     */
    constructor(command, container) {
        this.command = command;

        this.div = document.createElement('div');

        let span = document.createElement('span');
        span.classList.add("command");
        span.innerText = this.command.name;
        this.div.append(span);

        for (const param of this.command.params) {
            let span = document.createElement('span');
            span.classList.add("param");
            span.innerText = param;
            this.div.append(span);
        }

        container.appendChild(this.div)
    }
    
    setActive(active) {
        if (active) {
            this.div.classList.add("active");
        } else {
            this.div.classList.remove("active");
        }
    }
}

class TerminalSuggestion {

    /**
     *
     * @param {Commands} commands
     * @param {Element} container
     */
    constructor(commands, container) {
        this.commands = commands;
        this.container = container;

        this.hide();

        /** @type {Suggestion[]} */
        this.suggestions = [];

        /** @type {Suggestion|null} */
        this.activeSuggestion = null;

        this.active = -1
    }

    tabPressed(shiftPressed) {
        if (!this.presented) {
            this.container.classList.remove("hide");
            this.presented = true;
        } else if (this.suggestions.length) {
            if (shiftPressed) {
                this.active = (this.active - 1 + this.suggestions.length) % this.suggestions.length;
            } else {
                this.active = (this.active + 1) % this.suggestions.length;    
            }
            
            if (this.activeSuggestion) {
                this.activeSuggestion.setActive(false)
            }

            this.activeSuggestion = this.suggestions[this.active];
            this.activeSuggestion.setActive(true);
        }
    }

    hide() {
        this.container.classList.add("hide");
        this.presented = false;
    }
    
    getActive() {
        if (this.presented && this.activeSuggestion) {
            return this.activeSuggestion.command;    
        }
        
        return null;
    }

    /**
     *
     * @param {string} text
     */
    setText(text) {
        if (!text) {
            this.clear();
            return;
        }

        this.container.classList.remove("hide");
        this.presented = true;

        this.container.innerHTML = '';
        const filteredCommands = this.commands.search(text);

        if (!filteredCommands.length) {
            this.clear()
            return;
        }
        
        this.suggestions = filteredCommands.map(cmd => {
            return new Suggestion(cmd,  this.container);
        })
    }

    clear = () => {
        this.hide();
        this.container.innerHTML = '';

        this.active = -1;
        this.activeSuggestion = null;
        this.suggestions = [];
        this.presented = false;
    }
}