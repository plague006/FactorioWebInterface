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
    directory: string;
    createdTime: string;
    lastModifiedTime: string;
    size: number;
}

interface ScenarioMetaData {
    name: string;
    createdTime: string;
    lastModifiedTime: string;
}

interface FactorioContorlClientData {
    status: string;
    messages: MessageData[];
}

interface Error {
    key: string;
    description: string;
}

interface Result {
    success: boolean;
    errors: Error[];
}

interface FactorioServerSettings {
    name: string;
    description: string;
    tags: string[];
    max_players: number;
    game_password: string;
    auto_pause: boolean;
    admins: string[];
}

const maxMessageCount = 100;

const divMessages: HTMLDivElement = document.querySelector("#divMessages");
const tbMessage: HTMLInputElement = document.querySelector("#tbMessage");
const btnSend: HTMLButtonElement = document.querySelector("#btnSend");
const serverName = document.getElementById('serverName') as HTMLHeadingElement;
const serverIdInput: HTMLInputElement = document.getElementById('serverIdInput') as HTMLInputElement;
const resumeButton: HTMLButtonElement = document.getElementById('resumeButton') as HTMLButtonElement;
const loadButton: HTMLButtonElement = document.getElementById('loadButton') as HTMLButtonElement;
const startScenarioButton: HTMLButtonElement = document.getElementById('startScenarioButton') as HTMLButtonElement;
const stopButton: HTMLButtonElement = document.getElementById('stopButton') as HTMLButtonElement;
const saveButton: HTMLButtonElement = document.getElementById('saveButton') as HTMLButtonElement;
const updateButton: HTMLButtonElement = document.getElementById('updateButton') as HTMLButtonElement;
const forceStopButton: HTMLButtonElement = document.getElementById('forceStopButton') as HTMLButtonElement;
const getStatusButton: HTMLButtonElement = document.getElementById('getStatusButton') as HTMLButtonElement;
const statusText: HTMLInputElement = document.getElementById('statusText') as HTMLInputElement;

const tempSaveFilesTable: HTMLTableElement = document.getElementById('tempSaveFilesTable') as HTMLTableElement;
const localSaveFilesTable: HTMLTableElement = document.getElementById('localSaveFilesTable') as HTMLTableElement;
const globalSaveFilesTable: HTMLTableElement = document.getElementById('globalSaveFilesTable') as HTMLTableElement;
const scenarioTable: HTMLTableElement = document.getElementById('scenarioTable') as HTMLTableElement;
const logsFileTable: HTMLTableElement = document.getElementById('logsFileTable') as HTMLTableElement;

// XSRF/CSRF token, see https://docs.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-2.1
let requestVerificationToken = (document.querySelector('input[name="__RequestVerificationToken"][type="hidden"]') as HTMLInputElement).value

const fileUploadInput = document.getElementById('fileUploadInput') as HTMLInputElement;
const fileUplaodButton = document.getElementById('fileUploadButton') as HTMLButtonElement;
const fileDeleteButton = document.getElementById('fileDeleteButton') as HTMLButtonElement;
const fileMoveButton = document.getElementById('fileMoveButton') as HTMLButtonElement;
const fileCopyButton = document.getElementById('fileCopyButton') as HTMLButtonElement;
const destinationSelect = document.getElementById('destinationSelect') as HTMLSelectElement;
const saveRenameButton = document.getElementById('saveRenameButton') as HTMLButtonElement;
const saveDeflateButton = document.getElementById('saveDeflateButton') as HTMLButtonElement;
const fileRenameInput = document.getElementById('fileRenameInput') as HTMLInputElement;
const fileProgress = document.getElementById('fileProgress') as HTMLProgressElement;
const fileProgressContiner = document.getElementById('fileProgressContiner') as HTMLSpanElement;
const deflateProgress = document.getElementById('deflateProgress') as HTMLSpanElement;

