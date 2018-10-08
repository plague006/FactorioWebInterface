import * as signalR from "@aspnet/signalr";
import * as $ from "jquery";

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

interface FileMetaData {
    name: string;
    directory: string,
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
const resumeButton: HTMLButtonElement = document.getElementById('resumeButton') as HTMLButtonElement;
const LoadButton: HTMLButtonElement = document.getElementById('loadButton') as HTMLButtonElement;
const stopButton: HTMLButtonElement = document.getElementById('stopButton') as HTMLButtonElement;
const forceStopButton: HTMLButtonElement = document.getElementById('forceStopButton') as HTMLButtonElement;
const getStatusButton: HTMLButtonElement = document.getElementById('getStatusButton') as HTMLButtonElement;
const statusText: HTMLInputElement = document.getElementById('statusText') as HTMLInputElement;

const tempSaveFilesTable: HTMLTableElement = document.getElementById('tempSaveFilesTable') as HTMLTableElement;
const localSaveFilesTable: HTMLTableElement = document.getElementById('localSaveFilesTable') as HTMLTableElement;
const globalSaveFilesTable: HTMLTableElement = document.getElementById('globalSaveFilesTable') as HTMLTableElement;

let messageCount = 0

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/FactorioControlHub")
    .build();

async function init() {
    try {
        await connection.start();
        let data = await connection.invoke('SetServerId', serverIdInput.value) as FactorioContorlClientData;
        statusText.value = data.status;

        let tempFiles = await connection.invoke('GetTempSaveFiles') as FileMetaData[];
        buildFileTable(tempSaveFilesTable, tempFiles);

        let localFiles = await connection.invoke('GetLocalSaveFiles') as FileMetaData[];
        buildFileTable(localSaveFilesTable, localFiles);

        let globalFiles = await connection.invoke('GetGlobalSaveFiles') as FileMetaData[];
        buildFileTable(globalSaveFilesTable, globalFiles);

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

resumeButton.onclick = () => {
    connection.invoke("Resume")
        .then((result) => console.log("resumed:" + result));
}

LoadButton.onclick = () => {
    let checkboxes = document.querySelectorAll('input[name="fileCheckbox"]:checked');

    if (checkboxes.length != 1) {
        alert('Select one file to load.');
        return;
    }

    let checkbox = checkboxes[0];
    let dir = checkbox.getAttribute('data-directory');
    let name = checkbox.getAttribute('data-name');

    let filePath = `${dir}/${name}`;

    connection.invoke("Load", filePath)
        .then((result) => {
            console.log("loaded:");
            console.log(result);
        });
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
            div.classList.add('bg-warning');
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

function buildFileTable(table: HTMLTableElement, files: FileMetaData[]) {
    let body = table.tBodies[0];

    for (let child of body.children) {
        child.remove();
    }

    for (let file of files) {
        let row = document.createElement('tr');

        let cell = document.createElement('td');
        let checkbox = document.createElement('input') as HTMLInputElement;
        checkbox.type = 'checkbox';
        checkbox.name = 'fileCheckbox';
        checkbox.setAttribute('data-directory', file.directory);
        checkbox.setAttribute('data-name', file.name);
        cell.appendChild(checkbox);
        row.appendChild(cell);

        let cell2 = document.createElement('td');
        let link = document.createElement('a') as HTMLAnchorElement;
        link.innerText = file.name;
        link.href = `/admin/servers?handler=file&directory=${file.directory}&name=${file.name}`;
        cell2.appendChild(link);
        row.appendChild(cell2);

        createCell(row, formatDate(file.createdTime));
        createCell(row, formatDate(file.lastModifiedTime));
        createCell(row, file.size.toString());

        body.appendChild(row);
    }
}

function formatDate(dateString: string): string {
    let date = new Date(dateString);
    return date.toUTCString();
}

function createCell(parent: HTMLElement, content: string) {
    let cell = document.createElement('td');
    cell.innerText = content;
    parent.appendChild(cell);
}

let tokenInput = document.querySelector('input[name="__RequestVerificationToken"][type="hidden"]') as HTMLInputElement
let token = tokenInput.value;
console.log(token);

let fileUploadInput = document.getElementById('fileUploadInput') as HTMLInputElement;
let fileUplaodButton = document.getElementById('fileUploadButton') as HTMLButtonElement;

fileUplaodButton.onclick = () => {
    let formData = new FormData();
    let files = fileUploadInput.files
    for (let i = 0; i < files.length; i++) {
        formData.append('files', files[i]);
    }

    //formData.append('files', fileUploadInput.files[0]);

    fetch('/admin/servers?handler=file', {
        method: 'POST',
        body: formData,
        headers: {
            RequestVerificationToken: token
        },
    })
        .then(response => response.json())
        .then(response => console.log('Success:', JSON.stringify(response)))
        .catch(error => console.error('Error:', error));
}