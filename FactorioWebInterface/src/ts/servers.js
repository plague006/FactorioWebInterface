"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var signalR = require("@aspnet/signalr");
var divMessages = document.querySelector("#divMessages");
var tbMessage = document.querySelector("#tbMessage");
var btnSend = document.querySelector("#btnSend");
var serverIdInput = document.getElementById('serverIdInput');
var startButton = document.getElementById('startButton');
var stopButton = document.getElementById('stopButton');
var forceStopButton = document.getElementById('forceStopButton');
var connection = new signalR.HubConnectionBuilder()
    .withUrl("/FactorioControlHub")
    .build();
connection.start()
    .then(function () {
    connection.invoke("SetServerId", serverIdInput.value);
})
    .catch(function (err) { return document.write(err); });
connection.on("FactorioOutputData", function (data) {
    var m = document.createElement("div");
    m.innerHTML =
        "<div>" + data + "</div>";
    divMessages.appendChild(m);
    divMessages.scrollTop = divMessages.scrollHeight;
});
tbMessage.addEventListener("keyup", function (e) {
    if (e.keyCode === 13) {
        send();
    }
});
btnSend.addEventListener("click", send);
function send() {
    connection.send("SendToFactorio", tbMessage.value)
        .then(function () { return tbMessage.value = ""; });
}
startButton.onclick = function () {
    connection.invoke("Start")
        .then(function () { return console.log("started"); });
};
stopButton.onclick = function () {
    connection.invoke("Stop")
        .then(function () { return console.log("stopped"); });
};
forceStopButton.onclick = function () {
    connection.invoke("ForceStop")
        .then(function () { return console.log("force stopped"); });
};
//# sourceMappingURL=servers.js.map