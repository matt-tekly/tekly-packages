class Command
{
    /**
     * @param {string} name - The name of the command
     * @param {function} handler - The handler for the command
     */
    constructor(name, handler) {
        this.name = name;
        this.handler = handler
        this.params = this.getParameterNames(this.handler);
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

class Commands {
    constructor() {
        /** @type {Record<string, Command>} */
        this.commands = {};
        this.history = [];
        this.selectedHistory = 0;
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
        this.add(new Command(name, func));
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
}