const configNameInput = document.getElementById('configNameInput') as HTMLInputElement;
const configDescriptionInput = document.getElementById('configDescriptionInput') as HTMLInputElement;
const configTagsInput = document.getElementById('configTagsInput') as HTMLElement;
const configMaxPlayersInput = document.getElementById('configMaxPlayersInput') as HTMLInputElement;
const configPasswordInput = document.getElementById('configPasswordInput') as HTMLInputElement;
const configPauseInput = document.getElementById('configPauseInput') as HTMLInputElement;
const configAdminInput = document.getElementById('configAdminInput') as HTMLTextAreaElement;
const configSaveButton = document.getElementById('configSaveButton') as HTMLButtonElement;

let messageCount = 0;

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/FactorioControlHub")
    .build();

async function getFiles() {
    let tempFiles = await connection.invoke('GetTempSaveFiles') as FileMetaData[];
    buildFileTable(tempSaveFilesTable, tempFiles);
    updateFileTable(tempSaveFilesTable, tempFiles);

    let localFiles = await connection.invoke('GetLocalSaveFiles') as FileMetaData[];
    buildFileTable(localSaveFilesTable, localFiles);
    updateFileTable(localSaveFilesTable, localFiles);

    let globalFiles = await connection.invoke('GetGlobalSaveFiles') as FileMetaData[];
    buildFileTable(globalSaveFilesTable, globalFiles);
    updateFileTable(globalSaveFilesTable, globalFiles);
}

async function updateAllSaveFileTables() {
    let tempFiles = await connection.invoke('GetTempSaveFiles') as FileMetaData[];
    updateFileTable(tempSaveFilesTable, tempFiles);

    let localFiles = await connection.invoke('GetLocalSaveFiles') as FileMetaData[];
    updateFileTable(localSaveFilesTable, localFiles);

    let globalFiles = await connection.invoke('GetGlobalSaveFiles') as FileMetaData[];
    updateFileTable(globalSaveFilesTable, globalFiles);
}

async function getScenarios() {
    let scenarios = await connection.invoke('GetScenarios') as ScenarioMetaData[];
    buildScenarioTable(scenarioTable);
    updateBuildScenarioTable(scenarioTable, scenarios);
}

async function getLogs() {
    let logs = await connection.invoke('GetLogFiles') as FileMetaData[];
    buildLogFileTable(logsFileTable);
    updateLogFileTable(logsFileTable, logs)
}

function MakeTagInput(value: string) {
    let listItem = document.createElement('li');
    let input = document.createElement('input');
    listItem.appendChild(input);

    input.value = value;

    return listItem;
}

async function getSettings() {
    let settings = await connection.invoke('GetServerSettings') as FactorioServerSettings;

    configNameInput.value = settings.name;
    configDescriptionInput.value = settings.description;

    configTagsInput.innerHTML = '';

    for (let item of settings.tags) {
        let input = MakeTagInput(item);
        configTagsInput.appendChild(input);
    }

    let lastInput = MakeTagInput('');
    configTagsInput.appendChild(lastInput);

    configMaxPlayersInput.value = settings.max_players + "";
    configPasswordInput.value = settings.game_password;
    configPauseInput.checked = settings.auto_pause;
    configAdminInput.value = settings.admins.join(', ');

    serverName.innerText = settings.name;
}

