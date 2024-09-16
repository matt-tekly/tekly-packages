export class Command
{
    /**
     * @param {string} name - The name of the command
     * @param {function} handler - The handler for the command
     * @param {string[]} params
     */
    constructor(name, handler, params) {
        this.name = name;
        this.handler = handler
        this.params = params
    }
}

export class Commands {
    constructor() {
        /** @type {Record<string, Command>} */
        this.commands = {};
        this.history = [];
        this.selectedHistory = 0;
        
        this.addFunction("list", () => {
            let root = document.createElement('div');
            
            const commands = Object.values(this.commands);
            commands.map(cmd => {
                const newEntry = document.createElement('div');
                newEntry.innerText = cmd.name;
                root.appendChild(newEntry);
            })
            
            return root;
        });
    }

    /**
     *
     * @param {Command} command
     */
    add(command) {
        this.commands[command.name] = command;
    }

    /**
     * 
     * @param {string} name
     * @param {function} func
     */
    addFunction(name, func) {
        this.add(new Command(name, func, this.getParameterNames(func)));
    }

    /**
     * Gets a command by name
     * @param {string} name
     * @return {Command}
     */
    get(name) {
        return this.commands[name];
    }

    /**
     * Processes and runs a command
     * @param {string} input
     * @param {Terminal} terminal
     * @return {Promise<any>}
     */
    async process(input, terminal) {
        
        this.history.push(input);
        this.selectedHistory = this.history.length;
        
        const tokens = this.tokenize(input)

        let command = this.get(tokens[0]);
        if (command) {
            if (command.params.length > tokens.length - 1) {
                throw Error(`Not enough params`);
            }
            
            const context = {
                terminal: terminal,
                tokens: tokens
            }
            
            const boundFunction = command.handler.bind(context);
            let result = boundFunction(...tokens.slice(1));

            if (result instanceof Promise) {
                result = await result;
            }

            return result;
        } else {
            throw Error(`Unknown command [${tokens[0]}]`);
        }
    }
    
    historyUp() {
        return this.setSelectedHistory(-1);
    }

    historyDown() {
        return this.setSelectedHistory(1);
    }
    
    setSelectedHistory(increment) {
        if (this.history.length === 0) {
            return '';
        }
        
        this.selectedHistory = Math.max(0, Math.min(this.selectedHistory + increment, this.history.length - 1));
        return this.history[this.selectedHistory];
    }

    /**
     * Tokenizes input
     * @param {string} input
     * @return {string[]}
     */
    tokenize(input) {
        const regex = /[^\s"']+|"([^"]*)"|'([^']*)'/g;
        let match;
        const tokens = [];

        while ((match = regex.exec(input)) !== null) {
            tokens.push(match[1] || match[2] || match[0]);
        }

        return tokens;
    }

    /**
     * 
     * @param {string} text
     * @return {Command[]}
     */
    search(text) {
        const commands = Object.values(this.commands);
        return fuzzysort.go(text, commands, {
            key: "name", 
        }).map(x => x.obj);
    }
    
    addTinkerCommands(commands) {
        for (const cmd of commands) {
            
            const parameters = cmd.Parameters.filter(x => x.EditType).map(x => x.Name);
            const name = cmd.CommandName;
            const handler = functionHandler(cmd);
            
            const command = new Command(name, handler, parameters);
            this.add(command)
        }
    }

    getParameterNames(func) {
        const funcStr = func.toString();
        const result = funcStr.match(/^[^(]*\(\s*([^)]*?)\s*\)/);

        if (result && result[1]) {
            return result[1]
                .split(',')
                .map(param => param.trim())
                .filter(param => param);
        }

        return [];
    }
}

function functionHandler(cmd) {
    return async function (){
        const url = cmd.Path;

        let fetchData = null;
        if (cmd.Verb === "GET") {
            fetchData = {
                method: cmd.Verb,
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
            };
        } else {
            const params = new URLSearchParams();

            for (let i = 1; i < this.tokens.length; i++) {
                params.set(cmd.Parameters[i-1].Name, this.tokens[i]);
            }

            fetchData = {
                method: cmd.Verb,
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: params
            };
        }

        const fetchResult = await fetch(url, fetchData);

        if (!fetchResult.ok) {
            let text = await fetchResult.text();
            throw new Error('Network response was not ok: ' + text);
        }
        
        return handleResponse(fetchResult);
    }
}

async function handleResponse(response) {
    const contentType = response.headers.get('Content-Type');
    if (contentType.includes('text/html')) {
        const htmlText = await response.text();

        const range = document.createRange();
        const fragment = range.createContextualFragment(htmlText);
        return fragment.firstChild;
    } else if (contentType.includes('application/json')) {
        const json = await response.text();
        const newEntry = document.createElement('pre');
        newEntry.classList.add("json", "box-shadow");
        newEntry.innerText = json;

        return newEntry
    } else if (contentType.includes('image/png')) {
        const blob = await response.blob();
        const objectURL = URL.createObjectURL(blob);

        const div = document.createElement('div');
        const a = document.createElement('a');
        a.href = response.url;
        a.target = "_blank";

        const img = document.createElement('img');
        img.classList.add("terminal-image");
        img.src = objectURL;

        img.onload = () => {
            URL.revokeObjectURL(objectURL);
        };

        a.appendChild(img);
        div.appendChild(a);

        return div;
    } else {
        return response.text();
    }
}