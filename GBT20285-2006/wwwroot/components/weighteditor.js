
// let curField = '';

/* 模块级数据模型 */
let dm_MouseWeight = {
    productId: '',  // 样品编号
    testId: '',     // 试验编号
    weights: []     // 小鼠体重列表
}

/* 模块级公共函数 */

/*
 * 功能: 验证小鼠体重数据
 * 参数: 
 *       value - 小鼠体重数据
 * 返回:
 *       float   - 转换后的有效体重数值
 *       0       - 死亡
 *       null    - 无效输入,不纳入试验结论判定
*/
function validateWeight(value) {
    // null/undefined/空字符串 视为无效输入
    if (value === null || value === undefined || value === '' || value.length === 0) {
        return null;
    } else {
        var weight = parseFloat(value);
        if (!isNaN(weight)) { // 有效数字字符                
            if (parseInt(weight * 10) > 0) { // 体重 > 0 视为有效体重数据
                return weight;
            } else { // 体重 <= 0 视为小鼠死亡
                return 0;
            }
        } else { // 无效数字字符 视为小鼠死亡
            return 0;
        }
    }
}

// 配置小鼠体重列表控件的列属性
$('#dg-lstweight').datagrid({
    columns: [[
        { field: 'mouseId', title: '动物编号', width: 80, align: 'center' },
        {
            field: 'preWeight2', title: '试验前二日体重(g)', width: 140, align: 'center',
            formatter: function (value) {
                if (validateWeight(value) === 0) {
                    return '死亡';
                } else {
                    return value;
                }
            },
            editor: { type: 'numberbox', options: { precision: 1 } }
        },
        {
            field: 'preWeight1', title: '试验前一日体重(g)', width: 140, align: 'center',
            formatter: function (value) {
                if (validateWeight(value) === 0) {
                    return '死亡';
                } else {
                    return value;
                }
            },
            editor: { type: 'numberbox', options: { precision: 1 } }
        },
        {
            field: 'expWeight', title: '试验当日体重(g)', width: 140, align: 'center',
            formatter: function (value) {
                if (validateWeight(value) === 0) {
                    return '死亡';
                } else {
                    return value;
                }
            },
            editor: { type: 'numberbox', options: { precision: 1 } }
        },
        {
            field: 'postWeight1', title: '试验后一日体重(g)', width: 140, align: 'center',
            formatter: function (value) {
                if (validateWeight(value) === 0) {
                    return '死亡';
                } else {
                    return value;
                }
            },
            editor: { type: 'numberbox', options: { precision: 1 } }
        },
        {
            field: 'postWeight2', title: '试验后二日体重(g)', width: 140, align: 'center',
            formatter: function (value) {
                if (validateWeight(value) === 0) {
                    return '死亡';
                } else {
                    return value;
                }
            },
            editor: { type: 'numberbox', options: { precision: 1 } }
        },
        {
            field: 'postWeight3', title: '试验后三日体重(g)', width: 140, align: 'center',
            formatter: function (value) {
                if (validateWeight(value) === 0) {
                    return '死亡';
                } else {
                    return value;
                }
            },
            editor: { type: 'numberbox', options: { precision: 1 } }
        }
    ]]
}).datagrid('enableCellEditing');

/*
 * 功能: 加载指定样品的指定试验的小鼠体重数据
 * 参数:
 *       productid:string - 样品编号
 *       testid:string    - 试验编号
*/
function loadMouseWeight(productid, testid) {
    fetch(`../posttest/getmouseweight/${productid}/${testid}`)
        .then(response => response.json())
        .then(data => {
            // 设置数据模型中的对应数据项
            dm_MouseWeight.weights = data.parameters.result;
            // datagrid控件加载最新体重数据
            $('#dg-lstweight').datagrid('loadData', data.parameters.result);
        });
}

// 体重数据单元格事件响应函数:开始编辑
// function onCellEdit(index, field) {
//     // 记录当前正在编辑的单元格列名
//     curField = field;
// }

// 体重数据单元格事件响应函数:结束编辑
// function onEndEdit(index) {
//     // 获取当前编辑单元格的最新录入值
//     if (curField !== '') {
//         const editor = $('#dg-lstweight').datagrid('getEditor', { index: index, field: curField });
//         if (editor) {
//             var strWeight = $(editor.target).numberbox('getText');
//         }
//     }
// }

// 保存数据按钮事件响应: 更新小鼠体重数据
document.getElementById('btnSave').addEventListener('click', (event) => {
    // 将datagrid数据加载至内存数据缓存
    var data = $('#dg-lstweight').datagrid('getData');
    dm_MouseWeight.weights = data.rows;
    let option_fetch = {
        method: "PUT",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(dm_MouseWeight.weights)
    }
    // 调用服务端API,更新小鼠体重数据
    fetch(`../posttest/updatemouseweight/${dm_MouseWeight.productId}/${dm_MouseWeight.testId}`, option_fetch)
        .then(response => response.json())
        .then(data => {
            if (data.result === true) {
                $.messager.alert('信息提示', '体重数据更新成功。', 'info');
                // 提交体重数据成功,继续执行结论判定
                fetch(`../posttest/judgefinalresult/${dm_MouseWeight.productId}/${dm_MouseWeight.testId}`)
                    .then(response => response.json())
                    .then(data => {
                        if(data.result === false) {
                            $.messager.alert('错误提示', data.message, 'error');
                        }
                    });
            } else {
                $.messager.alert('错误提示', data.message, 'error');
            }
        });
});

document.getElementById('btnSearch').addEventListener('click', () => {
    dm_MouseWeight.productId = document.getElementById('txtSpecimenId').value.trim();
    dm_MouseWeight.testId = document.getElementById('txtTestId').value.trim();
    loadMouseWeight(dm_MouseWeight.productId, dm_MouseWeight.testId);
});