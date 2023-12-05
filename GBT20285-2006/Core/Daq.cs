using GBT20285_2006.DBContext;
using GBT20285_2006.Models;
using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace GBT20285_2006.Core
{
    public class Daq : IHostedService, IDisposable
    {
        private readonly IDbContextFactory<GB20285DBContext> _dbContextFactory;
        private Timer timer;
        
        public Daq(IDbContextFactory<GB20285DBContext> context)
        {
            _dbContextFactory = context;
            timer = new Timer(FetchIOData);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // 检查设备传感器连接

                // 建立传感器连接

                // 初始化传感器对象缓存
                using var ctx = _dbContextFactory.CreateDbContext();
                AppGlobal.Sensors = ctx.Sensors.ToDictionary(X => X.Sensorid);

                // 启动数据采集线程
                timer.Change(0, 1000);

                // 输出日志
                Log.Information("Establish sensor connection successful.");

                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return Task.FromException(e);
            }            
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void FetchIOData(object? status)
        {
            // 从传感器获取最新采集数据
            
            // 炉壁温度
            AppGlobal.Sensors[0].Inputvalue = 600;
            // 参照物温度
            AppGlobal.Sensors[3].Inputvalue = 400;
            // 载气流量
            AppGlobal.Sensors[1].Inputvalue = 10.02;
            // 稀释气流量
            AppGlobal.Sensors[2].Inputvalue = 8.07;
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}
