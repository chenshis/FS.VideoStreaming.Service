using FS.VideoStreaming.Infrastructure.Config;

namespace FS.VideoStreaming.WindowsService.BackgroundServices
{
    public class VideoStreamingBackgroundService : CssBackgroundService
    {
        public VideoStreamingBackgroundService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Schedule = "*/10 * * * * *";
        }

        protected override string Schedule { get; set; }

        protected override Task Process(IServiceProvider serviceProvider)
        {
            Logger.LogInformation($"当前流媒体服务轮询时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            return Task.CompletedTask;
        }
    }
}
