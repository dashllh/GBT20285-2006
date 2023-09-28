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
 *      idx     - 试验控制器索引
 *      data    - 服务端API返回的消息对象
*/
function appendServerMsg(idx, data) {
    $(`#idServerMsg${idx}`).datagrid('insertRow', {
        index: 0,
        row: {
            time: data.time,
            content: data.message
        }
    });
}

/* 功能: 新增一条试验控制器系统消息 
 * 参数:
 *      idx     - 试验控制器索引
 *      data    - signalR返回的消息对象
*/
function appendServerMsgFromSigR(idx, data) {
    $(`#idServerMsg${idx}`).datagrid('insertRow', {
        index: 0,
        row: {
            time: data.Time,
            content: data.Message
        }
    });
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
let config_chartTemperature = {
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
const chartInstance = new Chart(document.getElementById("chartTemperature"), config_chartTemperature);
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
        specilength: 0,
        apparatusid: "",
        apparatusname: "",
        checkdatef: "",
        checkdatet: "",
        according: "GB20285",
        safetylevel: "",
        gasconcen: 0,
        heattemp: 0,
        cgasflow: 0,
        dgasflow: 0,
        furnacespeed: 0,
        mousecnt: 0,
        recoveryday: 0,
        phenocode: "000000000000",
        testdate: "",
        operator: "",
        comment: "",
        nounresult: false,
        irriresult: false,
        testresult: false,
        flag: "0000"
    });
    broadcastDataModel.push({
        timer: 0,
        furnacetemp: 0,
        deltatemp: 0,
        cgasflow: 0,
        dgasflow: 0,
        calitemp: 0
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
        productDataModel[item.dataset.serverid][item.dataset.bindProduct] = item.value;
    });
});
// [Test]: 绑定对象 -> 数据源
const testUITargets = document.querySelectorAll(`[data-bind-test]`);
testUITargets.forEach(item => {
    item.addEventListener('change', () => {
        testDataModel[item.dataset.serverid][item.dataset.bindTest] = item.value;
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
    // if (data.MasterStatus === 3) {
    //     appendSensorData(data.MasterId, data);
    // }
    // 如果有新的系统消息,则添加
    // data.MasterMessages.forEach((item) => {
    //     appendSysMsgFromSigR(data.MasterId, item);            
    // });
    // 根据试验控制器状态更新面板工具栏命令按钮显示
    switch (data.MasterStatus) {
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

// startTestSignalR();

// 获取试验服务器数据
fetch("testserver/getserverinfo")
    .then(response => response.json())
    .then(data => {
        data.forEach((item,index) => {
            proxyApparatusData[index].apparatusid = item.apparatusid;
            proxyApparatusData[index].apparatusname = item.apparatusname;
            proxyApparatusData[index].checkdatef = item.checkdatef;
            proxyApparatusData[index].checkdatet = item.checkdatet;
            // 同步设置试验数据模型的设备相关字段
            proxyTestData[index].apparatusid = item.apparatusid;
            proxyTestData[index].apparatusname = item.apparatusname;
            proxyTestData[index].checkdatef = item.checkdatef;
            proxyTestData[index].checkdatet = item.checkdatet;
            // 添加服务器消息
            // ...
        });
    })