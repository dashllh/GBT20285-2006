document.getElementById("btnLogin").onclick = function () {
    let option_fetch = {
        method: "PUT",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({ userid: "Liulh", passwd: "111111", dispname: "刘小马", type: "admin" })
    }
    // 调用服务端API,更新小鼠体重数据
    fetch('TestServer/login/', option_fetch)
        .then(response => response.json())
        .then(data => console.log(data));
}