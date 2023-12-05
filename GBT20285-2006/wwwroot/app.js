const TestServerMode = {
    Calibration: 1,
    SpecimenTest: 2
}

// 初始化Easy UI对话框默认属性
$.messager.defaults = { ok: '确定', cancel: '取消', modal: true };

//#region 公共函数

/*
 * 功能:替换字符串中指定位置的字符 
 * 参数:
 *      str - 源字符串
 *      index - 要替换的字符索引
 *      chr - 替代字符
 * 返回:
 *      替换完成后的新字符串对象
*/
function setCharAt(str, index, chr) {
    if (index > str.length - 1) return str;
    return str.substring(0, index) + chr + str.substring(index + 1);
}

/* 功能: 新增一条试验控制器系统消息 
 * 参数:
 *      mode    - 控制器工作模式
 *      idx     - 试验控制器索引
 *      data    - 服务端API返回的消息对象
*/
function appendServerMsg(mode, idx, data) {
    if (mode === 1) { // 校准模式
        $(`#idServerCaliMsg${idx}`).datagrid('insertRow', {
            index: 0,
            row: {
                time: data.time,
                content: data.message
            }
        });
    } else if (mode === 2) { // 试验模式
        $(`#idServerMsg${idx}`).datagrid('insertRow', {
            index: 0,
            row: {
                time: data.time,
                content: data.message
            }
        });
    }
}

/* 功能: 新增一条试验控制器系统消息 
 * 参数:
 *      mode    - 控制器工作模式
 *      idx     - 试验控制器索引
 *      data    - signalR返回的消息对象
*/
function appendServerMsgFromSigR(mode, idx, data) {
    if (mode === 1) { // 校准模式
        $(`#idServerCaliMsg${idx}`).datagrid('insertRow', {
            index: 0,
            row: {
                time: data.Time,
                content: data.Message
            }
        });
    } else if (mode === 2) {  // 试验模式
        $(`#idServerMsg${idx}`).datagrid('insertRow', {
            index: 0,
            row: {
                time: data.Time,
                content: data.Message
            }
        });
    }

}

/* 功能: 新增一条传感器实时数据并同步更新温度曲线数据
 * 参数:
 *       mode - 控制器工作模式
 *       idx  - 试验控制器索引
 *       data - signalR实时数据对象
 */
function appendSensorData(mode, idx, data) {
    if (mode === 1) {
        // 新增传感器列表数据
        $(`#idSensorCaliData${idx}`).datagrid('insertRow', {
            index: 0,
            row: {
                timer: data.Counter,
                furnacetemp: data.SensorData.FurnaceTemp,
                refobjtemp: data.SensorData.RefObjTemp,
                cgasflow: data.SensorData.CGasFlow
            }
        });
        // 如果计时超过60秒,则移除列表数据末尾行
        if (data.Counter > 60) {
            var rows = $(`#idSensorCaliData${idx}`).datagrid('getRows');
            $(`#idSensorCaliData${idx}`).datagrid('deleteRow', rows.length - 1);
        }
        // 新增曲线数据
        _caliCharts[idx].config.data.labels.push(data.Counter);
        _caliCharts[idx].config.data.datasets[0].data.push(data.SensorData.FurnaceTemp);
        _caliCharts[idx].config.data.datasets[1].data.push(data.SensorData.RefObjTemp);
        _caliCharts[idx].target.update();
        // 如果计时超过10分钟,则移除图表头部数据点
        if (data.Counter > 600) {
            _caliCharts[idx].config.data.labels.shift();
            _caliCharts[idx].config.data.datasets[0].data.shift();
            _caliCharts[idx].config.data.datasets[1].data.shift();
        }
    } else if (mode === 2) {
        // 新增传感器列表数据
        $(`#idSensorData${idx}`).datagrid('insertRow', {
            index: 0,
            row: {
                timer: data.Counter,
                furnacetemp: data.SensorData.FurnaceTemp,
                cgasflow: data.SensorData.CGasFlow,
                dgasflow: data.SensorData.DGasFlow
            }
        });
        // 如果计时超过60秒,则移除列表数据末尾行
        if (data.Counter > 60) {
            var rows = $(`#idSensorData${idx}`).datagrid('getRows');
            $(`#idSensorData${idx}`).datagrid('deleteRow', rows.length - 1);
        }
        // 新增曲线数据
        _charts[idx].config.data.labels.push(data.Counter);
        _charts[idx].config.data.datasets[0].data.push(data.SensorData.FurnaceTemp);
        _charts[idx].target.update();
        // 如果计时超过10分钟,则移除图表头部数据点
        if (data.Counter > 600) {
            _charts[idx].config.data.labels.shift();
            _charts[idx].config.data.datasets[0].data.shift();
        }
    }
}

