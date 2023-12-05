// 试验记录模块功能

// 初始化
for (let i = 0; i < TESTSERVER_COUNT; i++) {
    $(`#chkMovement${i}`).checkgroup({
        name: 'movement',
        dir: 'h',
        data: [
            { value: 1, label: '欲跑不能' },
            { value: 2, label: '呼吸变化' },
            { value: 3, label: '惊跳' },
            { value: 4, label: '挣扎' },
            { value: 5, label: '不能翻身' },
            { value: 6, label: '昏迷' },
            { value: 7, label: '痉挛' }
        ]
    });
    $(`#chkEyeSituation${i}`).checkgroup({
        name: 'eyesituation',
        dir: 'h',
        data: [
            { value: 8, label: '视力丧失' },
            { value: 9, label: '流泪' },
            { value: 10, label: '肿胀' },
            { value: 11, label: '闭目' }
        ]
    });
}

// 模块功能代码
// 打开试验记录对话框
function setTestPheno(serverid) {
    $(`#dlgSetPheno${serverid}`).dialog('open');
}
// 设置现象编码
function submitTestPheno(serverid) {
    let code = "000000000000";
    // 动物存活情况
    code = setCharAt(code, 0, $(`#chkAlive${serverid}`).checkbox('options').checked ? '1' : '0');
    // 动物运动情况
    $(`#chkMovement${serverid}`).checkgroup('getValue').forEach(item => {
        code = setCharAt(code, item, '1');
    });
    // 动物眼区情况
    $(`#chkEyeSituation${serverid}`).checkgroup('getValue').forEach(item => {
        code = setCharAt(code, item, '1');
    });
    proxyTestData[serverid].phenocode = code;
    // 提交最新设置
    fetch(`testserver/setphenomenon/${serverid}/${proxyTestData[serverid].phenocode}/''`)
        .then(response => response.json())
        .then(data => appendServerMsg(TestServerMode.SpecimenTest,serverid, data));

    $(`#dlgSetPheno${serverid}`).dialog('close');
}
// 取消现象编码设置并关闭试验记录对话框
function closeTestPheno(serverid) {
    $(`#dlgSetPheno${serverid}`).dialog('close');
}