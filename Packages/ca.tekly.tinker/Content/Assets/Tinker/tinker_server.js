class TinkerServer {
    constructor() {
        const hostname = window.location.hostname;
        
        this.ws = new WebSocket(`ws://${hostname}:3334/`);
        
        this.ws.onopen = () => {
            console.log('Connected to WebSocket server');
            this.topics.subscribe("general", (frame)=> {
                console.log("General", frame)
            });
            
            this.topics.send("SEND", "general", "text", "Hello");
        };
        
        this.ws.onclose = () => {
            console.log('WebSocket connection closed');
        };
        
        this.listeners = [];
        this.topics = new Topics(this.ws);
    }
    
    addListener(listener) {
        this.listeners.push(listener);
    }
}

tinkerServer = new TinkerServer();