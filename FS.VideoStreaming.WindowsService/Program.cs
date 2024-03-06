using FS.VideoStreaming.Application.AppService;
using FS.VideoStreaming.Application.IAppService;
using FS.VideoStreaming.Infrastructure.Config;
using FS.VideoStreaming.WindowsService.BackgroundServices;
using NLog.Extensions.Logging;
using NLog.Web;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Host.UseNLog();
builder.Host.UseWindowsService();
// ���Ķ˿ں�
var configBuilder = new ConfigurationBuilder()
   .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
   .AddJsonFile(SystemConstant.HostFileName)
   .Build();
var port = configBuilder.GetSection(SystemConstant.HostPort).Value;
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ����ע��
builder.Services.AddScoped<ICameraConfigAppService, CameraConfigAppService>();


// ��̨����
builder.Services.AddHostedService<VideoStreamingBackgroundService>();


var app = builder.Build();


// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}

app.UseAuthorization();

app.MapControllers();

app.Run();
