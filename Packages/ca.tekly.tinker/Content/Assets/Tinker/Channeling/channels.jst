/**
 * @typedef {Object} ChannelFrame
 * @property {string} Command - The command
 * @property {string} Channel - The name of the channel
 * @property {string} Session - The session id
 * @property {string} ContentType - The type of content
 * @property {string} Content - The content
 */

/**
 * Handles the completion of an asynchronous operation.
 *
 * @callback SubscriptionHandler
 * @param {ChannelFrame} frame
 */

export class Channels {
    
    /**
     * @param {WebSocket} ws
     */
    constructor(ws) {
        this.ws = ws;
        
        /** type {Record<string, Subscription> */
        this.subscriptions = {};

        this.queuedFrames = [];
        this.id = 0;

        this.ws.addEventListener("open", this.connected)
        this.ws.addEventListener("message", this.onMessage);
        this.ws.addEventListener("close", this.onClose);
    }

    connected = () => {
        for (let i = 0; i < this.queuedFrames.length; i++) {
            this.ws.send(this.queuedFrames[i]);
        }

        this.queuedFrames.length = 0;
    }

    /**
     * 
     * @param {MessageEvent<T = any>} evt
     */
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
            this.queuedFrames.push(frame);
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

    /**
     * @param {string} frameText
     * @return {ChannelFrame}
     */
    parseFrame(frameText) {
        const [headersStr, content] = frameText.split('\r\n\r\n');
        const headerPairs = headersStr.trim().split('\r\n');

        let frame = {};
        frame.Command = headerPairs[0];

        for (let i = 1; i < headerPairs.length; i++) {
            const [key, value] = headerPairs[i].split(':');
            frame[key] = value;
        }

        frame.Content = content;

        return frame;
    }

    /**
     * 
     * @param {string} channel
     * @param {SubscriptionHandler} handler
     * @return {Subscription}
     */
    subscribe(channel, handler) {
        const id = (this.id++).toString();
        const subscription = new Subscription(channel, id, handler, this);
        this.send("SUBSCRIBE", subscription.channel, subscription.id);

        this.subscriptions[subscription.id] = subscription;
        return subscription;
    }

    /**
     * @param {Subscription} subscription
     */
    unsubscribe(subscription) {
        this.send("UNSUBSCRIBE", subscription.channel, subscription.id);
        delete this.subscriptions[subscription.id];
    }
}

class Subscription {

    /**
     * @param {string} channel
     * @param {string} id
     * @param {SubscriptionHandler} handler
     * @param {Channels} channels
     */
    constructor(channel, id, handler, channels) {
        this.channel = channel;
        this.id = id;
        this.handler = handler;
        this.channels = channels;
    }

    /**
     * @param {ChannelFrame} frame
     */
    onMessage(frame) {
        this.handler(frame);
    }

    unsubscribe() {
        this.channels.unsubscribe(this);
    }
}