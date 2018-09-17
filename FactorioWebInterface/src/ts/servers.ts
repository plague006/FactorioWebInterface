import * as signalR from "@aspnet/signalr";

const divMessages: HTMLDivElement = document.querySelector("#divMessages");
const tbMessage: HTMLInputElement = document.querySelector("#tbMessage");
const btnSend: HTMLButtonElement = document.querySelector("#btnSend");
const username = new Date().getTime();

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/FactorioControlHub")
    .build();

connection.start()
    .then(() => {
        connection.invoke("SetServerId", "1");
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