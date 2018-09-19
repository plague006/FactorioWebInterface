import * as signalR from "@aspnet/signalr";

const divMessages: HTMLDivElement = document.querySelector("#divMessages");
const tbMessage: HTMLInputElement = document.querySelector("#tbMessage");
const btnSend: HTMLButtonElement = document.querySelector("#btnSend");
const serverIdInput: HTMLInputElement = document.getElementById('serverIdInput') as HTMLInputElement;
const startButton: HTMLButtonElement = document.getElementById('startButton') as HTMLButtonElement;
const stopButton: HTMLButtonElement = document.getElementById('stopButton') as HTMLButtonElement;
const forceStopButton: HTMLButtonElement = document.getElementById('forceStopButton') as HTMLButtonElement;

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/FactorioControlHub")
    .build();

connection.start()
    .then(() => {
        connection.invoke("SetServerId", serverIdInput.value);
    })
    .catch(err => document.write(err));

connection.on("FactorioOutputData", (data: string) => {
    let m = document.createElement("div");

    m.innerHTML =
        `<div>${data}</div>`;

    divMessages.appendChild(m);
    divMessages.scrollTop = divMessages.scrollHeight;
});

tbMessage.addEventListener("keyup", (e: KeyboardEvent) => {
    if (e.keyCode === 13) {
        send();
    }
});

btnSend.addEventListener("click", send);

function send() {
    connection.send("SendToFactorio", tbMessage.value)
        .then(() => tbMessage.value = "");
}

startButton.onclick = () => {
    connection.invoke("Start")
        .then(() => console.log("started"));
}

stopButton.onclick = () => {
    connection.invoke("Stop")
        .then(() => console.log("stopped"));
}

forceStopButton.onclick = () => {
    connection.invoke("ForceStop")
        .then(() => console.log("force stopped"));
}