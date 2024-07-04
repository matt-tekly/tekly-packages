class TinkerServer {
    constructor() {
        const hostname = window.location.hostname;
        this.ws = new WebSocket(`ws://${hostname}:3334/`);
        this.ws.onopen = () => {
            console.log('Connected to WebSocket server');
            this.ws.send('Hello Server');
        };
        this.ws.onmessage = (event) => {
            console.log('Received:', event.data);
        };
        this.ws.onclose = () => {
            console.log('WebSocket connection closed');
        };
    }
}

tinkerServer = new TinkerServer();