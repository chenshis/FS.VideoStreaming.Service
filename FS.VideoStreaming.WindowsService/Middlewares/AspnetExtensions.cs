using FS.VideoStreaming.Application.IAppService;

namespace FS.VideoStreaming.WindowsService.Middlewares
{
    public static class AspnetExtensions
    {
        /// <summary>
        /// 启动ffmpeg工具
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseFfmpeg(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var ffmpegOperateAppService = scope.ServiceProvider.GetService<IFfmpegOperateAppService>();
            ffmpegOperateAppService.Run();
            return app;
        }

        public static void UseServiceStop(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var lifetime = scope.ServiceProvider.GetService<IHostApplicationLifetime>();
            var ffmpegOperateAppService = scope.ServiceProvider.GetService<IFfmpegOperateAppService>();
            var logger = scope.ServiceProvider.GetService<ILogger<Program>>();

            lifetime.ApplicationStopped.Register((service) =>
            {
                logger.LogInformation("程序停止！");
                if (service != null && service is IFfmpegOperateAppService appService)
                {
                    appService.KillProcess();
                }
            }, ffmpegOperateAppService);
        }
    }
}
