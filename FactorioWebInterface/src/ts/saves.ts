!function () {
    function init(id: string) {
        const table: HTMLTableElement = document.getElementById(id) as HTMLTableElement;

        let body = table.tBodies[0];
        let rows = body.rows;
        let sortProperty = "";

        let rowsCopy: HTMLTableRowElement[] = []
        for (let i = 0; i < rows.length; i++) {
            let r = rows[i];
            rowsCopy.push(r);
        }

        let cells = table.tHead.rows[0].cells;

        cells[0].onclick = () => { sortTable('Name', r => r.children[0].firstChild.textContent.toLowerCase()); };
        cells[1].onclick = () => { sortTable('Date', r => r.children[1].textContent.toLowerCase()); };
        cells[2].onclick = () => { sortTable('Size', r => parseInt(r.children[2].getAttribute('data-size'))); };

        function sortTable(property: string, keySelector: (r: HTMLTableRowElement) => any) {
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
    }

    init('startSavesTable');
    init('finalSavesTable');
    init('oldSavesTable');
}();