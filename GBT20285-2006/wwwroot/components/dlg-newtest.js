function createNewTest(id) {
    $(`#dlgNewTest${id}`).dialog('open');
}

function closeNewTest(id) {
    $(`#dlgNewTest${id}`).dialog('close');
}

function submitNewTest(id) {
    let testData = {
        product: productDataModel[id],
        test: testDataModel[id]
    }
    let option = {
        method: "POST",
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(testData)
    }
    fetch(`testserver/createnewtest/${id}`, option)
        .then(response => response.json())
        .then(data => {
            // 显示服务器返回消息
            // ...
        })
}