class Topics {
    /**
     *
     * @param {WebSocket} ws
     */
    constructor(ws) {
        this.ws = ws;
        
        /** type {Record<string, Topic> */
        this.topics = {}
        
        this.ws.addEventListener("message", this.onMessage);
        this.ws.addEventListener("close", this.onClose);
    }

    parseFrame(frameText) {
        const [headersStr, body] = frameText.split('\r\n\r\n');
        const headers = {};
        const headerPairs = headersStr.trim().split('\r\n');
        let command = headerPairs[0];
        
        for (let i = 1; i < headerPairs.length; i++) {
            const [key, value] = headerPairs[i].split(':');
            headers[key] = value;
        }

        return { command, headers, body };
    }
    
    onMessage = (evt) => {
        const frame = this.parseFrame(evt.data);

        let topic = this.topics[frame.headers["Topic"]];

        if (topic) {
            topic.onMessage(frame);
        }
    }

    onClose = () =>{
        // Do something?
    }

    /**
     * 
     * @param {string} topicId
     * @param {Function} func
     */
    subscribe(topicId, func) {
        let topic = this.topics[topicId];
        
        if (!topic) {
            topic = new Topic(topicId);
            this.topics[topicId] = topic;
        }
        
        topic.subscribe(func);
    }

    /**
     *
     * @param {string} topicId
     * @param {Function} func
     */
    unsubscribe(topicId, func) {
        let topic = this.topics[topicId];

        if (topic) {
            topic.unsubscribe(topicId);
        }
    }
}

class Topic {

    /**
     *
     * @param {String} name
     */
    constructor(name) {
        this.name = name;
        /** @type {Function[]} */
        this.subscribers = [];
    }

    subscribe(func) {
        this.subscribers.push(func);
    }

    unsubscribe(func) {
        const length = this.subscribers.length;
        for (let i = length - 1; i >= 0; i--) {
            if (this.subscribers[i] === func) {
                this.subscribers.splice(i, 1);
            }
        }
    }
    
    onMessage(frame) {
        const length = this.subscribers.length;
        for (let i = length - 1; i >= 0; i--) {
            this.subscribers[i](frame);
        }
    }
}