/* 功能: 重置试验控制器面板显示
 * 参数:
 *       idx  - 试验控制器索引
 */
function resetTestControlPanel(idx) {
    // 清空传感器列表显示
    $(`#idSensorData${idx}`).datagrid('loadData', []);
    // 清空温度曲线
    _charts[idx].config.data.labels = [];
    _charts[idx].config.data.datasets[0].data = [];
    _charts[idx].target.update();
}

// 根据产烟浓度返回对应的浓度等级
function getSafetyLevel(concentration) {
    var value = parseInt((parseFloat(concentration) + Number.EPSILON) * 100);
    if (value >= 10000) {
        return 'AQ1';
    } else if (value >= 5000) {
        return 'AQ2';
    } else if (value >= 2500) {
        return 'ZA1';
    } else if (value >= 1240) {
        return 'ZA2';
    } else if (value >= 615) {
        return 'ZA3';
    } else {
        return 'WX';
    }
}

// 计算载气流量与稀释气流量 返回[F1,F2]
function caculateGasFlow(panelid) {
    var V = 10.0;
    var M = proxyTestData[panelid].speciweight;
    var L = proxyTestData[panelid].specilength;
    var C = proxyTestData[panelid].gasconcen;
    F = (V * M) / (L * C);
    if (F > 5) {
        return [5, Math.round(((F - 5) + Number.EPSILON) * 100) / 100];
    } else {
        return [Math.round((F + Number.EPSILON) * 100) / 100, 0];
    }
}

/* 试验控制面板命令按钮响应函数 */
function setFurnaceTemperature(apparatusid) {

}
// 开始记录
function startRecording(serverid) {
    fetch(`testserver/startrecording/${serverid}`)
        .then(response => response.json())
        .then(data => {
            if (data.result === true) {
                appendServerMsg(TestServerMode.SpecimenTest, serverid, data);
            } else {
                $.messager.confirm('操作确认提示', data.message, (confirm) => {
                    if (confirm) {
                        fetch(`testserver/startrecordinganyway/${serverid}`)
                            .then(response => response.json())
                            .then(data => appendServerMsg(TestServerMode.SpecimenTest, serverid, data))
                    }
                });
            }
        });
}
// 停止记录
function stopRecording(serverid) {
    $.messager.defaults = { ok: '是', cancel: '否', modal: true };
    $.messager.confirm('操作确认提示', '正在停止试验,是否保存本次试验数据?', (confirm) => {
        fetch(`testserver/stoprecording/${serverid}/${confirm}`)
            .then(response => response.json())
            .then(data => {
                if (data.result === true) {
                    resetTestControlPanel(serverid);
                    appendServerMsg(serverid, data);
                }
            })
    });
    $.messager.defaults = { ok: '确定', cancel: '取消', modal: true };
}
// 参数设置
function setParameters(apparatusid) {

}
// 开始升温
function startHeating(apparatusid) {
    fetch(`testserver/startheating/${apparatusid}`)
        .then(response => response.json())
        .then(data => {
            appendServerMsg(TestServerMode.SpecimenTest, apparatusid, data);
        });
}
// 停止升温
function stopHeating(apparatusid) {

}

