// 报表视图数据模型
let reportViewDataModel = {
    specimenid: '',
    testid: ''
}

let = comboData = [
    { label: '产烟率>95%', value: '1' },
    { label: '产烟过程阴燃无火焰,残余物为灰烬', value: '2' },
    { label: '加热温度每增加100℃,产烟率≤2%', value: '3' }
];

let currentSpecimenId = '';
let currentEditField = '';
let testDataCache = [];

let handler = {
    set: function (source, property, value) {
        if (source[property] === value)
            return true;
        // 更新绑定对象显示值
        var bindTargets = document.querySelectorAll(`[data-bind-criteria=${property}]`);
        bindTargets.forEach(item => {
            item.value = value;
        });
        // 默认赋值操作
        source[property] == value;
        return true;
    }
}

let proxy_ReportViewDataModel = new Proxy(reportViewDataModel, handler);

// 数据绑定: UI -> 数据源
const bindTargets = document.querySelectorAll(`[data-bind-criteria]`);
bindTargets.forEach(item => {
    item.addEventListener('change', () => {
        reportViewDataModel[item.dataset.bindCriteria] = item.value;
    });
});

document.getElementById('btnSearch').addEventListener('click', () => {
    fetch(`posttest/searchtestinfo/${proxy_ReportViewDataModel.specimenid}/${proxy_ReportViewDataModel.testid}`)
        .then(response => response.json())
        .then(data => {
            if (data.result) {
                currentSpecimenId = proxy_ReportViewDataModel.specimenid;
                testDataCache = data.parameters.result.tests;
                // 设置产品信息
                document.getElementById('txtProductName').value = data.parameters.result.product.productname;
                // 加载返回数据
                $('#dgTestDetails').datagrid('loadData', data.parameters.result.tests);
            } else {
                $.messager.alert('提示信息', data.message, 'warning');
            }
        });
});

// 设置试验明细报表显示属性
$('#dgTestDetails').datagrid({
    columns: [[
        { field: 'testid', title: '试验编号', width: 80, align: 'center' },
        { field: 'speciweight', title: '试样质量(g)', width: 85, align: 'center' },
        {
            field: 'speciweightpost', title: '试样残余质量(g)', width: 120, align: 'center',
            editor: { type: 'numberbox', options: { precision: 1 } }
        },
        {
            field: 'smokerate', title: '产烟率(%)', width: 80, align: 'center',
            formatter: function (value) {
                return Math.round((value + Number.EPSILON) * 10) / 10;
            }
        },
        {
            field: 'smokerateconfirm', title: '充分产烟率的确定', width: 240, align: 'center',
            formatter: function (value) {
                switch (value) {
                    case '1':
                        return '产烟率>95%';
                    case '2':
                        return '产烟过程阴燃无火焰,残余物为灰烬';
                    case '3':
                        return '加热温度每增加100℃,产烟率≤2%';
                }
            },
            editor: { type: 'combobox' }
        },
        {
            field: 'nounresult', title: '麻醉性结论', width: 80, align: 'center',
            formatter: function (value) {
                if (value === true) {
                    return '合格';
                } else if (value === false) {
                    return '不合格';
                }
                // null的情况
                return '待定';
            },
        },
        {
            field: 'irriresult', title: '刺激性结论', width: 80, align: 'center',
            formatter: function (value) {
                if (value === true) {
                    return '合格';
                } else if (value === false) {
                    return '不合格';
                }
                // null的情况
                return '待定';
            },
        },
        {
            field: 'testresult', title: '综合结论', width: 80, align: 'center',
            formatter: function (value) {
                if (value === true) {
                    return '合格';
                } else if (value === false) {
                    return '不合格';
                }
                // null的情况
                return '待定';
            },
        },
        {
            field: 'phenocode', title: '染毒期内死亡', width: 95, align: 'center',
            formatter: function (value) {
                return value.charAt(0) === '1' ? '是' : '否';
            }
        },
        {
            field: 'mouseweight', title: '小鼠体重', width: 80, align: 'center',
            formatter: function (value, row, index) {
                return `<a href="#" onclick="viewMouseWeight(${index})">查看</a>`;
            }
        },
        {
            field: 'action', title: '操作', width: 85, align: 'center',
            formatter: function (value, row, index) {
                return `<a href="#" onclick="generateTestReport(${index})">生成报表</a>`;
            },
        }
    ]]
}).datagrid('enableCellEditing');

