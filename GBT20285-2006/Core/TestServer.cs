using GBT20285_2006.Models;
using GBT20285_2006.DBContext;
using GBT20285_2006.Utility;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Globalization;
using CsvHelper;
using OfficeOpenXml;
using DevExpress.Spreadsheet;
using DevExpress.XtraPrinting;
using System.Text;


namespace GBT20285_2006.Core
{
    /* 该类型定义试验控制相关逻辑 */
    public class TestServer : ITestServer
    {
        /* 试验服务器Id */
        private int _serverid;
        /* 控制器工作模式指示变量 */
        private MasterWorkMode _workmode;
        /* 控制器工作状态指示变量 */
        private MasterStatus _status;
        /* 数据记录计数器变量(客户端显示为[试验计时]) */
        private int _counter;
        /* 试验服务器工作线程(试验数据记录线程) */
        private Timer _timer;
        /* 传感器原始数据记录缓存 */
        private List<SensorData> _bufSensorData;
        /* SignalR数据 */
        private SignalRData _signalRData;
        /* IHubContext对象,用于发送实时数据广播 */
        private readonly IHubContext<BroadcastHub> _broadcast;
        /* 本次试验的产品数据及试样数据缓存 */
        private readonly IDbContextFactory<GB20285DBContext> _dbFactory;
        /* 试验样品数据缓存对象 */
        private Product? _product;
        /* 试验记录数据缓存对象 */
        private Test? _test;
        /* 试验条件判定变量 - 炉温10分钟静态温度偏差不超过 1℃ */
        private uint _stableCounter;

        /* 属性定义 */
        public MasterStatus Status
        {
            get
            {
                return _status;
            }
        }

        public TestServer(int serverid, IDbContextFactory<GB20285DBContext> dbFactory, IHubContext<BroadcastHub> broadcast)
        {
            _serverid = serverid;
            _workmode = MasterWorkMode.Standby;
            _status = MasterStatus.Idle;
            _counter = 0;
            _stableCounter = 120;  // 初始化为2分钟(120秒)计时
            _dbFactory = dbFactory;
            _broadcast = broadcast;
            _bufSensorData = new List<SensorData>();
            _signalRData = new SignalRData();
            _signalRData.ServerId = _serverid;
            _timer = new Timer(DoTest);
        }

        /*
         * 功能: 判断试验装置是否已满足试验开始条件: 环形炉静态温度 < 1℃
         */
        public bool CheckStartCriteria()
        {
            if (_test != null && _test.Heattemp != null)
            {
                try
                {
                    if (Math.Abs((int)(_signalRData.SensorData.FurnaceTemp * 10) - (int)(_test.Heattemp * 10)) <= 10)
                    {
                        _stableCounter--;
                    }
                    else
                    {
                        _stableCounter = 120;
                    }
                    return _stableCounter == 0;
                }
                catch (OverflowException)
                {
                    return false;
                }

            }
            return false;
        }

        public bool CheckTerminateCriteria()
        {
            throw new NotImplementedException();
        }

        public Product? GetProductData()
        {
            return _product;
        }

        public Test? GetTestData()
        {
            return _test;
        }

        public void Initialize()
        {
            // 启动工作线程
            _timer.Change(0, 1000);
        }

        public void ResetProductData()
        {
            _product = null;
        }

        public void ResetTestData()
        {
            _test = null;
        }

        public bool SetPhenomenon(string phenocode, string memo)
        {
            if (_test != null && _test.Phenocode != null)
            {
                _test.Phenocode = phenocode;
                if (memo != string.Empty)
                {
                    // 其他试验现象描述(数据库内尚未增加该字段)
                }
                return true;
            }
            return false;
        }

        public void SetProductData(Product product)
        {
            if (product != null)
                _product = product;
        }

        public void SetTestData(Test test)
        {
            if (test != null)
                _test = test;
        }

        public void StartRecording()
        {
            _status = MasterStatus.Recording;
        }

