import * as signalR from "@aspnet/signalr";

const divMessages: HTMLDivElement = document.querySelector("#divMessages");
const tbMessage: HTMLInputElement = document.querySelector("#tbMessage");
const btnSend: HTMLButtonElement = document.querySelector("#btnSend");
const serverIdInput: HTMLInputElement = document.getElementById('serverIdInput') as HTMLInputElement;
const startButton: HTMLButtonElement = document.getElementById('startButton') as HTMLButtonElement;
const stopButton: HTMLButtonElement = document.getElementById('stopButton') as HTMLButtonElement;
const forceStopButton: HTMLButtonElement = document.getElementById('forceStopButton') as HTMLButtonElement;
const getStatusButton: HTMLButtonElement = document.getElementById('getStatusButton') as HTMLButtonElement;
const statusText: HTMLInputElement = document.getElementById('statusText') as HTMLInputElement;

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/FactorioControlHub")
    .build();

async function init() {
    try {
        await connection.start();
        let data = await connection.invoke("SetServerId", serverIdInput.value);        
        statusText.value = data.status;
    } catch (ex) {
        console.log(ex.message);
    }
}

init();

connection.on("FactorioOutputData", (data: string) => {
    let m = document.createElement("div");

    m.innerHTML =
        `<div>${data}</div>`;

    divMessages.appendChild(m);
    divMessages.scrollTop = divMessages.scrollHeight;
});

connection.on("FactorioWrapperOutputData", (data: string) => {
    let m = document.createElement("div");

    m.innerHTML =
        `<div>Wrapper: ${data}</div>`;

    divMessages.appendChild(m);
    divMessages.scrollTop = divMessages.scrollHeight;
});

connection.on("FactorioWebInterfaceData", (data: string) => {
    let m = document.createElement("div");

    m.innerHTML =
        `<div>Web: ${data}</div>`;

    divMessages.appendChild(m);
    divMessages.scrollTop = divMessages.scrollHeight;
});

connection.on('FactorioStatusChanged', (newStatus: string, oldStatus: string) => {
    console.log(`new: ${newStatus}, old: ${oldStatus}`);
    statusText.value = newStatus;

    let m = document.createElement("div");

    m.innerHTML =
        `<div>[STATUS]: Changed from ${oldStatus} to ${newStatus}</div>`;

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

getStatusButton.onclick = () => {
    connection.invoke("GetStatus").then((data) => {
        console.log(`status: ${data}`);
        statusText.value = data;
    });
}