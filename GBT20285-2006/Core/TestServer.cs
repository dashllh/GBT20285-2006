using GBT20285_2006.Models;
using GBT20285_2006.DBContext;
using GBT20285_2006.Utility;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Globalization;
using CsvHelper;


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

        public async Task<bool> SetPhenomenon(string phenocode, string memo)
        {
            if (_test == null)
                return false;

            _test.Phenocode = phenocode;
            //if (memo != string.Empty)
            //{
            //    // 其他试验现象描述(数据库内尚未增加该字段)
            //}

            // 试验已经完成,执行试验后期处理
            if (_status == MasterStatus.Complete)
            {
                await PostTestProcess();
            }

            return true;
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

        /*
         * 功能: 停止数据记录
         * 参数:
         *       save - 是否保存本次试验数据(false: 不保存)
         */
        public void StopRecording(bool save = true)
        {
            if (!save)
            {
                // 清空试验数据缓存
                _test = null;
                // 计时器归零
                _counter = 0;
                // 重置试验服务器状态为 [空闲],等待下一次试验
                _status = MasterStatus.Idle;
            }
            else
            {
                // 计时器归零
                _counter = 0;
                // 设置试验服务器状态为 [完成]
                _status = MasterStatus.Complete;
            }
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

        /* 试验后期处理函数(处理并保存试验数据,但不包括试验结论判定及试验报表生成) */
        public async Task PostTestProcess()
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

                /* 保存本次试验数据文件 */
                //传感器采集数据
                using (var writer = new StreamWriter($"{datapath}\\sensordata.csv", false))
                using (var csvwriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    //写入数据内容
                    await csvwriter.WriteRecordsAsync(_bufSensorData);
                }

                //保存本次试验数据至试验数据库
                var ctx = _dbFactory.CreateDbContext();
                if (_test != null)
                {
                    ctx.Tests.Add(_test);
                    // 新建小鼠体重数据记录
                    List<MouseWeight> weights = new List<MouseWeight>();
                    for (short i = 0; i < _test.Mousecnt; i++)
                    {
                        weights.Add(new MouseWeight()
                        {
                            ProductId = _test.Specimenid,
                            TestId = _test.Testid,
                            MouseId = i
                        });
                    }
                    ctx.MouseWeights.AddRange(weights.ToArray());
                }
                await ctx.SaveChangesAsync();
            }
            catch (Exception)
            {
                // 将本次试验数据保存至指定目录
                // ...
                // 输出异常日志
                // ...
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
