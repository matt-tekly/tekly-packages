function triggerReload(element) {
    let event = new Event("reload", {bubbles: true});
    element.dispatchEvent(event);
}

/**
 * @typedef {Object} Command
 * @property {string} name - The name of the command
 * @property {function} handler - The handler for the command
 */

/**
 * @typedef {Object.<string, Command>} CommandMap
 * @description A map where keys are strings and values are User objects.
 */
class Commands {
    constructor() {
        /** @type {CommandMap} */
        this.commands = {};
        this.history = [];
        this.selectedHistory = 0;
    }

    /**
     *
     * @param {string} name
     * @param {Command} command
     */
    add(name, command) {
        this.commands[name] = command;
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
     * @return {Promise<any>}
     */
    async process(input) {
        
        this.history.push(input);
        this.selectedHistory = this.history.length;
        
        const tokens = this.tokenize(input)

        let command = this.get(tokens[0]);
        if (command) {
            let result = command.handler(tokens);

            if (result instanceof Promise) {
                result = await result;
            }

            return result;
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
}