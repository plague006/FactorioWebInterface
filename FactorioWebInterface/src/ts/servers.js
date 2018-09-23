var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
import * as signalR from "@aspnet/signalr";
const divMessages = document.querySelector("#divMessages");
const tbMessage = document.querySelector("#tbMessage");
const btnSend = document.querySelector("#btnSend");
const serverIdInput = document.getElementById('serverIdInput');
const startButton = document.getElementById('startButton');
const stopButton = document.getElementById('stopButton');
const forceStopButton = document.getElementById('forceStopButton');
const getStatusButton = document.getElementById('getStatusButton');
const statusText = document.getElementById('statusText');
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/FactorioControlHub")
    .build();
function init() {
    return __awaiter(this, void 0, void 0, function* () {
        try {
            yield connection.start();
            let data = yield connection.invoke("SetServerId", serverIdInput.value);
            statusText.value = data.status;
        }
        catch (ex) {
            console.log(ex.message);
        }
    });
}
init();
connection.on("FactorioOutputData", (data) => {
    let m = document.createElement("div");
    m.innerHTML =
        `<div>${data}</div>`;
    divMessages.appendChild(m);
    divMessages.scrollTop = divMessages.scrollHeight;
});
connection.on("FactorioWrapperOutputData", (data) => {
    let m = document.createElement("div");
    m.innerHTML =
        `<div>Wrapper: ${data}</div>`;
    divMessages.appendChild(m);
    divMessages.scrollTop = divMessages.scrollHeight;
});
connection.on("FactorioWebInterfaceData", (data) => {
    let m = document.createElement("div");
    m.innerHTML =
        `<div>Web: ${data}</div>`;
    divMessages.appendChild(m);
    divMessages.scrollTop = divMessages.scrollHeight;
});
connection.on('FactorioStatusChanged', (newStatus, oldStatus) => {
    console.log(`new: ${newStatus}, old: ${oldStatus}`);
    statusText.value = newStatus;
    let m = document.createElement("div");
    m.innerHTML =
        `<div>[STATUS]: Changed from ${oldStatus} to ${newStatus}</div>`;
    divMessages.appendChild(m);
    divMessages.scrollTop = divMessages.scrollHeight;
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
startButton.onclick = () => {
    connection.invoke("Start")
        .then(() => console.log("started"));
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
    connection.invoke("GetStatus").then((data) => {
        console.log(`status: ${data}`);
        statusText.value = data;
    });
};
//# sourceMappingURL=servers.js.map