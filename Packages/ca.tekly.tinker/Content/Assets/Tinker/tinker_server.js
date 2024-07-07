import {Topics} from "./Topics/topics.js";

export class TinkerServer {
    constructor() {
        const hostname = window.location.hostname;
        
        this.ws = new WebSocket(`ws://${hostname}:3334/`);
        
        this.ws.onopen = () => {
            console.log('Tinker WebSocket connected');
            this.topics.subscribe("general", (frame)=> {
                console.log("General", frame)
            });
            
            this.topics.connected();
            this.topics.send("SEND", "general", "text", "Hello");
        };
        
        this.ws.onclose = () => {
            console.log('Tinker WebSocket closed');
        };
        
        this.topics = new Topics(this.ws);
    }
}

export const server = new TinkerServer();