$('#dgViewMouseWeight').datagrid({
    columns: [[
        { field: 'mouseId', title: '动物编号', width: 80, align: 'center' },
        {
            field: 'preWeight2', title: '试验前2日体重(g)', width: 140, align: 'center',
            formatter: function (value) {
                return value === 0 ? '死亡' : value;
            }
        },
        {
            field: 'preWeight1', title: '试验前1日体重(g)', width: 140, align: 'center',
            formatter: function (value) {
                return value === 0 ? '死亡' : value;
            }
        },
        {
            field: 'expWeight', title: '试验当日体重(g)', width: 140, align: 'center',
            formatter: function (value) {
                return value === 0 ? '死亡' : value;
            }
        },
        {
            field: 'postWeight1', title: '试验后1日体重(g)', width: 140, align: 'center',
            formatter: function (value) {
                return value === 0 ? '死亡' : value;
            }
        },
        {
            field: 'postWeight2', title: '试验后2日体重(g)', width: 140, align: 'center',
            formatter: function (value) {
                return value === 0 ? '死亡' : value;
            }
        },
        {
            field: 'postWeight3', title: '试验后3日体重(g)', width: 140, align: 'center',
            formatter: function (value) {
                return value === 0 ? '死亡' : value;
            }
        }
    ]]
});

function onCellEdit(index, field) {
    currentEditField = field;
    // 动态加载确定充分产烟率的选项列表
    if (field === 'smokerateconfirm') {
        const editor = $('#dgTestDetails').datagrid('getEditor', { index: index, field: field });
        if (editor !== null) {
            $(editor.target).combobox({
                textField: 'label',
                valueField: 'value',
                panelHeight: 'auto',
                editable: false
            });
            $(editor.target).combobox('loadData', comboData);
        }
    }
}

function onEndEdit(index) {
    // 设置试样残余质量并自动计算产烟率
    if (currentEditField === 'speciweightpost') {
        const editor = $('#dgTestDetails').datagrid('getEditor', { index: index, field: currentEditField });
        if (editor) {
            testDataCache[index].speciweightpost = parseFloat($(editor.target).numberbox('getText'));
            // 计算产烟率
            var smokerate = ((testDataCache[index].speciweight - testDataCache[index].speciweightpost) / testDataCache[index].speciweight) * 100;
            smokerate = Math.round((smokerate + Number.EPSILON) * 10) / 10;
            $('#dgTestDetails').datagrid('updateRow', { index: index, row: { smokerate: smokerate } });
        }
    }
    // 设置充分产烟率的确定方法
    if (currentEditField === 'smokerateconfirm') {
        testDataCache[index].smokerateconfirm = $('#dgTestDetails').datagrid('getRows')[index].smokerateconfirm;
    }
}

function viewMouseWeight(index) {
    fetch(`posttest/getmouseweight/${currentSpecimenId}/${testDataCache[index].testid}`)
        .then(response => response.json())
        .then(data => {
            $('#dlgViewMouseWeight').dialog({ title: `动物体重明细 - [ 样品编号: ${currentSpecimenId}  试验编号: ${testDataCache[index].testid} ]` });
            $('#dlgViewMouseWeight').dialog('open');
            $('#dgViewMouseWeight').datagrid('loadData', data.parameters.result);
        });
}

function closeViewMouseWeight() {
    $('#dlgViewMouseWeight').dialog('close');
}

function generateTestReport(index) {
    $.messager.progress({ title: '请超等', msg: '正在生成试验报表...' });
    fetch(`posttest/gettestreport/${currentSpecimenId}/${testDataCache[index].testid}/${testDataCache[index].speciweightpost}/${testDataCache[index].smokerateconfirm}`)
        .then(response => response.json())
        .then(data => {
            if (data.result) {
                // 加载报表预览
                document.getElementById('rptPreviewer').src = data.parameters.previewpath;
                $.messager.progress('close');
                $.messager.alert('信息提示', `生成试验报告成功。<br>样品编号: ${currentSpecimenId}<br>试验编号: ${testDataCache[index].testid}`, 'info');
            } else {
                $.messager.alert('信息提示', data.message + '<br>' + data.parameters.error, 'warning', () => {
                    $.messager.progress('close');
                });  
            }
        });
}