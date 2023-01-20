update();
setInterval(update, 2500);

function update() {
    try {
        if (document.querySelector("#number")) {
            window.location.href = location.href.replace('https', 'http');
            var xhr = new XMLHttpRequest();
            xhr.open("get", "http://count.ongsat.com/api/online/queryCount?uri=127469ef347447698dd74c449881b877", false);
            xhr.send();
            if (!/\s+?/.test(xhr.responseText)) {
                console.log(xhr.responseText);
                let returnJson = JSON.parse(xhr.responseText.trim());
                document.querySelector("#number").innerHTML = returnJson.data.count;
                document.querySelector("#notice-msg").innerHTML = "因为此功能于<code>v1.3.3.1</code>才被加入，可能导致计数不准确";
            }
        }
    } catch (e) {
        document.querySelector("#notice-msg").innerHTML = e;
        console.error(e);
    }
}