//#endregion

//#region 客户端-视图数据模型

const TESTSERVER_COUNT = 1;

// 产品数据模型
let productDataModel = [];
// 试验数据模型
let testDataModel = [];
// 试验装置数据模型
let apparatusDataModel = [];
// 登录用户数据模型
let userDataModel = {
    userid: null,
    passwd: null,
    dispname: null,
    type: null
};
// 试验服务器广播数据模型
let broadcastDataModel = [];

//#endregion

//#region 界面初始化

// 温度图表配置参数
let options_chartTemperature = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
        x: {
            beginAtZero: true,
            ticks: {
                maxTicksLimit: 10
            }
        },
        y: {
            beginAtZero: true,
            suggestedMax: 900,
            title: {
                display: true,
                text: '温度(℃)'
            }
        }
    },
    plugins: {
        legend: {
            labels: {
                boxWidth: 25,
                boxHeight: 2
            }
        }
    }
};
let config_chartTemperature0 = {
    type: 'line',
    data: {
        datasets: [
            { label: '炉壁温度', data: [], borderWidth: 1, pointRadius: 0 }
        ],
        labels: []
    },
    options: options_chartTemperature
};

//#endregion

//#region 初始化

// 温度图表
let _charts = [
    { target: null, config: config_chartTemperature0 }
];
_charts.forEach((item, index) => {
    item.target = new Chart(document.getElementById("chartTemperature" + index), item.config);
});

