!function () {
    const table: HTMLTableElement = document.getElementById('adminsTable') as HTMLTableElement;

    let body = table.tBodies[0];
    let rows = body.rows;
    let sortProperty = 'name';

    let rowsCopy: HTMLTableRowElement[] = []
    for (let i = 0; i < rows.length; i++) {
        let r = rows[i];
        rowsCopy.push(r);
    }

    let cells = table.tHead.rows[0].cells;

    let nameClick = () => { sortTable('name', r => r.children[0].textContent.toLowerCase()); };

    cells[0].onclick = nameClick;

    nameClick();

    function sortTable(property: string, keySelector: (r: HTMLTableRowElement) => string) {
        if (sortProperty === property) {
            sortProperty = "";
            rowsCopy.sort((a, b) => { return keySelector(a) > keySelector(b) ? 1 : -1; });
        } else {
            sortProperty = property;
            rowsCopy.sort((a, b) => { return keySelector(a) < keySelector(b) ? 1 : -1; });
        }

        body.innerHTML = "";

        for (let i = 0; i < rowsCopy.length; i++) {
            let r = rowsCopy[i];
            body.appendChild(r);
        }
    }
}();