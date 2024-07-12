import {server} from "./tinker_server.js";

class ChannelElement extends HTMLElement {
    static get observedAttributes() {
        return ['channel'];
    }

    constructor() {
        super();
        this.subscription = null;
    }

    attributeChangedCallback(name, oldValue, newValue) {
        if (name === "channel") {
            this.trySubscribe(newValue);
        }
    }

    connectedCallback() {
        this.trySubscribe(this.getAttribute('channel'));
    }

    disconnectedCallback() {
        this.tryUnsubscribe();
    }

    trySubscribe(channel) {
        if (!this.subscription || this.subscription.channel !== channel) {
            this.tryUnsubscribe();

            if (channel) {
                this.subscription = server.channels.subscribe(channel, this.onChannelMessage);
            }
        }
        
        this.render();
    }

    tryUnsubscribe() {
        if (this.subscription) {
            this.subscription.unsubscribe();
            this.subscription = null;
        }
    }

    /**
     * @param {ChannelFrame} message
     */
    onChannelMessage = (message) => {
        this._data = JSON.parse(message.Content);
        this.render();
    }

    render() {
    }
}

class StatsCard extends ChannelElement {
    render() {
        this.innerHTML = '';

        if (!this._data) {
            return;
        }
        
        if (this._data.Name) {
            const headerContainer = document.createElement('div');
            headerContainer.classList.add("header");

            const header = document.createElement('h3');
            header.textContent = this._data.Name;
            headerContainer.appendChild(header);

            const buttonsContainer = document.createElement('div');
            buttonsContainer.classList.add("buttons");
            
            const trashSpan = document.createElement('span')
            trashSpan.classList.add('button-icon', 'hide', 'edit-inline');
            trashSpan.addEventListener('click', () => triggerRemove(trashSpan));

            const icon = document.createElement('i')
            icon.classList.add('bx', 'bx-trash');
            
            trashSpan.appendChild(icon);
            buttonsContainer.appendChild(trashSpan);
            headerContainer.appendChild(buttonsContainer);
            
            this.appendChild(headerContainer);
        }
        
        const statsContainer = document.createElement('div');
        statsContainer.classList.add("stats");

        for (const stat of this._data.Items) {
            const item = document.createElement('div');
            item.classList.add("stat")

            const valueDiv = document.createElement('div');
            valueDiv.textContent = stat.Value;
            valueDiv.classList.add("value", stat.Color)

            const nameDiv = document.createElement('div');
            nameDiv.classList.add("label");
            nameDiv.textContent = stat.Name;

            item.appendChild(valueDiv);
            item.appendChild(nameDiv);
            statsContainer.appendChild(item);
        }

        this.appendChild(statsContainer);
    }
}

customElements.define('stats-card', StatsCard);