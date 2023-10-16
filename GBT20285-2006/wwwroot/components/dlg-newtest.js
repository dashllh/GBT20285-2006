function createNewTest(serverid) {
    // 试验日期
    const today = new Date();
    proxyTestData[serverid].testdate = today.getFullYear() + "年" + (today.getMonth() + 1) + "月" + today.getDate() + "日";
    // 检验依据
    proxyTestData[serverid].according = "GB/T 20285";
    // 检验人员
    proxyTestData[serverid].operator = userDataModel.dispname;
    // 炉位移速率
    proxyTestData[serverid].furnacespeed = 10.0;
    // 重置试验编号
    proxyTestData[serverid].testid = "";

    $(`#dlgNewTest${serverid}`).dialog('open');
}

function closeNewTest(serverid) {
    $(`#dlgNewTest${serverid}`).dialog('close');
}

function submitNewTest(serverid) {
    // specimenid在Test对象中是主键,但客户端未有专门文本框与之绑定(已绑定productid),故提交数据前设置
    testDataModel[serverid].specimenid = productDataModel[serverid].productid;
    let requestBody = {
        product: productDataModel[serverid],
        test: testDataModel[serverid]
    }
    let option = {
        method: "POST",
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(requestBody)
    }
    fetch(`testserver/createnewtest/${serverid}`, option)
        .then(response => response.json())
        .then(data => {
            // 显示服务器返回消息
            appendServerMsg(serverid, data);
            // 关闭对话框
            closeNewTest(serverid);
        })
}