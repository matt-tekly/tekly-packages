import {Channels} from "./Channeling/channels.js";

export class TinkerServer {
    constructor() {
        const hostname = window.location.hostname;
        
        this.ws = new WebSocket(`ws://${hostname}:${TINKER_WS_PORT}/`);
        this.channels = new Channels(this.ws);
    }
}

export const server = new TinkerServer();