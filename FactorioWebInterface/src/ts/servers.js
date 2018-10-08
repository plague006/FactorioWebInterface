var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
import * as signalR from "@aspnet/signalr";
var MessageType;
(function (MessageType) {
    MessageType[MessageType["Output"] = 0] = "Output";
    MessageType[MessageType["Wrapper"] = 1] = "Wrapper";
    MessageType[MessageType["Control"] = 2] = "Control";
    MessageType[MessageType["Status"] = 3] = "Status";
    MessageType[MessageType["Discord"] = 4] = "Discord";
})(MessageType || (MessageType = {}));
const maxMessageCount = 100;
const divMessages = document.querySelector("#divMessages");
const tbMessage = document.querySelector("#tbMessage");
const btnSend = document.querySelector("#btnSend");
const serverIdInput = document.getElementById('serverIdInput');
const resumeButton = document.getElementById('resumeButton');
const LoadButton = document.getElementById('loadButton');
const stopButton = document.getElementById('stopButton');
const forceStopButton = document.getElementById('forceStopButton');
const getStatusButton = document.getElementById('getStatusButton');
const statusText = document.getElementById('statusText');
const tempSaveFilesTable = document.getElementById('tempSaveFilesTable');
const localSaveFilesTable = document.getElementById('localSaveFilesTable');
const globalSaveFilesTable = document.getElementById('globalSaveFilesTable');
let messageCount = 0;
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/FactorioControlHub")
    .build();
function init() {
    return __awaiter(this, void 0, void 0, function* () {
        try {
            yield connection.start();
            let data = yield connection.invoke('SetServerId', serverIdInput.value);
            statusText.value = data.status;
            let tempFiles = yield connection.invoke('GetTempSaveFiles');
            buildFileTable(tempSaveFilesTable, tempFiles);
            let localFiles = yield connection.invoke('GetLocalSaveFiles');
            buildFileTable(localSaveFilesTable, localFiles);
            let globalFiles = yield connection.invoke('GetGlobalSaveFiles');
            buildFileTable(globalSaveFilesTable, globalFiles);
            for (let message of data.messages) {
                writeMessage(message);
            }
        }
        catch (ex) {
            console.log(ex.message);
        }
    });
}
init();
connection.on("SendMessage", writeMessage);
connection.on('FactorioStatusChanged', (newStatus, oldStatus) => {
    console.log(`new: ${newStatus}, old: ${oldStatus}`);
    statusText.value = newStatus;
});
tbMessage.addEventListener("keyup", (e) => {
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
};
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
};
stopButton.onclick = () => {
    connection.invoke("Stop")
        .then(() => console.log("stopped"));
};
forceStopButton.onclick = () => {
    connection.invoke("ForceStop")
        .then(() => console.log("force stopped"));
};
getStatusButton.onclick = () => {
    connection.invoke("GetStatus");
};
function writeMessage(message) {
    let div = document.createElement("div");
    let data;
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
        let first = divMessages.firstChild;
        divMessages.removeChild(first);
    }
    else {
        messageCount++;
    }
    divMessages.appendChild(div);
    divMessages.scrollTop = divMessages.scrollHeight;
}
function buildFileTable(table, files) {
    let body = table.tBodies[0];
    for (let child of body.children) {
        child.remove();
    }
    for (let file of files) {
        let row = document.createElement('tr');
        let cell = document.createElement('td');
        let checkbox = document.createElement('input');
        checkbox.type = 'checkbox';
        checkbox.name = 'fileCheckbox';
        checkbox.setAttribute('data-directory', file.directory);
        checkbox.setAttribute('data-name', file.name);
        cell.appendChild(checkbox);
        row.appendChild(cell);
        let cell2 = document.createElement('td');
        let link = document.createElement('a');
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
function formatDate(dateString) {
    let date = new Date(dateString);
    return date.toUTCString();
}
function createCell(parent, content) {
    let cell = document.createElement('td');
    cell.innerText = content;
    parent.appendChild(cell);
}
//# sourceMappingURL=servers.js.map