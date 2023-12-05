// 初始化视图组件
let config_chartCalibration0 = {
    type: 'line',
    data: {
        datasets: [
            { label: '炉壁温度', data: [], borderWidth: 1, pointRadius: 0 },
            { label: '参照物温度', data: [], borderWidth: 1, pointRadius: 0 }
        ],
        labels: []
    },
    options: options_chartTemperature
};

let _caliCharts = [
    { target: null, config: config_chartCalibration0 }
];
_caliCharts.forEach((item, index) => {
    item.target = new Chart(document.getElementById("chartCalibration" + index), item.config);
});

function startCalibrating(apparatusId) {

}

function stopCalibrating(apparatusId) {

}

function startFurnaceMove(apparatusId) {
    fetch(`testserver/startcalirecording/${apparatusId}`)
        .then(response => response.json())
        .then(data => {
            if (data.result === true) {
                appendServerMsg(TestServerMode.Calibration, apparatusId, data);
            }
        });
}