"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var signalR = require("@aspnet/signalr");
var divMessages = document.querySelector("#divMessages");
var tbMessage = document.querySelector("#tbMessage");
var btnSend = document.querySelector("#btnSend");
var username = new Date().getTime();
var connection = new signalR.HubConnectionBuilder()
    .withUrl("/FactorioControlHub")
    .build();
connection.start()
    .then(function () {
    connection.invoke("SetServerId", "1");
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
