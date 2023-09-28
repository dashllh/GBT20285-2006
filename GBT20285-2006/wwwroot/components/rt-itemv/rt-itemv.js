/* 模块功能:
 *          实时数据项显示组件(标签与值纵向排列)
 * 包含属性：
 *          1.label:string - 数据项目名称
 *          2.value:float  - 数据实时值
 * 控制参数:
 *          1.round:string - 保留小数点后数据位数
 *          
 * 修改日期: 2023-7-5
 * 
 * 修改履历:
 *          1. 将属性接口Value修改为value以匹配数据模型绑定方法中对.value的统一访问
 *          2. 增加上下限阈值属性,超过设定阈值将显示为红色
 *  
 */

class RealTimeItemV extends HTMLElement {
    //属性定义
    #label = 'name';  // 数据名称
    #value = 8888;   // 当前数据值
    #round = 0;       // 保留小数位数
    #unit = "";       // 物理量单位 
    #min = undefined; // 最小值(低于该值则显示字体为红色)
    #max = undefined; // 最大值(高于该值则显示字体为红色)
    #displaycolor = 'normal';   // 数值显示颜色

    constructor(label, round, unit) {
        super();

        this.#label = label;
        this.#round = round;
        this.#unit = unit;
    }

    // 内部私有方法
    updateDisplayColor() {        
        if (this.#min !== undefined && this.#max !== undefined) {
            if (parseInt(this.#value * (10 ** parseInt(this.#round))) > parseInt(this.#max * (10 ** parseInt(this.#round)))
            || parseInt(this.#value * (10 ** parseInt(this.#round))) < parseInt(this.#min * (10 ** parseInt(this.#round)))) {
                this.#displaycolor = 'alert';
            } else {
                this.#displaycolor = 'normal';
            }
        } else if (this.#min === undefined && this.#max !== undefined) {
            if (parseInt(this.#value * (10 ** parseInt(this.#round))) > parseInt(this.#max * (10 ** parseInt(this.#round)))) {
                this.#displaycolor = 'alert';
            } else {
                this.#displaycolor = 'normal';
            }
        } else if (this.#min !== undefined && this.#max === undefined) {
            if (parseInt(this.#value * (10 ** parseInt(this.#round))) < parseInt(this.#min * (10 ** parseInt(this.#round)))) {
                this.#displaycolor = 'alert';
            } else {
                this.#displaycolor = 'normal';
            }
        } else {
            this.#displaycolor = 'normal';
        }
    }

    //getter setter
    set Label(data) {
        this.#label = data;
        this.render();
    }

    get Label() {
        return this.#label;
    }

    set value(data) {
        switch (this.#round) {
            case '0':
                this.#value = Math.round(data);
                break;
            case '1':
                this.#value = Math.round((data + Number.EPSILON) * 10) / 10;
                break;
            case '2':
                this.#value = Math.round((data + Number.EPSILON) * 100) / 100;
                break;
            case '3':
                this.#value = Math.round((data + Number.EPSILON) * 1000) / 1000;
                break;
        }
        this.updateDisplayColor();
        this.render();
    }

    get value() {
        return this.#value;
    }

    set Round(data) {
        this.#round = data;
    }

    get Round() {
        return this.#round;
    }

    set Unit(data) {
        this.#unit = data;
        this.render();
    }

    get Unit() {
        return this.#unit;
    }

    set max(data) {
        this.#max = data;
    }

    get max() {
        return this.#max;
    }

    set min(data) {
        this.#min = data;
    }

    get min() {
        return this.#min;
    }

    static get observedAttributes() {
        return ['label', 'round', 'value', 'unit', 'min', 'max']
    }

    //回调函数
    attributeChangedCallback(item, oldvalue, newvalue) {
        switch (item) {
            case 'label':
                this.Label = newvalue;
                break;
            case 'round':
                this.Round = newvalue;
                break;
            case 'value':
                this.value = newvalue;
                break;
            case 'unit':
                this.Unit = newvalue;
                break;
            case 'min':
                this.min = newvalue;
                break;
            case 'max':
                this.max = newvalue;
                break;
        }
    }

    connectedCallback() {

    }

    disconnectedCallback() {

    }

    // 组件样式
    get style() {

    }

    //组件HTML模板
    get template() {
        return `            
            <div class="rt-itemv">
                <div class="header">
                    ${this.#label} ${this.#unit}
                </div>
                <div class="value ${this.#displaycolor}">
                    ${this.#value}
                </div>
            </div>
        `;
    }

    //渲染函数
    render() {
        this.innerHTML = this.template;
    }
}

customElements.define('rt-itemv', RealTimeItemV);

export { RealTimeItemV }