        public void StopRecording()
        {
            _status = MasterStatus.Idle;
        }

        /* 试验服务器工作函数 */
        public void DoTest(object? state)
        {

            /* 获取传感器最新采集数据 */
            _signalRData.SensorData.FurnaceTemp = AppGlobal.Sensors[0].Outputvalue;  // 环形炉温度
            _signalRData.SensorData.CGasFlow = AppGlobal.Sensors[1].Outputvalue; ;   // 载气流量
            _signalRData.SensorData.DGasFlow = AppGlobal.Sensors[2].Outputvalue; ;   // 稀释气流量
            /* 设置计算数据最新值 */
            // 炉壁温度偏差
            if (_test != null && _test.Heattemp.HasValue)
            {
                _signalRData.CaculationData.DeltaTemp = _signalRData.SensorData.FurnaceTemp - _test.Heattemp.Value;
            }
            else
            {
                _signalRData.CaculationData.DeltaTemp = _signalRData.SensorData.FurnaceTemp - default(double);
            }
            /* 设备最新状态数据 */
            // 鼠笼罩开闭状态
            // ...

            _signalRData.Counter = _counter++;

            // 根据试验服务器状态执行响应操作
            switch (_status)
            {
                case MasterStatus.Idle:
                    break;
                case MasterStatus.Preparing:
                    if (CheckStartCriteria())
                    {
                        _status = MasterStatus.Ready;
                    }
                    break;
                case MasterStatus.Ready:
                    if (CheckStartCriteria())
                    {
                        _status = MasterStatus.Ready;
                    }
                    else
                    {
                        _status = MasterStatus.Preparing;
                    }
                    break;
                case MasterStatus.Recording:
                    _signalRData.Counter = _counter;
                    // 将最新采集数据添加至缓存
                    _bufSensorData.Add(new SensorData()
                    {
                        Counter = _counter,
                        FurnaceTemp = _signalRData.SensorData.FurnaceTemp,
                        CGasFlow = _signalRData.SensorData.CGasFlow,
                        DGasFlow = _signalRData.SensorData.DGasFlow
                    });
                    // 增加数据缓存计数器
                    _counter++;
                    break;
                case MasterStatus.Complete:
                    break;
                case MasterStatus.Exception:
                    break;
                default:
                    break;
            }
            _signalRData.Status = _status;
            _signalRData.WorkMode = _workmode;
            //向客户端发送数据广播
            var jsonData = JsonSerializer.Serialize(_signalRData);
            _broadcast.Clients.All.SendAsync("ServerBroadCast", jsonData);
        }

