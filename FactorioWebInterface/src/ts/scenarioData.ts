import * as signalR from "@aspnet/signalr";
import * as $ from "jquery";

!function () {

    interface ScenarioData {
        data_set: string;
        key: string;
        value: string;
    }

    const dataTable = document.getElementById('dataTable') as HTMLTableElement;
    const dataSetsCurrent = document.getElementById('dataSetsCurrent') as HTMLSpanElement;
    const dataSetsDropdownList = document.getElementById('dataSetsDropdownList') as HTMLUListElement;
    const dataSetInput = document.getElementById('dataSetInput') as HTMLInputElement;
    const keyInput = document.getElementById('keyInput') as HTMLInputElement;
    const valueInput = document.getElementById('valueInput') as HTMLTextAreaElement;
    const updateButton = document.getElementById('updateButton') as HTMLButtonElement;
    const refreshDataSets = document.getElementById('refreshDataSets') as HTMLButtonElement;

    let currentDataSet = "";
    let dataMap = new Map<string, HTMLTableRowElement>();

    function createCell(row: HTMLTableRowElement, value: string) {
        let cell = document.createElement('td');
        cell.innerText = value;
        row.appendChild(cell);
    }

    async function reBuildDataSetsDropDown() {
        let dataSets: string[] = await connection.invoke('GetAllDataSets');

        dataSetsDropdownList.innerHTML = "";
        for (let set of dataSets) {
            let item = document.createElement('li');

            let a = document.createElement('a');
            a.textContent = set;
            item.appendChild(a);

            a.onclick = () => {
                dataSetsCurrent.textContent = set;
                currentDataSet = set;
                connection.send('TrackDataSet', set);
                connection.send('RequestAllDataForDataSet', set);
            }

            item.appendChild(a);

            dataSetsDropdownList.appendChild(item);
        }
    }

    function buildDataTable() {
        let cells = dataTable.tHead.rows[0].cells;
        cells[0].onclick = () => sortTable(dataTable, 'key');
        cells[1].onclick = () => sortTable(dataTable, 'value');

        let jTable = $(dataTable);
        jTable.data('key', r => r.children[0].textContent.toLowerCase());
        jTable.data('value', r => r.children[1].textContent.toLowerCase());
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/scenarioDataHub")
        .build();

    async function start() {
        try {
            await connection.start();

            await reBuildDataSetsDropDown();
            dataSetsCurrent.textContent = "Select a data set."
            buildDataTable();

        } catch (ex) {
            console.log(ex.message);
            setTimeout(() => start(), 2000);
        }
    }

    connection.onclose(async () => {
        await start();
    });

    function onRowClicked(this: HTMLTableRowElement) {
        let cells = this.children;

        dataSetInput.value = currentDataSet;
        keyInput.value = cells[0].textContent;
        valueInput.value = cells[1].textContent;
    }

    connection.on('SendAllEntriesForDataSet', (dataSet: string, datas: ScenarioData[]) => {
        if (currentDataSet !== dataSet)
            return;

        let body = dataTable.tBodies[0];

        body.innerHTML = "";
        dataMap = new Map();

        for (let data of datas) {
            let row = document.createElement('tr');
            createCell(row, data.key);
            createCell(row, data.value);
            body.appendChild(row);

            dataMap.set(data.key, row)

            row.onclick = onRowClicked;
        }

        let jTable = $(dataTable);

        let rows: HTMLTableRowElement[] = []
        let rc = body.rows;
        for (let r of rc) {
            rows.push(r);
        }
        jTable.data('rows', rows);

        jTable.data('sortProperty', 'key');
        jTable.data('ascending', false);
        sortTable(dataTable, 'key');
    });

    connection.on('SendEntry', (data: ScenarioData) => {
        if (data.data_set !== currentDataSet) {
            return;
        }

        let jTable = $(dataTable);
        let rows: HTMLTableRowElement[] = jTable.data('rows');

        if (data.value === null || data.value === undefined) {
            if (dataMap.has(data.key)) {
                let row = dataMap.get(data.key);
                row.remove();
                let index = rows.indexOf(row)
                rows.splice(index, 1);
                dataMap.delete(data.key);
            }
        }
        else {

            if (dataMap.has(data.key)) {
                let row = dataMap.get(data.key);
                row.cells[1].innerText = data.value
            } else {
                let body = dataTable.tBodies[0];

                let row = document.createElement('tr');
                createCell(row, data.key);
                createCell(row, data.value);
                body.appendChild(row);

                dataMap.set(data.key, row)

                row.onclick = onRowClicked;

                if (rows) {
                    rows.push(row);
                }
            }

            if (rows) {
                let direction = jTable.data('ascending');
                jTable.data('ascending', !direction);
                let property = jTable.data('sortProperty');
                sortTable(dataTable, property);
            }
        }
    });

    updateButton.onclick = () => {
        let data = {} as ScenarioData;
        data.data_set = dataSetInput.value;
        data.key = keyInput.value;

        let value = valueInput.value;
        if (value.trim() !== "") {
            data.value = value;
        }

        connection.invoke('UpdateData', data);
    };

    refreshDataSets.onclick = async () => {
        await reBuildDataSetsDropDown();
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

    start();
}();
