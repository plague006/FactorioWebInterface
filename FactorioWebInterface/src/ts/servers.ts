import * as signalR from "@aspnet/signalr";

enum MessageType {
    Output,
    Wrapper,
    Control,
    Status,
    Discord
}

interface MessageData {
    messageType: MessageType;
    message: string;
}

interface FileData {
    name: string;
    createdTime: string;
    lastModifiedTime: string;
    size: number;
}

interface FactorioContorlClientData {
    status: string;
    messages: MessageData[];
}



const maxMessageCount = 100;

const divMessages: HTMLDivElement = document.querySelector("#divMessages");
const tbMessage: HTMLInputElement = document.querySelector("#tbMessage");
const btnSend: HTMLButtonElement = document.querySelector("#btnSend");
const serverIdInput: HTMLInputElement = document.getElementById('serverIdInput') as HTMLInputElement;
const startButton: HTMLButtonElement = document.getElementById('startButton') as HTMLButtonElement;
const stopButton: HTMLButtonElement = document.getElementById('stopButton') as HTMLButtonElement;
const forceStopButton: HTMLButtonElement = document.getElementById('forceStopButton') as HTMLButtonElement;
const getStatusButton: HTMLButtonElement = document.getElementById('getStatusButton') as HTMLButtonElement;
const statusText: HTMLInputElement = document.getElementById('statusText') as HTMLInputElement;

const localSaveFilesTable: HTMLTableElement = document.getElementById('localSaveFilesTable') as HTMLTableElement;

let messageCount = 0

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/FactorioControlHub")
    .build();

async function init() {
    try {
        await connection.start();
        let data = await connection.invoke('SetServerId', serverIdInput.value) as FactorioContorlClientData;
        statusText.value = data.status;

        let files = await connection.invoke('GetLocalSaveFiles') as FileData[];
        buildFileTable(localSaveFilesTable, files);

        for (let message of data.messages) {
            writeMessage(message);
        }

    } catch (ex) {
        console.log(ex.message);
    }
}

init();

connection.on("SendMessage", writeMessage)

connection.on('FactorioStatusChanged', (newStatus: string, oldStatus: string) => {
    console.log(`new: ${newStatus}, old: ${oldStatus}`);
    statusText.value = newStatus;
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
    connection.invoke("GetStatus");
}

function writeMessage(message: MessageData): void {
    let div = document.createElement("div");
    let data: string;

    switch (message.messageType) {
        case MessageType.Output:
            data = `${message.message}`;
            break;
        case MessageType.Wrapper:
            data = `[Wrapper] ${message.message}`;
            break;
        case MessageType.Control:
            data = `[Control] ${message.message}`;
            break;
        case MessageType.Discord:
            data = message.message;
            break;
        case MessageType.Status:
            div.classList.add('bg-info', 'text-white');
            data = message.message;
            break;
        default:
            data = "";
            break;
    }

    div.innerText = data;

    if (messageCount === 100) {
        let first = divMessages.firstChild
        divMessages.removeChild(first);
    } else {
        messageCount++;
    }

    divMessages.appendChild(div);
    divMessages.scrollTop = divMessages.scrollHeight;
}

function buildFileTable(table: HTMLTableElement, files: FileData[]) {
    let body = table.tBodies[0];

    for (let child of body.children) {
        child.remove();
    }

    for (let file of files) {
        let row = document.createElement('tr');

        let cell = document.createElement('td');
        let checkbox = document.createElement('input') as HTMLInputElement;
        checkbox.type = 'checkbox';
        cell.appendChild(checkbox);
        row.appendChild(cell);

        createCell(row, file.name);
        createCell(row, file.createdTime);
        createCell(row, file.lastModifiedTime);
        createCell(row, file.size.toString());

        body.appendChild(row);
    }
}

function createCell(parent: HTMLElement, content: string) {
    let cell = document.createElement('td');
    cell.innerText = content;
    parent.appendChild(cell);
}