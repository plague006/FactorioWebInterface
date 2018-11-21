!function () {
    const table: HTMLTableElement = document.getElementById('bansTable') as HTMLTableElement;

    let body = table.tBodies[0];
    let rows = body.rows;
    let sortProperty = "";

    let rowsCopy: HTMLTableRowElement[] = []
    for (let r of rows) {
        rowsCopy.push(r);
    }

    let cells = table.tHead.rows[0].cells;

    cells[0].onclick = () => { sortTable('username', r => r.children[0].textContent.toLowerCase()); };
    cells[1].onclick = () => { sortTable('reason', r => r.children[1].textContent.toLowerCase()); };
    cells[2].onclick = () => { sortTable('admin', r => r.children[2].textContent.toLowerCase()); };
    cells[3].onclick = () => { sortTable('datetime', r => r.children[3].textContent); };

    function sortTable(property: string, keySelector: (r: HTMLTableRowElement) => string) {
        if (sortProperty === property) {
            sortProperty = "";
            rowsCopy.sort((a, b) => { return keySelector(a) > keySelector(b) ? 1 : -1; });
        } else {
            sortProperty = property;
            rowsCopy.sort((a, b) => { return keySelector(a) < keySelector(b) ? 1 : -1; });
        }

        body.innerHTML = "";

        for (let r of rowsCopy) {
            body.appendChild(r);
        }
    }
}();