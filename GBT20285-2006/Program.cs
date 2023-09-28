using GBT20285_2006.DBContext;
using GBT20285_2006.Models;
using GBT20285_2006.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.EventLog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddDbContextFactory<GB20285DBContext>(option =>
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("GB20285"));
});

//添加应用程序全局存储对象
//builder.Services.AddSingleton<AppGlobal>();
//注册试验传感器数据采集服务
builder.Services.AddHostedService<Daq>();
//注册试验服务器对象
//builder.Services.AddSingleton<TestServer>();
//注册试验服务器容器对象
builder.Services.AddSingleton<TestServerContainer>();
//配置Windows日志记录器显示信息
builder.Services.Configure<EventLogSettings>(config =>
{
    config.LogName = "GB20285 Test Server";
    //该属性对应 Windows事件记录日志程序中的 "来源" 查询字段
    config.SourceName = "GB20285TestServer";
});

// 支持以WindowsService方式启动试验服务器
builder.Host.UseWindowsService();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<BroadcastHub>("/broadcast");

app.Run();
