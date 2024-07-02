class Terminal {
    constructor(query) {
        this.container = document.querySelector(query);
        this.content = this.container.querySelector('#content');
        this.prefix = this.container.querySelector('#prefix');
        
        this.commands = new Commands();
        this.addDefaultCommands();

        this.input = new TerminalInput(this, this.commands);

        document.addEventListener('keydown', this.processKey);

        const resizeObserver= new MutationObserver(entries => {
            // TODO: This doesn't seem like a good solution
            // setTimeout(this.scrollToBottom, 100);
        });

        var config = {childList: true};
        resizeObserver.observe(this.content, config);
        
        this.addText("Tinker Terminal", "title");
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
        this.content.prepend(element);
        htmx.process(element);
    }

    addText = (text, className) => {
        const divElement = document.createElement('div');
        divElement.textContent = text;
        if (className){
            divElement.classList.add(className);    
        }
        this.content.prepend(divElement);
        // this.content.appendChild(divElement)
    }

    addError = (text) => {
        this.addText(text, "error");
    }

    scrollToBottom = () => {
        // this.content.scrollTop = this.content.scrollHeight;
    }

    scrollToBottomIfFollowingBottom = (element) => {
        // element.scrollIntoView({ behavior: "instant", block: "end" });
        
        // console.log(this.content.scrollTop === this.content.scrollHeight);
        // this.content.scrollTop = this.content.scrollHeight;
    }

    clear = () => {
        this.content.innerHTML = "";
    }
}