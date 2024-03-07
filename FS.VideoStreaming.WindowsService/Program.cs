using FS.VideoStreaming.Application.AppService;
using FS.VideoStreaming.Application.IAppService;
using FS.VideoStreaming.Infrastructure.Config;
using FS.VideoStreaming.WindowsService.BackgroundServices;
using FS.VideoStreaming.WindowsService.Middlewares;
using Microsoft.Extensions.Hosting.WindowsServices;
using NLog.Web;
using System.Reflection;


var webApplicationOptions = new WebApplicationOptions()
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
};
var builder = WebApplication.CreateBuilder(webApplicationOptions);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

// 依赖注入
builder.Services.AddScoped<ICameraConfigAppService, CameraConfigAppService>();
builder.Services.AddScoped<IFfmpegOperateAppService, FfmpegOperateAppService>();
// 后台服务
builder.Services.AddHostedService<VideoStreamingBackgroundService>();

builder.Host.UseNLog();
builder.Host.UseWindowsService();
// 更改端口号
builder.WebHost.UseUrls($"http://0.0.0.0:{GetPort()}");

var app = builder.Build();


// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}
app.UseFfmpeg();
app.UseServiceStop();
app.UseAuthorization();

app.MapControllers();

app.Run();




// 获取端口号
string GetPort()
{
    var configBuilder = new ConfigurationBuilder()
   .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
   .AddJsonFile(SystemConstant.HostFileName)
   .Build();
    var port = configBuilder.GetSection(SystemConstant.HostPort).Value;
    return port;
}