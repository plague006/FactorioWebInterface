import * as signalR from "@aspnet/signalr";
import * as $ from "jquery";

!function () {
    interface Ban {
        username: string;
        reason: string;
        admin: string;
        dateTime: string;
    }

    interface Error {
        key: string;
        description: string;
    }

    interface Result {
        success: boolean;
        errors: Error[];
    }

    const table = document.getElementById('bansTable') as HTMLTableElement;

    const addBanForm = document.getElementById('addBanForm') as HTMLFormElement;
    const usernameInput = document.getElementById('usernameInput') as HTMLInputElement;
    const reasonInput = document.getElementById('reasonInput') as HTMLTextAreaElement;
    const adminInput = document.getElementById('adminInput') as HTMLInputElement;
    const dateInput = document.getElementById('dateInput') as HTMLInputElement;
    const timeInput = document.getElementById('timeInput') as HTMLInputElement;
    const synchronizeWithServersCheckbox = document.getElementById('synchronizeWithServersCheckbox') as HTMLInputElement;
    const banCount = document.getElementById('banCount') as HTMLSpanElement;

    const propertySorters = {
        username: r => r.children[0].textContent.toLowerCase(),
        reason: r => r.children[1].textContent.toLowerCase(),
        admin: r => r.children[2].textContent.toLowerCase(),
        datetime: r => r.children[3].textContent
    }

    let dataMap = new Map<string, HTMLTableRowElement>();

    let body = table.tBodies[0];
    let rows = body.rows;
    let ascending = true;
    let sortProperty = "datetime";

    let rowsCopy: HTMLTableRowElement[] = []
    for (let i = 0; i < rows.length; i++) {
        let r = rows[i];
        let cells = r.cells

        rowsCopy.push(r);
        dataMap.set(cells[0].textContent, r);

        r.onclick = onRowClicked;

        let button = cells[4].children[0] as HTMLButtonElement;
        button.onclick = removeBanClick;
    }

    let cells = table.tHead.rows[0].cells;

    cells[0].onclick = () => sortTable('username');
    cells[1].onclick = () => sortTable('reason');
    cells[2].onclick = () => sortTable('admin');
    cells[3].onclick = () => sortTable('datetime');

    updateBanCount();
    sortTable(sortProperty);

    addBanForm.onsubmit = addBanClick;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/factorioBanHub")
        .build();

    async function start() {
        try {
            await connection.start();
        } catch (ex) {
            console.log(ex.message);
            setTimeout(() => start(), 2000);
        }
    }

    connection.onclose(async () => {
        await start();
    });

    connection.on('SendAddBan', async (ban: Ban) => {
        let username = ban.username;
        let dateString = formatDate(ban.dateTime);

        if (dataMap.has(username)) {
            let oldRow = dataMap.get(username);

            oldRow.remove();
            dataMap.delete(username);
            rowsCopy = rowsCopy.filter((e) => e.cells[0].textContent != username);
        }

        let row = document.createElement('tr');
        createCell(row, username);
        createCell(row, ban.reason);
        createCell(row, ban.admin);
        createCell(row, dateString);

        let cell4 = document.createElement('td');
        let button = document.createElement('button') as HTMLButtonElement
        button.innerText = 'Remove';
        button.classList.add('button', 'is-danger');
        button.onclick = removeBanClick;
        cell4.appendChild(button);
        row.appendChild(cell4);

        row.onclick = onRowClicked;

        dataMap.set(username, row);
        rowsCopy.push(row);

        updateBanCount();

        ascending = !ascending;
        sortTable(sortProperty);
    });

    connection.on('SendRemoveBan', async (username: string) => {
        if (dataMap.has(username)) {
            let oldRow = dataMap.get(username);

            oldRow.remove();
            dataMap.delete(username);
            rowsCopy = rowsCopy.filter((e) => e.cells[0].textContent != username);
        }

        updateBanCount();

        ascending = !ascending;
        sortTable(sortProperty);
    });

    async function addBanClick(this: HTMLElement, event: MouseEvent) {
        event.preventDefault();
        event.stopPropagation();

        let dateTime = dateInput.value + 'T' + timeInput.value;

        let ban: Ban = {
            username: usernameInput.value,
            reason: reasonInput.value,
            admin: adminInput.value,
            dateTime: dateTime
        };

        let result = await connection.invoke('AddBan', ban, synchronizeWithServersCheckbox.checked) as Result;
        if (!result.success) {
            alert(JSON.stringify(result.errors));
        }
    }

    function setform(row: HTMLTableRowElement) {
        let cells = row.cells;

        let dateTime = cells[3].textContent.split(' ');

        usernameInput.value = cells[0].textContent;
        reasonInput.value = cells[1].textContent;
        adminInput.value = cells[2].textContent;
        dateInput.value = dateTime[0];
        timeInput.value = dateTime[1];
    }

    async function removeBanClick(this: HTMLElement, event: MouseEvent) {
        event.stopPropagation();

        let parent = this.parentElement.parentElement as HTMLTableRowElement;
        setform(parent);

        let cells = parent.cells

        let username = cells[0].textContent;

        let result = await connection.invoke('RemoveBan', username, synchronizeWithServersCheckbox.checked) as Result;

        if (!result.success) {
            alert(JSON.stringify(result.errors));
        }
    }

    function onRowClicked(this: HTMLTableRowElement) {
        setform(this);
    }

    function createCell(row: HTMLTableRowElement, value: string) {
        let cell = document.createElement('td');
        cell.innerText = value;
        row.appendChild(cell);
    }

    function pad(number) {
        return number < 10 ? '0' + number : number;
    }

    function formatDate(dateString: string): string {
        let date = new Date(dateString);
        let year = pad(date.getUTCFullYear());
        let month = pad(date.getUTCMonth() + 1);
        let day = pad(date.getUTCDate());
        let hour = pad(date.getUTCHours());
        let min = pad(date.getUTCMinutes());
        let sec = pad(date.getUTCSeconds());
        return year + '-' + month + '-' + day + ' ' + hour + ':' + min + ':' + sec;
    }

    function updateBanCount() {
        let length = rowsCopy.length;

        banCount.textContent = '(' + length + ')';
    }

    function sortTable(property: string) {
        let keySelector: (r: HTMLTableRowElement) => any = propertySorters[property];

        if (sortProperty === property) {
            ascending = !ascending;
        } else {
            sortProperty = property;
            ascending = true;
        }

        if (ascending) {
            rowsCopy.sort((a, b) => {
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
            rowsCopy.sort((a, b) => {
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

        for (let i = 0; i < rowsCopy.length; i++) {
            let r = rowsCopy[i];
            body.appendChild(r);
        }
    }

    start();
}();