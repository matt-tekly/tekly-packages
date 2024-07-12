export class Channels {
    /**
     *
     * @param {WebSocket} ws
     */
    constructor(ws) {
        this.ws = ws;

        /** type {Record<string, Topic> */
        this.channels = {};
        this.subscriptions = {};

        this.frames = [];
        this.id = 0;

        this.ws.addEventListener("open", this.connected)
        this.ws.addEventListener("message", this.onMessage);
        this.ws.addEventListener("close", this.onClose);
    }

    connected = () => {
        for (let i = 0; i < this.frames.length; i++) {
            this.ws.send(this.frames[i]);
        }

        this.frames.length = 0;
    }

    onMessage = (evt) => {
        const frame = this.parseFrame(evt.data);

        let subscription = this.subscriptions[frame.Session];

        if (subscription) {
            subscription.onMessage(frame);
        }
    }

    send(command, channelId, session, contentType, content) {

        const frame = this.encodeFrame(command, channelId, session, contentType, content);
        if (this.ws.readyState === WebSocket.OPEN) {
            this.ws.send(frame);
        } else {
            this.frames.push(frame);
        }
    }

    encodeFrame(command, channelId, session, contentType, content) {
        const NEWLINE = "\r\n";

        let frame = command + NEWLINE;
        frame += "Channel:" + channelId + NEWLINE;
        frame += "Session:" + session + NEWLINE;

        if (contentType != null) {
            frame += "ContentType:" + contentType + NEWLINE + NEWLINE;
            frame += content;
        }

        return frame;
    }

    onClose = () => {
        // Do something?
    }

    parseFrame(frameText) {
        const [headersStr, content] = frameText.split('\r\n\r\n');
        const headerPairs = headersStr.trim().split('\r\n');

        let frame = {};
        frame.command = headerPairs[0];

        for (let i = 1; i < headerPairs.length; i++) {
            const [key, value] = headerPairs[i].split(':');
            frame[key] = value;
        }

        frame.Content = content;

        return frame;
    }

    subscribe(channel, handler) {
        const id = (this.id++).toString();
        const subscription = new Subscription(channel, id, handler, this);
        this.send("SUBSCRIBE", subscription.channel, subscription.id);

        this.subscriptions[subscription.id] = subscription;
        return subscription;
    }

    unsubscribe(subscription) {
        this.send("UNSUBSCRIBE", subscription.channel, subscription.id);
        delete this.subscriptions[subscription.id];
    }
}

class Subscription {
    constructor(channel, id, handler, channels) {
        this.channel = channel;
        this.id = id;
        this.handler = handler;
        this.channels = channels;
    }

    onMessage(frame) {
        this.handler(frame);
    }

    unsubscribe() {
        this.channels.unsubscribe(this);
    }
}