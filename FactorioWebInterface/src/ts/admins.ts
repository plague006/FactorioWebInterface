!function () {
    const table: HTMLTableElement = document.getElementById('adminsTable') as HTMLTableElement;

    let body = table.tBodies[0];
    let rows = body.rows;
    let sortProperty = "";

    let rowsCopy: HTMLTableRowElement[] = []
    for (let i = 0; i < rows.length; i++) {
        let r = rows[i];
        rowsCopy.push(r);
    }

    let cells = table.tHead.rows[0].cells;

    cells[0].onclick = () => { sortTable('name', r => r.children[0].textContent.toLowerCase()); };

    function sortTable(property: string, keySelector: (r: HTMLTableRowElement) => string) {
        if (sortProperty === property) {
            sortProperty = "";
            rowsCopy.sort((a, b) => { return keySelector(a) > keySelector(b) ? 1 : -1; });
        } else {
            sortProperty = property;
            rowsCopy.sort((a, b) => { return keySelector(a) < keySelector(b) ? 1 : -1; });
        }

        body.innerHTML = "";

        for (let i = 0; i < rows.length; i++) {
            let r = rows[i];
            body.appendChild(r);
        }
    }
}();