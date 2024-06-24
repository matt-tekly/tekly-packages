class Terminal {
    constructor(query) {
        this.container = document.querySelector(query);
        this.content = this.container.querySelector('#content');
        this.input = this.container.querySelector('#input');
        this.prefix = this.container.querySelector('#prefix');

        this.scrollToBottom();
        
        this.input.addEventListener('keydown', this.processInput);
        this.input.focus();

        document.addEventListener('keydown', this.processKey);

        const resizeObserver = new ResizeObserver(entries => {
            entries.forEach(entry => {
                if (entry.target === this.content) {
                    console.log("Observer");
                    
                }
            });

            this.scrollToBottom();
        });

        // Start observing size changes
        resizeObserver.observe(this.content);
        
        this.commands = new Commands();
        this.commands.add("test", {
            handler: () => {
                console.log("test function");
                const newEntry = document.createElement('div');
                newEntry.textContent = "bingus bongus";
                
                return newEntry;
            }
        });

        this.commands.add("fetchy", {
            handler: async () => {

                let url = "/tinker/info/app";
                let html = `<div hx-trigger="revealed,reload" hx-get="${url}"></div>`

                const newEntry = document.createElement('div');
                newEntry.innerHTML = html;
                
                return newEntry;
            }
        });

        this.commands.add("inventory", {
            handler: async () => {

                let url = "/game/inventory/card";
                let html = `<div hx-trigger="revealed,reload" hx-get="${url}"></div>`

                const newEntry = document.createElement('div');
                newEntry.innerHTML = html;

                return newEntry;
            }
        });

        this.commands.add("generator_run", {
            handler: async (tokens) => {

                const url = '/game/generators/run';
                const params = new URLSearchParams();
                
                params.append('generator', tokens[1]);

                const fetchData = {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded'
                    },
                    body: params
                };

                const fetchResult = await fetch(url, fetchData);

                if (!fetchResult.ok) {
                    throw new Error('Network response was not ok');
                }
                
                return fetchResult.text();
            }
        });

        this.commands.add("screenshot", {
            handler: () => {
                let url = "/screenshot" + "?t=" + new Date().getTime();
                const text = `<a href="${url}" target="_blank" class="clickable-image"><img class="terminal-image" src="${url}"></a>`

                const range = document.createRange();
                const fragment = range.createContextualFragment(text);
                const newElement = fragment.firstChild;
                
                return newElement;
            }
        });

        this.commands.add("clear", {
            handler: () => {
                this.content.innerHTML = '';
            }
        });
    }
    
    processKey = (event) => {
        if (event.ctrlKey || event.metaKey) {
            if (event.key === 'a' || event.key === 'A') {
                event.preventDefault(); // Prevent default Ctrl+A behavior

                // Select all text nodes inside the div
                const range = document.createRange();
                range.selectNodeContents(this.content);
                const selection = window.getSelection();
                selection.removeAllRanges();
                selection.addRange(range);

                this.input.blur();
            }
        }

        if (event.key === 'Escape' || event.key === 'Esc') {
            event.preventDefault();
            const selection = window.getSelection();
            selection.removeAllRanges();

            this.input.blur();
            this.input.focus();
        }
    }

    processInput = (event) => {
        if (event.key === 'Enter') {
            event.preventDefault();
            const input = event.target.value;
            if (input.trim() !== '') {
                const newEntry = document.createElement('div');
                newEntry.textContent = "> " + input;

                this.content.appendChild(newEntry);
                this.processCommandText(input)

                this.scrollToBottom();
                this.input.value = '';
            }
        }

        if (event.key === 'ArrowUp') {
            event.preventDefault();
            this.input.value = this.commands.historyUp();
        }

        if (event.key === 'ArrowDown') {
            event.preventDefault();
            this.input.value = this.commands.historyDown();
        }
    }

    processCommandText = async (text) => {
        try {
            this.input.disabled = true;
            const result = await this.commands.process(text);

            if (result) {
                if (result instanceof Element || result instanceof DocumentFragment) {
                    let element = this.content.appendChild(result);
                    htmx.process(element);
                } else {
                    const newEntry = document.createElement('div');
                    newEntry.textContent = result;

                    this.content.appendChild(newEntry);
                }
            }
        } catch (error) {
            console.error(error);
        } finally {
            this.input.disabled = false;

            this.input.blur();
            this.input.focus();

            setTimeout(() => {
                console.log("move end");
                this.scrollToBottom();
            }, 30)
        }
    }
    
    scrollToBottom = () => {
        this.content.scrollTop = this.content.scrollHeight;
    }
}