        /* 试验后期处理函数(试验数据处理、试验数据输出、报表生成等) */
        public async void PostTestProcess()
        {
            /* 创建本地存储目录 */
            string prodpath = $"D:\\GB20285\\{_product?.Productid}";
            string smppath = $"{prodpath}\\{_test?.Testid}";
            string datapath = $"{smppath}\\data";
            string rptpath = $"{smppath}\\report";
            try
            {
                /* 创建本次试验结果文件的存储目录 */
                //试验样品根目录
                Directory.CreateDirectory(prodpath);
                //本次试验根目录
                Directory.CreateDirectory(smppath);
                //本次试验原始数据目录
                Directory.CreateDirectory(datapath);
                //本次试验报表目录
                Directory.CreateDirectory(rptpath);

                var ctx = _dbFactory.CreateDbContext();

                /* 保存本次试验数据文件 */
                //传感器采集数据
                using (var writer = new StreamWriter($"{datapath}\\sensordata.csv", false))
                using (var csvwriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    //写入数据内容
                    await csvwriter.WriteRecordsAsync(_bufSensorData);
                }
                //其他数据文件(比如视频记录等)
                //...

                /* 生成本次试验的报表 */
                //设置EPPlus license版本为非商用版本
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                //设置CSV文件格式
                var format = new ExcelTextFormat()
                {
                    Delimiter = ',',
                    EOL = "\r"    // 修改行尾结束符,默认为 "\r\n" ("\r"为回车符 "\n"为换行符);
                                  // 字符类型引用符 format.TextQualifier = '"';
                };
                //操作Excel文件
                using (ExcelPackage package = new ExcelPackage(new FileInfo($"D:\\GB20285\\template_report_{_serverid}.xlsx")))
                {
                    //取得传感器数据页面
                    ExcelWorksheet sheet_sensordata = package.Workbook.Worksheets.ElementAt(1);
                    //填充传感器原始数据(含首行标题)
                    sheet_sensordata.Cells["A1"].LoadFromText(new FileInfo($"{datapath}\\sensordata.csv"), format, null, true);
                    //取得小鼠体重页面
                    ExcelWorksheet sheet_mouseweight = package.Workbook.Worksheets.ElementAt(2);
                    //将小鼠体重页面拷贝至试验报表的 [小鼠体重] 页面
                    if(_product != null && _test != null)
                    {
                        var records = ctx.MouseWeights.AsNoTracking()
                            .Where(x => x.ProductId == _product.Productid && x.TestId == _test.Testid)
                            .OrderBy(x => x.MouseId)
                            .ToList();
                        sheet_mouseweight.Cells["A1"].LoadFromDataTable(Utils.ToDataTable(records));
                    }                    

                    /* 设置报表首页部分数据 */
                    //取得报表首页页面(页面索引从 0 开始)
                    ExcelWorksheet sheet_main = package.Workbook.Worksheets.ElementAt(0);
                    if (_product != null && _test != null)
                    {
                        //报告编号
                        sheet_main.Cells["AB2"].Value = _product.Productid;
                        //实验室温度
                        sheet_main.Cells["G9"].Value = _test.Ambtemp;
                        //环境湿度
                        sheet_main.Cells["AC9"].Value = _test.Ambhumi;
                        //产品名称
                        sheet_main.Cells["G10"].Value = _product.Productname;
                        //样品编号
                        sheet_main.Cells["AA10"].Value = _product.Productid;
                        //试验编号(样品标识号)
                        sheet_main.Cells["AA11"].Value = _test.Testid;
                        //规格型号
                        sheet_main.Cells["G12"].Value = _product.Specification;
                        //试件尺寸
                        sheet_main.Cells["AA12"].Value = _test.Specilength;
                        //样品形态
                        sheet_main.Cells["G13"].Value = _product.Shape;
                        //加热温度
                        sheet_main.Cells["AA13"].Value = _test.Heattemp;

                        //状态调节T
                        //状态调节H
                        //状态调节S

                        //设备编号
                        sheet_main.Cells["G15"].Value = _test.Apparatusid;
                        //设备检定日期From
                        sheet_main.Cells["AA14"].Value = _test.Checkdatef?.ToString("d");
                        //设备检定日期To
                        sheet_main.Cells["AA15"].Value = _test.Checkdatet?.ToString("d");

                        //实验动物来源
                        //实验动物品种
                        //实验动物级别

                        //检验依据
                        sheet_main.Cells["AA17"].Value = _test.According;
                        //材料产烟浓度等级
                        switch (_test.Safetylevel)
                        {
                            case "AQ1":
                                sheet_main.Cells["G18"].Value = "■";
                                sheet_main.Cells["G45"].Value = "■";
                                break;
                            case "AQ2":
                                sheet_main.Cells["L18"].Value = "■";
                                sheet_main.Cells["K45"].Value = "■";
                                break;
                            case "ZA1":
                                sheet_main.Cells["Q18"].Value = "■";
                                sheet_main.Cells["O45"].Value = "■";
                                break;
                            case "ZA2":
                                sheet_main.Cells["V18"].Value = "■";
                                sheet_main.Cells["S45"].Value = "■";
                                break;
                            case "ZA3":
                                sheet_main.Cells["AA18"].Value = "■";
                                sheet_main.Cells["W45"].Value = "■";
                                break;
                            case "WX":
                                sheet_main.Cells["AA45"].Value = "■";
                                break;
                            default:
                                break;
                        }
                        //试件质量
                        sheet_main.Cells["G19"].Value = _test.Speciweight;
                        //残余质量
                        sheet_main.Cells["R19"].Value = _test.Speciweightpost;
                        //产烟率
                        sheet_main.Cells["AA19"].Value = _test.Smokerate;
                        //充分产烟率的确定方法
                        if (_test.Smokerate != null)
                        {
                            var tempvalue = (int)(_test.Smokerate * 10);
                            if (tempvalue > 950)
                            {
                                sheet_main.Cells["G21"].Value = "■";
                            }
                            //else if () { }
                        }

                        //载气流量
                        sheet_main.Cells["G23"].Value = _test.Cgasflow;
                        //稀释气流量
                        sheet_main.Cells["AA23"].Value = _test.Dgasflow;
                        //麻醉性结论
                        if (_test.Nounresult == true)
                        {
                            sheet_main.Cells["K43"].Value = "合格";
                        }
                        else if (_test.Nounresult == false)
                        {
                            sheet_main.Cells["K43"].Value = "不合格";
                        }
                        else
                        {
                            sheet_main.Cells["K43"].Value = "待观察";
                        }
                        //刺激性结论
                        if (_test.Irriresult == true)
                        {
                            sheet_main.Cells["S43"].Value = "合格";
                        }
                        else if (_test.Irriresult == false)
                        {
                            sheet_main.Cells["S43"].Value = "不合格";
                        }
                        else
                        {
                            sheet_main.Cells["S43"].Value = "待观察";
                        }
                        //综合结论
                        if (_test.Testresult == true)
                        {
                            sheet_main.Cells["G46"].Value = "■";
                            sheet_main.Cells["K46"].Value = "";
                        }
                        else if (_test.Testresult == false)
                        {
                            sheet_main.Cells["G46"].Value = "";
                            sheet_main.Cells["K46"].Value = "■";
                        }
                        else
                        {
                            sheet_main.Cells["G46"].Value = "";
                            sheet_main.Cells["K46"].Value = "";
                        }
                        //备注
                        sheet_main.Cells["G47"].Value = _test.Comment;
                        //检验人员
                        sheet_main.Cells["E50"].Value = _test.Operator;
                        //试验日期
                        sheet_main.Cells["AB50"].Value = _test.Testdate?.ToString("d");

                        //小鼠体重恢复情况
                        if (_test.Testresult == true)
                        {
                            if (_test.Recoveryday != null)
                            {
                                sheet_main.Cells["G34"].Value = "■";
                                sheet_main.Cells["L34"].Value = _test.Recoveryday;
                            }
                        }
                        else if (_test.Testresult == false)
                        {
                            sheet_main.Cells["G35"].Value = "■";
                        }
                        //试验现象
                        if (_test.Phenocode != null)
                        {
                            var pheno = new StringBuilder();
                            if (_test.Phenocode[1] == '1') pheno.Append("欲跑不能,");
                            if (_test.Phenocode[2] == '1') pheno.Append("呼吸变化,");
                            if (_test.Phenocode[3] == '1') pheno.Append("惊跳,");
                            if (_test.Phenocode[4] == '1') pheno.Append("挣扎,");
                            if (_test.Phenocode[5] == '1') pheno.Append("不能翻身,");
                            if (_test.Phenocode[6] == '1') pheno.Append("昏迷,");
                            if (_test.Phenocode[7] == '1') pheno.Append("痉挛,");
                            if (_test.Phenocode[8] == '1') pheno.Append("视力丧失,");
                            if (_test.Phenocode[9] == '1') pheno.Append("流泪,");
                            if (_test.Phenocode[10] == '1') pheno.Append("肿胀,");
                            if (_test.Phenocode[11] == '1') pheno.Append("闭目,");
                            //去掉末尾","
                            pheno.Length--;
                            //输出现象描述
                            sheet_main.Cells["G36"].Value = pheno.ToString();
                        }

                        //保存本次试验报表
                        await package.SaveAsAsync($"{rptpath}\\report.xlsx");
                    }

                }

                /* 使用DevExpress Office API打开报表文件、执行公式计算并回填关键数据项 */
                using (var workbook = new Workbook())
                {
                    // 加载报表文件
                    workbook.LoadDocument($"{rptpath}\\report.xlsx", DocumentFormat.Xlsx);
                    // 计算报表中所有公式
                    workbook.CalculateFull();
                    // 取得报表首页Sheet对象(Sheet索引从0开始)
                    Worksheet worksheet = workbook.Worksheets[0];
                    /* 根据报表计算结果回填本次试验的【结论判定属性】 */
                    // 麻醉性结论
                    // ...
                    // 刺激性结论
                    // ...
                    // 综合结论
                    // ...

                    // 保存报表
                    //workbook.SaveDocument($"{rptpath}\\report.xlsx", DocumentFormat.OpenXml);
                    // 导出报表首页为PDF格式
                    PdfExportOptions options = new PdfExportOptions();
                    options.DocumentOptions.Author = "李西黎";
                    workbook.ExportToPdf($"{rptpath}\\report.pdf", options, "main");
                }

                //保存本次试验数据至试验数据库                
                if (_test != null)
                {
                    ctx.Tests.Add(_test);
                }
                await ctx.SaveChangesAsync();
            }
            catch (Exception)
            {
                // 输出异常日志
                // ...
                throw;
            }
        }
    }

