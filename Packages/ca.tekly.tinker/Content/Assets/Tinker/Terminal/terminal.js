import { Commands } from "./commands.js";
import { TerminalInput} from "./terminalinput.js";

export class Terminal {
    constructor(query) {
        this.container = document.querySelector(query);
        this.content = this.container.querySelector('#content');
        this.prefix = this.container.querySelector('#prefix');

        this.commands = new Commands();
        this.addDefaultCommands();

        this.input = new TerminalInput(this, this.commands);

        document.addEventListener('keydown', this.processKey);
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

        let event = new Event("reload", {bubbles: true});
        element.dispatchEvent(event);
    }

    addText = (text, className) => {
        const divElement = document.createElement('div');
        divElement.textContent = text;
        if (className) {
            divElement.classList.add(className);
        }
        this.content.prepend(divElement);
    }

    addJson = (text) => {
        const pre = document.createElement('pre');
        pre.textContent = text;
        pre.classList.add("json", "box-shadow");
        pre.innerText = text;
        this.content.prepend(pre);
    }

    addError = (text) => {
        this.addText(text, "error");
    }
    clear = () => {
        this.content.innerHTML = "";
    }
}