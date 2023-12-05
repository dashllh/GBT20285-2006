using GBT20285_2006.DBContext;
using GBT20285_2006.Models;
using GBT20285_2006.Core;
using GBT20285_2006.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.EventLog;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
});
builder.Services.AddSignalR();
builder.Services.AddDbContextFactory<GB20285DBContext>(option =>
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("GB20285"));
});

//���Ӧ�ó���ȫ�ִ洢����
//builder.Services.AddSingleton<AppGlobal>();
//ע�����鴫�������ݲɼ�����
builder.Services.AddHostedService<Daq>();
//ע���������������
//builder.Services.AddSingleton<TestServer>();
//ע�������������������
builder.Services.AddSingleton<TestServerContainer>();
//����Windows��־��¼����ʾ��Ϣ
builder.Services.Configure<EventLogSettings>(config =>
{
    config.LogName = "GB20285 Test Server";
    //�����Զ�Ӧ Windows�¼���¼��־�����е� "��Դ" ��ѯ�ֶ�
    config.SourceName = "GB20285TestServer";
});

// ���SeriLog֧��
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// ֧����WindowsService��ʽ�������������
builder.Host.UseWindowsService();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSerilogRequestLogging();

app.UseStaticFiles();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<BroadcastHub>("/broadcast");

app.Run();
