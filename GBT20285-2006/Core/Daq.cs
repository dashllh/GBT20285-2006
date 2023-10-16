using GBT20285_2006.DBContext;
using GBT20285_2006.Models;
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
            // 检查设备传感器连接

            // 建立传感器连接

            // 初始化传感器对象缓存
            using var ctx = _dbContextFactory.CreateDbContext();
            AppGlobal.Sensors = ctx.Sensors.ToDictionary(X => X.Sensorid);

            // 启动数据采集线程
            timer.Change(0,1000);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void FetchIOData(object? status)
        {
            // 炉壁温度
            AppGlobal.Sensors[0].Inputvalue = 5.569;
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