    //定义控制器任务模式枚举
    public enum MasterWorkMode
    {
        Standby = 0,     //待命模式(一般为刚登录系统时)
        Calibration,     //标定、校准模式
        SampleTest       //样品试验模式
    }

    //定义控制器状态枚举
    public enum MasterStatus
    {
        Idle = 0,       //空闲状态(一般为刚登录系统时)
        Preparing,      //试验或标定条件准备中(比如:将温度恒定在某个范围)
        Ready,          //达到进行样品试验或系统标定的条件(比如:温度已经恒定在规定范围)
        Recording,      //开启数据记录
        Complete,       //当前试验结束
        Exception       //发生异常
    }

    /* 传感器数据 */
    public class SensorData
    {
        // 数据计数器(等效于试验计时器)
        public int Counter { get; set; }
        // 环形炉温度
        public double FurnaceTemp { get; set; }
        // 载气流量
        public double CGasFlow { get; set; }
        // 稀释气流量
        public double DGasFlow { get; set; }
    }

    /* 计算数据 */
    public class CaculationData
    {
        // 炉壁温度偏差
        public double DeltaTemp { get; set; }
    }

    public class ApparatusStatus
    {
        // 三通阀状态
        public bool ValveStatus { get; set; }
        // 鼠笼开闭状态
        public bool CageStatus { get; set; }
        public ApparatusStatus() { }
    }

    // 定义用于标识试验控制器实时返回消息的对象类型
    public class TestServerMessage
    {
        public string Time { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class SignalRData
    {
        public int ServerId { get; set; }
        public MasterWorkMode WorkMode { get; set; }
        public MasterStatus Status { get; set; }
        public int Counter { get; set; }
        public SensorData SensorData { get; set; }
        public CaculationData CaculationData { get; set; }
        public ApparatusStatus ApparatusStatus { get; set; }
        public List<TestServerMessage> ServerMessages { get; set; }

        public SignalRData()
        {
            ServerId = 0;
            WorkMode = MasterWorkMode.Standby;
            Status = MasterStatus.Idle;
            SensorData = new SensorData();
            CaculationData = new CaculationData();
            ApparatusStatus = new ApparatusStatus();
            ServerMessages = new List<TestServerMessage>();
        }
    }
}