// 数据绑定
let handlerProductData = [];
let handlerTestData = [];
let handlerApparatusData = [];
let handlerBroadcastData = [];
let proxyProductData = [];
let proxyTestData = [];
let proxyApparatusData = [];
let proxyBroadcastData = [];
for (let i = 0; i < TESTSERVER_COUNT; i++) {
    apparatusDataModel.push({
        apparatusid: "",
        apparatusname: "",
        checkdatef: "",
        checkdatet: ""
    });
    productDataModel.push({
        productid: "",
        productname: "",
        specification: "",
        shape: ""
    });
    testDataModel.push({
        testid: "",
        specimenid: "",
        reportid: "",
        ambtemp: 0,
        ambhumi: 0,
        speciweight: 0,
        speciweightpost: 0,
        smokerate: 0,
        smokerateconfirm: "",
        specilength: 0,
        apparatusid: "",
        apparatusname: "",
        checkdatef: "",
        checkdatet: "",
        according: "",
        safetylevel: "",
        gasconcen: 0,
        heattemp: 0,
        cgasflow: 0,
        dgasflow: 0,
        furnacespeed: 0,
        mousecnt: 10,
        recoveryday: 0,
        phenocode: "000000000000",
        testdate: "",
        operator: "",
        comment: "",
        nounresult: null,
        irriresult: null,
        testresult: null,
        flag: "00000000"
    });
    broadcastDataModel.push({
        timer: 0,
        furnacetemp: 0,
        deltatemp: 0,
        refobjtemp: 0,
        cgasflow: 0,
        dgasflow: 0
    });
    // [Product]: 数据源 -> 绑定对象
    handlerProductData.push({
        set: function (source, property, value) {
            // 属性存在于数据源中 且 新值不等于现值时执行更新
            if (!(property in source)) {
                throw new Error("property not in the binding source object!");
            }
            if (value !== source[property]) {
                // 更新绑定对象的显示值
                const targets = document.querySelectorAll(`[data-serverid="${i}"][data-bind-product="${property}"]`);
                targets.forEach(item => {
                    item.value = value;
                });
                // 执行默认赋值
                source[property] = value;
                return true;
            }
        }
    });
    // [Test]: 数据源 -> 绑定对象
    handlerTestData.push({
        set: function (source, property, value) {
            // 属性存在于数据源中 且 新值不等于现值时执行更新
            if (!(property in source)) {
                throw new Error("property not in the binding source object!");
            }
            if (value !== source[property]) {
                // 更新绑定对象的显示值
                const targets = document.querySelectorAll(`[data-serverid="${i}"][data-bind-test="${property}"]`);
                targets.forEach(item => {
                    item.value = value;
                });
                // 执行默认赋值
                source[property] = value;
                return true;
            }
        }
    });
    // [Apparatus]: 数据源 -> 绑定对象
    handlerApparatusData.push({
        set: function (source, property, value) {
            // 属性存在于数据源中 且 新值不等于现值时执行更新
            if (!(property in source)) {
                throw new Error("property not in the binding source object!");
            }
            if (value !== source[property]) {
                // 更新绑定对象的显示值
                const targets = document.querySelectorAll(`[data-serverid="${i}"][data-bind-apparatus="${property}"]`);
                targets.forEach(item => {
                    item.value = value;
                });
                // 执行默认赋值
                source[property] = value;
                return true;
            }
        }
    });
    // [BroadCast]: 数据源 -> 绑定对象
    handlerBroadcastData.push({
        set: function (source, property, value) {
            // 属性存在于数据源中 且 新值不等于现值时执行更新
            if (!(property in source)) {
                throw new Error("property not in the binding source object!");
            }
            if (value !== source[property]) {
                // 更新绑定对象的显示值
                const targets = document.querySelectorAll(`[data-serverid="${i}"][data-bind-broadcast="${property}"]`);
                targets.forEach(item => {
                    item.value = value;
                });
                // 执行默认赋值
                source[property] = value;
                return true;
            }
        }
    });
    proxyProductData.push(new Proxy(productDataModel[i], handlerProductData[i]));
    proxyTestData.push(new Proxy(testDataModel[i], handlerTestData[i]));
    proxyApparatusData.push(new Proxy(apparatusDataModel[i], handlerApparatusData[i]));
    proxyBroadcastData.push(new Proxy(broadcastDataModel[i], handlerBroadcastData[i]));
};
// [Product]: 绑定对象 -> 数据源
const productUITargets = document.querySelectorAll(`[data-bind-product]`);
productUITargets.forEach(item => {
    item.addEventListener('change', () => {
        if (item.dataset.bindProduct === 'productid') {
            // 根据样品编号获取样品信息
            fetch(`testserver/getproductinfo/${item.value}`)
                .then(response => response.json())
                .then(data => {
                    // 返回有效记录,设置对应值的显示
                    if (data.result === true) {
                        var record = data.parameters.result;
                        proxyProductData[item.dataset.serverid].productname = record.productname;
                        proxyProductData[item.dataset.serverid].specification = record.specification;
                        proxyProductData[item.dataset.serverid].shape = record.shape;
                    } else {
                        proxyProductData[item.dataset.serverid].productname = '';
                        proxyProductData[item.dataset.serverid].specification = '';
                        proxyProductData[item.dataset.serverid].shape = '';
                    }
                });
            // 同步报表编号与样品编号
            proxyTestData[item.dataset.serverid].reportid = item.value;
        }
        productDataModel[item.dataset.serverid][item.dataset.bindProduct] = item.value;
    });
});
// [Test]: 绑定对象 -> 数据源
const testUITargets = document.querySelectorAll(`[data-bind-test]`);
testUITargets.forEach(item => {
    item.addEventListener('change', () => {
        testDataModel[item.dataset.serverid][item.dataset.bindTest] = item.value;
        // 更新试样质量与产烟浓度时,自动计算并设置浓度等级、载气流量与稀释气流量值
        if (item.dataset.bindTest === 'gasconcen') {
            proxyTestData[item.dataset.serverid].safetylevel = getSafetyLevel(item.value);
            const gasflow = caculateGasFlow(item.dataset.serverid);
            proxyTestData[item.dataset.serverid].cgasflow = gasflow[0];
            proxyTestData[item.dataset.serverid].dgasflow = gasflow[1];
        }
        if (item.dataset.bindTest === 'speciweight') {
            const gasflow = caculateGasFlow(item.dataset.serverid);
            proxyTestData[item.dataset.serverid].cgasflow = gasflow[0];
            proxyTestData[item.dataset.serverid].dgasflow = gasflow[1];
        }
    });
});
// [Apparatus]: 绑定对象 -> 数据源
const apparatusUITargets = document.querySelectorAll(`[data-bind-apparatus]`);
apparatusUITargets.forEach(item => {
    item.addEventListener('change', () => {
        testDataModel[item.dataset.serverid][item.dataset.bindApparatus] = item.value;
    });
});

