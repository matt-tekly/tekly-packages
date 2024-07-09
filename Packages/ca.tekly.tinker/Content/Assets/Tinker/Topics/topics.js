export class Topics {
    /**
     *
     * @param {WebSocket} ws
     */
    constructor(ws) {
        this.ws = ws;
        
        /** type {Record<string, Topic> */
        this.topics = {};
        
        this.frames = [];
        
        this.ws.addEventListener("message", this.onMessage);
        this.ws.addEventListener("close", this.onClose);
    }
    
    connected() {
        for (let i = 0; i < this.frames.length; i++) {
            this.ws.send(this.frames[i]);
        }

        this.frames.length = 0;
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
        
        //if (!topic.subscribers.length) {
            this.send("SUBSCRIBE", topicId);
        //}
        
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
            topic.unsubscribe(func);
            if (!topic.subscribers.length) {
                this.send("UNSUBSCRIBE", topicId);
            }
        }
    }
    
    send(command, topicId, contentType, content) {
        
        const frame = this.encodeFrame(command, topicId, contentType, content);
        if (this.ws.readyState === WebSocket.OPEN) {
            this.ws.send(frame);
        } else {
            this.frames.push(frame);
        }
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

    encodeFrame(command, topicId, contentType, content) {
        const NEWLINE = "\r\n";
        
        let frame = command + NEWLINE;
        frame += "Topic:" + topicId + NEWLINE;
        
        if (contentType != null) {
            frame += "Content-Type:" + contentType + NEWLINE + NEWLINE;
            frame += content;
        }
        
        return frame;
    }
}

export class Topic {

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