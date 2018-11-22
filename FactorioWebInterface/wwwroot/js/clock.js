function GetClock() {
    var d = new Date();
    document.getElementById('time').innerHTML = d.toUTCString().substring(17, 26);
}

$(document).ready(function () {
    GetClock();
    setInterval(GetClock, 100);
})


