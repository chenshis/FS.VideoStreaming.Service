using FS.VideoStreaming.Application.Dto;
using FS.VideoStreaming.Application.IAppService;
using FS.VideoStreaming.Infrastructure.Config;

namespace FS.VideoStreaming.WindowsService.BackgroundServices
{
    public class VideoStreamingBackgroundService : CssBackgroundService
    {
        public VideoStreamingBackgroundService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            Schedule = "*/30 * * * * *";
        }

        protected override string Schedule { get; set; }

        protected override Task Process(IServiceProvider serviceProvider)
        {
            Logger.LogInformation($"当前流媒体守护服务轮询时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            // 激活服务接口
            var ffmpegOperateAppService = serviceProvider.GetService<IFfmpegOperateAppService>();
            var cameraConfigAppService = serviceProvider.GetService<ICameraConfigAppService>();
            // 验证摄像头信息是否改变
            if (!ffmpegOperateAppService.IsValidate(out List<CameraConfigBaseDto> addConfigs, out List<CameraConfigBaseDto> deleteConfigs))
            {
                return Task.CompletedTask;
            }
            // 新增进程
            if (addConfigs.Count > 0)
            {
                // 检测摄像头地址是否合法
                var urls = addConfigs.Select(t => t.Url).ToList();
                if (!cameraConfigAppService.IsValidRtspAddress(urls))
                {
                    Logger.LogError($"轮询服务检测：存在不合法的摄像头配置信息；请检查配置文件，确认是正确的rtsp协议！");
                }

                foreach (var item in addConfigs)
                {
                    try
                    {
                        var pid = ffmpegOperateAppService.Start(item);
                        if (pid <= 0)
                        {
                            Logger.LogError($"轮询服务检测新增进程运行失败，摄像头名称：{item.Name}；摄像头地址：{item.Url}");
                        }
                        else
                        {
                            ffmpegOperateAppService.SetCameraCache(pid, item, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"轮询服务检测新增进程异常：{ex.Message}；摄像头名称：{item.Name}；摄像头地址：{item.Url}");
                    }
                }
            }

            // 删除进程
            if (deleteConfigs.Count > 0)
            {
                foreach (var item in deleteConfigs)
                {
                    try
                    {
                        Logger.LogInformation($"轮询服务删除进程：{item.ProcessId}；摄像头名称：{item.Name}；摄像头地址：{item.Url}");
                        ffmpegOperateAppService.KillProcess(item.ProcessId);
                        ffmpegOperateAppService.SetCameraCache(item.ProcessId, item, true);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, $"轮询服务检测删除进程异常：{ex.Message}；摄像头名称：{item.Name}；摄像头地址：{item.Url}");
                    }

                }
            }

            return Task.CompletedTask;
        }


    }
}