// SignalR
var connection = new signalR.HubConnectionBuilder()
    .withUrl("/broadcast")
    .configureLogging(signalR.LogLevel.Information)
    .build();
// 注册服务端实时消息处理函数
connection.on("ServerBroadCast", function (jsonObject) {
    // 解析服务器消息
    const data = JSON.parse(jsonObject);
    /* 更新对应编号的控制器面板数据模型及显示 */
    proxyBroadcastData[data.ServerId].timer = data.Counter;
    proxyBroadcastData[data.ServerId].furnacetemp = data.SensorData.FurnaceTemp;
    proxyBroadcastData[data.ServerId].cgasflow = data.SensorData.CGasFlow;
    proxyBroadcastData[data.ServerId].dgasflow = data.SensorData.DGasFlow;
    proxyBroadcastData[data.ServerId].deltatemp = data.CaculationData.DeltaTemp;
    // 若试验控制器状态为[Recording]新增传感器历史数据及曲线数据
    if (data.Status === 3) {
        appendSensorData(data.WorkMode, data.ServerId, data);
    }
    // 如果有新的系统消息,则添加
    data.ServerMessages.forEach((item) => {
        appendServerMsgFromSigR(data.WorkMode, data.ServerId, item);
    });
    // 根据试验控制器状态更新面板工具栏命令按钮显示
    switch (data.Status) {
        case 0: // Idle                
            break;
        case 1: // Preparing
            break;
        case 2: // Ready
            break;
        case 3: // Recording
            break;
        case 4: // Complete   
            break;
    }
});
// 注册onClose事件处理函数,连接意外中断时自动重新建立连接
connection.onclose(async () => {
    await startTestSignalR();
});
// 试验服务器SignalR连接函数
async function startTestSignalR() {
    try {
        await connection.start();
    } catch (err) {
        // 提示错误信息
        $.messager.alert('错误提示', err.message, 'error');
        // 5秒后重新尝试连接
        setTimeout(startTestSignalR, 5000);
    }
};

//#endregion

startTestSignalR();

// 获取试验服务器数据
fetch("testserver/getserverinfo")
    .then(response => response.json())
    .then(data => {
        data.parameters.result.forEach((item, index) => {
            proxyApparatusData[index].apparatusid = item.apparatusid;
            proxyApparatusData[index].apparatusname = item.apparatusname;
            proxyApparatusData[index].checkdatef = item.checkdatef.substring(0, 4) + "年" + item.checkdatef.substring(5, 7) + "月" + item.checkdatef.substring(8, 10) + "日";
            proxyApparatusData[index].checkdatet = item.checkdatet.substring(0, 4) + "年" + item.checkdatet.substring(5, 7) + "月" + item.checkdatet.substring(8, 10) + "日";
            // 同步设置试验数据模型的设备相关字段
            proxyTestData[index].apparatusid = item.apparatusid;
            proxyTestData[index].apparatusname = item.apparatusname;
            proxyTestData[index].checkdatef = item.checkdatef.substring(0, 4) + "年" + item.checkdatef.substring(5, 7) + "月" + item.checkdatef.substring(8, 10) + "日";
            proxyTestData[index].checkdatet = item.checkdatet.substring(0, 4) + "年" + item.checkdatet.substring(5, 7) + "月" + item.checkdatet.substring(8, 10) + "日";
            // 添加服务器消息
            // ...
        });
    })