async function init() {
    try {
        await connection.start();
        let data = await connection.invoke('SetServerId', serverIdInput.value) as FactorioContorlClientData;

        getFiles();
        getScenarios();
        getLogs();
        getSettings();

        statusText.value = data.status;

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
        .then((result: Result) => {
            if (!result.success) {
                alert(JSON.stringify(result.errors));                
            }
        });
}

loadButton.onclick = () => {
    let checkboxes = document.querySelectorAll('input[name="fileCheckbox"]:checked');

    if (checkboxes.length != 1) {
        alert('Select one file to load.');
        return;
    }

    let checkbox = checkboxes[0];
    let dir = checkbox.getAttribute('data-directory');
    let name = checkbox.getAttribute('data-name');

    connection.invoke("Load", dir, name)
        .then((result: Result) => {
            if (!result.success) {
                alert(JSON.stringify(result.errors));
            }
        });
}

startScenarioButton.onclick = () => {
    let checkboxes = document.querySelectorAll('input[name="scenarioCheckbox"]:checked');

    if (checkboxes.length != 1) {
        alert('Select one scenario to start.');
        return;
    }

    let checkbox = checkboxes[0];
    let name = checkbox.getAttribute('data-name');

    connection.invoke("StartScenario", name)
        .then((result: Result) => {
            if (!result.success) {
                alert(JSON.stringify(result.errors));
            }
        });
}

stopButton.onclick = () => {
    connection.invoke("Stop")
        .then((result: Result) => {
            if (!result.success) {
                alert(JSON.stringify(result.errors));
            }
        });
}

saveButton.onclick = () => {
    connection.invoke("Save")
        .then((result: Result) => {
            if (!result.success) {
                alert(JSON.stringify(result.errors));
            }
        });
};

updateButton.onclick = () => {
    connection.invoke("Update")
        .then((result: Result) => {
            if (!result.success) {
                alert(JSON.stringify(result.errors));
            }
        });
};

forceStopButton.onclick = () => {
    connection.invoke("ForceStop")
        .then((result: Result) => {
            if (!result.success) {
                alert(JSON.stringify(result.errors));
            }
        });
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

const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
function bytesToSize(bytes: number) {
    // https://gist.github.com/lanqy/5193417

    if (bytes === 0)
        return 'n/a';
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    if (i === 0)
        return `${bytes} ${sizes[i]})`;
    else
        return `${(bytes / (1024 ** i)).toFixed(1)} ${sizes[i]}`;
}

function buildFileTable(table: HTMLTableElement, files: FileMetaData[]) {
    let cells = table.tHead.rows[0].cells;

    let input = cells[0].firstChild as HTMLInputElement;
    input.onchange = () => toggleSelectTable(input, table);

    cells[0].onclick = () => sortTable(table, 'select');
    cells[1].onclick = () => sortTable(table, 'name');
    cells[2].onclick = () => sortTable(table, 'createdTime');
    cells[3].onclick = () => sortTable(table, 'lastModifiedTime');
    cells[4].onclick = () => sortTable(table, 'size');

    let jTable = $(table);

    jTable.data('select', r => {
        let value = r.children[0].firstChild as HTMLInputElement;
        return value.checked ? 1 : 0;
    });

    jTable.data('name', r => r.children[1].firstChild.textContent.toLowerCase());
    jTable.data('createdTime', r => r.children[2].getAttribute('data-date'));
    jTable.data('lastModifiedTime', r => r.children[3].getAttribute('data-date'));
    jTable.data('size', r => parseInt(r.children[4].getAttribute('data-size')));

    jTable.data('sortProperty', 'lastModifiedTime');
    jTable.data('ascending', false);
}

function updateFileTable(table: HTMLTableElement, files: FileMetaData[]) {
    let body = table.tBodies[0];

    body.innerHTML = "";

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

        let cell3 = document.createElement('td');
        cell3.innerText = formatDate(file.createdTime);
        cell3.setAttribute('data-date', file.createdTime);
        row.appendChild(cell3);

        let cell4 = document.createElement('td');
        cell4.innerText = formatDate(file.lastModifiedTime);
        cell4.setAttribute('data-date', file.lastModifiedTime);
        row.appendChild(cell4);

        let cell5 = document.createElement('td');
        cell5.innerText = bytesToSize(file.size);
        cell5.setAttribute('data-size', file.size.toString());
        row.appendChild(cell5);

        body.appendChild(row);
    }

    let jTable = $(table);

    let rows: HTMLTableRowElement[] = []
    let rc = body.rows;
    for (let r of rc) {
        rows.push(r);
    }
    jTable.data('rows', rows);

    let ascending = !jTable.data('ascending');
    jTable.data('ascending', ascending);
    let property = jTable.data('sortProperty');

    sortTable(table, property);
}

function buildLogFileTable(table: HTMLTableElement) {
    let cells = table.tHead.rows[0].cells;

    cells[0].onclick = () => sortTable(table, 'name');
    cells[1].onclick = () => sortTable(table, 'createdTime');
    cells[2].onclick = () => sortTable(table, 'lastModifiedTime');
    cells[3].onclick = () => sortTable(table, 'size');

    let jTable = $(table);

    jTable.data('name', r => r.children[0].firstChild.textContent.toLowerCase());
    jTable.data('createdTime', r => r.children[1].getAttribute('data-date'));
    jTable.data('lastModifiedTime', r => r.children[2].getAttribute('data-date'));
    jTable.data('size', r => parseInt(r.children[3].getAttribute('data-size')));

    jTable.data('sortProperty', 'lastModifiedTime');
    jTable.data('ascending', false);
}

function updateLogFileTable(table: HTMLTableElement, files: FileMetaData[]) {
    let body = table.tBodies[0];

    body.innerHTML = "";

    for (let file of files) {
        let row = document.createElement('tr');

        let cell2 = document.createElement('td');
        let link = document.createElement('a') as HTMLAnchorElement;
        link.innerText = file.name;
        link.href = `/admin/servers?handler=logFile&directory=${file.directory}&name=${file.name}`;
        cell2.appendChild(link);
        row.appendChild(cell2);

        let cell3 = document.createElement('td');
        cell3.innerText = formatDate(file.createdTime);
        cell3.setAttribute('data-date', file.createdTime);
        row.appendChild(cell3);

        let cell4 = document.createElement('td');
        cell4.innerText = formatDate(file.lastModifiedTime);
        cell4.setAttribute('data-date', file.lastModifiedTime);
        row.appendChild(cell4);

        let cell5 = document.createElement('td');
        cell5.innerText = bytesToSize(file.size);
        cell5.setAttribute('data-size', file.size.toString());
        row.appendChild(cell5);

        body.appendChild(row);
    }

    let jTable = $(table);

    let rows: HTMLTableRowElement[] = []
    let rc = body.rows;
    for (let r of rc) {
        rows.push(r);
    }
    jTable.data('rows', rows);

    let ascending = !jTable.data('ascending');
    jTable.data('ascending', ascending);
    let property = jTable.data('sortProperty');

    sortTable(table, property);
}

function buildScenarioTable(table: HTMLTableElement) {
    let cells = table.tHead.rows[0].cells;

    let input = cells[0].firstChild as HTMLInputElement;
    input.onchange = () => toggleSelectTable(input, table);

    cells[0].onclick = () => sortTable(table, 'select');
    cells[1].onclick = () => sortTable(table, 'name');
    cells[2].onclick = () => sortTable(table, 'createdTime');
    cells[3].onclick = () => sortTable(table, 'lastModifiedTime');

    let jTable = $(table);

    jTable.data('select', r => {
        let value = r.children[0].firstChild as HTMLInputElement;
        return value.checked ? 1 : 0;
    });

    jTable.data('name', r => r.children[1].firstChild.textContent.toLowerCase());
    jTable.data('createdTime', r => r.children[2].getAttribute('data-date'));
    jTable.data('lastModifiedTime', r => r.children[3].getAttribute('data-date'));

    jTable.data('sortProperty', 'lastModifiedTime');
    jTable.data('ascending', false);
}

function updateBuildScenarioTable(table: HTMLTableElement, scenarios: ScenarioMetaData[]) {
    let body = table.tBodies[0];

    body.innerHTML = "";

    for (let scenario of scenarios) {
        let row = document.createElement('tr');

        let cell = document.createElement('td');
        let checkbox = document.createElement('input') as HTMLInputElement;
        checkbox.type = 'checkbox';
        checkbox.name = 'scenarioCheckbox';
        checkbox.setAttribute('data-name', scenario.name);
        cell.appendChild(checkbox);
        row.appendChild(cell);

        createCell(row, scenario.name);

        let cell3 = document.createElement('td');
        cell3.innerText = formatDate(scenario.createdTime);
        cell3.setAttribute('data-date', scenario.createdTime);
        row.appendChild(cell3);

        let cell4 = document.createElement('td');
        cell4.innerText = formatDate(scenario.lastModifiedTime);
        cell4.setAttribute('data-date', scenario.lastModifiedTime);
        row.appendChild(cell4);

        body.appendChild(row);
    }

    let jTable = $(table);

    let rows: HTMLTableRowElement[] = []
    let rc = body.rows;
    for (let r of rc) {
        rows.push(r);
    }
    jTable.data('rows', rows);

    let ascending = !jTable.data('ascending');
    jTable.data('ascending', ascending);
    let property = jTable.data('sortProperty');

    sortTable(table, property);
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

fileUplaodButton.onclick = () => {
    fileUploadInput.click();
}

fileUploadInput.onchange = function (this: HTMLInputElement, ev: Event) {
    if (this.files.length == 0) {
        return;
    }

    let formData = new FormData();
    formData.set('directory', `${serverIdInput.value}/local_saves`);

    let files = fileUploadInput.files
    for (let i = 0; i < files.length; i++) {
        formData.append('files', files[i]);
    }

    let xhr = new XMLHttpRequest();
    xhr.open('POST', '/admin/servers?handler=fileUpload', true);
    xhr.setRequestHeader('RequestVerificationToken', requestVerificationToken);

    xhr.upload.addEventListener('loadstart', function (event) {
        fileProgressContiner.hidden = false;
        fileProgress.value = 0;
    }, false);

    xhr.upload.addEventListener("progress", function (event) {
        fileProgress.value = event.loaded / event.total;
    }, false);

    xhr.onloadend = function (event) {
        fileProgressContiner.hidden = true;
        updateAllSaveFileTables();

        var result = JSON.parse(xhr.responseText) as Result;
        if (!result.success) {
            alert(JSON.stringify(result.errors))
        }
    }

    xhr.send(formData);
};

fileDeleteButton.onclick = async () => {
    let checkboxes = document.querySelectorAll('input[name="fileCheckbox"]:checked');

    if (checkboxes.length == 0) {
        alert('Please select saves to delete.');
        return;
    }

    let files = [];

    for (let checkbox of checkboxes) {
        let dir = checkbox.getAttribute('data-directory');
        let name = checkbox.getAttribute('data-name');

        let filePath = `${dir}/${name}`;

        files.push(filePath);
    }

    let result: Result = await connection.invoke('DeleteFiles', files);

    if (!result.success) {
        alert(JSON.stringify(result.errors));
    }

    updateAllSaveFileTables();
}

fileMoveButton.onclick = async () => {
    let checkboxes = document.querySelectorAll('input[name="fileCheckbox"]:checked');

    if (checkboxes.length == 0) {
        alert('Please select saves to move.');
        return;
    }

    let files = [];

    for (let checkbox of checkboxes) {
        let dir = checkbox.getAttribute('data-directory');
        let name = checkbox.getAttribute('data-name');

        let filePath = `${dir}/${name}`;

        files.push(filePath);
    }

    let destination = destinationSelect.options[destinationSelect.selectedIndex].value;

    let result: Result = await connection.invoke('MoveFiles', destination, files);

    if (!result.success) {
        alert(JSON.stringify(result.errors));
    }

    updateAllSaveFileTables();
}

fileCopyButton.onclick = async () => {
    let checkboxes = document.querySelectorAll('input[name="fileCheckbox"]:checked');

    if (checkboxes.length == 0) {
        alert('Please select saves to copy.');
        return;
    }

    let files = [];

    for (let checkbox of checkboxes) {
        let dir = checkbox.getAttribute('data-directory');
        let name = checkbox.getAttribute('data-name');

        let filePath = `${dir}/${name}`;

        files.push(filePath);
    }

    let destination = destinationSelect.options[destinationSelect.selectedIndex].value;

    let result: Result = await connection.invoke('CopyFiles', destination, files);

    if (!result.success) {
        alert(JSON.stringify(result.errors));
    }

    updateAllSaveFileTables();
}

saveRenameButton.onclick = async () => {
    let checkboxes = document.querySelectorAll('input[name="fileCheckbox"]:checked');

    if (checkboxes.length != 1) {
        alert('Please select one file to rename.');
        return;
    }

    let newName = fileRenameInput.value.trim();
    if (newName === "") {
        alert('New name cannot be empty');
        return;
    }

    let checkbox = checkboxes[0];
    let dir = checkbox.getAttribute('data-directory');
    let name = checkbox.getAttribute('data-name');

    let result: Result = await connection.invoke('RenameFile', dir, name, newName);

    if (!result.success) {
        alert(JSON.stringify(result.errors));
    }

    updateAllSaveFileTables();
}

saveDeflateButton.onclick = async () => {
    let checkboxes = document.querySelectorAll('input[name="fileCheckbox"]:checked');

    if (checkboxes.length != 1) {
        alert('Please select one file to deflate.');
        return;
    }

    let newName = fileRenameInput.value.trim();

    let checkbox = checkboxes[0];
    let dir = checkbox.getAttribute('data-directory');
    let name = checkbox.getAttribute('data-name');

    let result: Result = await connection.invoke('DeflateSave', dir, name, newName);
    if (!result.success) {
        alert(JSON.stringify(result.errors));
        return;
    }

    deflateProgress.hidden = false;
}

connection.on('DeflateFinished', (result: Result) => {
    deflateProgress.hidden = true;
    updateAllSaveFileTables();

    if (!result.success) {
        alert(JSON.stringify(result.errors));
    }
});

configTagsInput.oninput = function (this, e: Event) {
    let target = e.target as HTMLInputElement;
    let bottomInput = configTagsInput.lastChild.firstChild;

    if (target === bottomInput) {
        let lastInput = MakeTagInput('');
        configTagsInput.appendChild(lastInput);
    }
}

configSaveButton.onclick = async () => {

    let tags = [];

    for (let child of configTagsInput.children) {
        let input = child.firstChild as HTMLInputElement;
        let value = input.value.trim();
        if (value !== '') {
            tags.push(value);
        }
    }

    let max_players = parseInt(configMaxPlayersInput.value);
    if (isNaN(max_players)) {
        max_players = 0;
    }

    let settings: FactorioServerSettings = {
        name: configNameInput.value,
        description: configDescriptionInput.value,
        tags: tags,
        max_players: max_players,
        game_password: configPasswordInput.value,
        auto_pause: configPauseInput.checked,
        admins: configAdminInput.value.split(',')
    };

    let result: Result = await connection.invoke('SaveServerSettings', settings);

    if (!result.success) {
        alert(JSON.stringify(result.errors));
    }

    await getSettings();
}

function toggleSelectTable(input: HTMLInputElement, table: HTMLTableElement) {
    let checkboxes = table.querySelectorAll('input[type="checkbox"]') as NodeListOf<HTMLInputElement>;

    for (let checkbox of checkboxes) {
        checkbox.checked = input.checked;
    }
}

function sortTable(table: HTMLTableElement, property: string) {
    let jTable = $(table);

    let rows: HTMLTableRowElement[] = jTable.data('rows');
    let keySelector: (r: HTMLTableRowElement) => any = jTable.data(property);

    let sortProperty = jTable.data('sortProperty');

    let ascending: boolean;
    if (sortProperty === property) {
        ascending = !jTable.data('ascending');
        jTable.data('ascending', ascending);
    } else {
        jTable.data('sortProperty', property);
        ascending = true;
        jTable.data('ascending', ascending);
    }

    if (ascending) {
        rows.sort((a, b) => {
            let left = keySelector(a);
            let right = keySelector(b);
            if (left === right) {
                return 0;
            } else if (left > right) {
                return 1;
            } else {
                return -1;
            }
        });
    } else {
        rows.sort((a, b) => {
            let left = keySelector(a);
            let right = keySelector(b);
            if (left === right) {
                return 0;
            } else if (left > right) {
                return -1;
            } else {
                return 1;
            }
        });
    }

    let body = table.tBodies[0];
    body.innerHTML = "";

    for (let r of rows) {
        body.appendChild(r);
    }
}


