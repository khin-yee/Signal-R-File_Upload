
export class SignalRService {
    constructor(baseUrl) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${baseUrl}/SignalR`)
            .withAutomaticReconnect()
            .build();
    }

    start() {
        return this.connection.start();
    }

    stop() {
        return this.connection.stop();
    }

    joinGroup(groupName) {
        return this.connection.invoke("JoinGroup", groupName);
    }

    onReceiveMessage(methodName, callback) {
        this.connection.on(methodName, callback);
    }

    sendMessage(methodName, ...args) {
        return this.connection.invoke(methodName, ...args);
    }
}
