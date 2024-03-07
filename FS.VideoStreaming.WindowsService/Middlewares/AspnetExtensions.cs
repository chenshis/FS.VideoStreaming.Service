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
    }
}
