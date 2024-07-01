class Terminal {
    constructor(query) {
        this.container = document.querySelector(query);
        this.content = this.container.querySelector('#content');
        this.prefix = this.container.querySelector('#prefix');
        
        this.commands = new Commands();
        this.addDefaultCommands();

        this.input = new TerminalInput(this, this.commands);

        document.addEventListener('keydown', this.processKey);

        const resizeObserver = new ResizeObserver(entries => {
            entries.forEach(entry => {
                if (entry.target === this.content) {
                    console.log("Observer");
                }
            });

            this.scrollToBottom();
        });
        
        resizeObserver.observe(this.content);
        
        this.addText("Tinker", "title");
    }

    addDefaultCommands = () => {
        this.commands.addFunction("clear", this.clear);
    }

    processKey = (evt) => {
        if (evt.key === "Escape") {
            this.input.focus();
        }
    }

    addContent = (element) => {
        const newEntry= this.content.appendChild(element);
        htmx.process(newEntry);
    }

    addText = (text, className) => {
        const divElement = document.createElement('div');
        divElement.textContent = text;
        if (className){
            divElement.classList.add(className);    
        }
        this.content.appendChild(divElement)
    }

    addError = (text) => {
        this.addText(text, "error");
    }

    scrollToBottom = () => {
        this.content.scrollTop = this.content.scrollHeight;
    }

    clear = () => {
        this.content.innerHTML = "";
    }
}