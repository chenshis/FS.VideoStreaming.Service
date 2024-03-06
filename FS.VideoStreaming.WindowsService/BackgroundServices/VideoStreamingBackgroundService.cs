using FS.VideoStreaming.Application.Dto;
using FS.VideoStreaming.Application.IAppService;
using FS.VideoStreaming.Infrastructure.Config;
using System.Diagnostics;

namespace FS.VideoStreaming.WindowsService.BackgroundServices
{
    public class VideoStreamingBackgroundService : CssBackgroundService
    {
        private readonly IConfiguration _configuration;

        private static string BasePath;

        public VideoStreamingBackgroundService(IServiceProvider serviceProvider, IConfiguration configuration) : base(serviceProvider)
        {
            _configuration = configuration;
            Schedule = "*/10 * * * * *";
            BasePath = _configuration["FileM3U8path:Path"]?? "D:\\nginx-1.24.0\\html";
            KillAllProcess();
            InitVideoInfo(serviceProvider);
            PullFlowStart();
        }

        protected override string Schedule { get; set; }

        protected override Task Process(IServiceProvider serviceProvider)
        {
            Logger.LogInformation($"当前流媒体守护服务轮询时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Process[] processArr =System.Diagnostics.Process.GetProcessesByName("ffmpeg");
            List<int> pIdList = (processArr != null && processArr.Length > 0) ? processArr.Select(m => m.Id).ToList() : new List<int>();

            for (int i = 0; i < CamerConfigData.CamerasList.Count; i++)
            {
                bool isRestart = true;
                if (pIdList.Contains(CamerConfigData.CamerasList[i].ProcessId) && CamerConfigData.CamerasList[i].ProcessId > 0)
                {
                    string tsFilePath = CamerConfigData.CamerasList[i].PlayUrl;
                    if (File.Exists(tsFilePath))
                    {
                        FileInfo fi = new FileInfo(tsFilePath);
                        if ((DateTime.Now - fi.LastWriteTime).TotalSeconds < 100)//文件未过期 一直在拉流
                        {
                            isRestart = false;
                        }
                    }
                    else
                    {
                        //覆盖文件时，会存在 JudgeFileName 刚好不存在的情况(ffmpeg会先删除文件然后再生成，所以必须要保证第一次开启所有的摄像头都能生成m3u8文件)
                        isRestart = false;
                    }
                }
                //重启进程
                if (isRestart)
                {
                    if (CamerConfigData.CamerasList[i].ProcessId != 0)
                    {
                        if (pIdList.Contains(CamerConfigData.CamerasList[i].ProcessId))
                        {
                            processArr.FirstOrDefault(p => p.Id == CamerConfigData.CamerasList[i].ProcessId)?.Kill();
                        }

                        Logger.LogInformation($"杀掉进程：{DateTime.Now:yyyy-MM-dd HH:mm:ss}-{CamerConfigData.CamerasList[i]}");
                        CamerConfigData.CamerasList[i].ProcessId = 0;
                        string M3u8FileName = CamerConfigData.CamerasList[i].PlayUrl;
                        CamerConfigData.CamerasList[i].ProcessId = PullFlowService(CamerConfigData.CamerasList[i]); //重启推流
                        Logger.LogInformation($"起进程：{DateTime.Now:yyyy-MM-dd HH:mm:ss}-{CamerConfigData.CamerasList[i].ProcessId}");

                    }
                }
            }
            return Task.CompletedTask;
        }

        #region 私有方法
        private void PullFlowStart()
        {
            for (int i = 0; i < CamerConfigData.CamerasList.Count; i++)
            {
                CamerConfigData.CamerasList[i].ProcessId = 0;
                int pid = PullFlowService(CamerConfigData.CamerasList[i]); //重启推流
                if (pid > 0)
                {
                    CamerConfigData.CamerasList[i].ProcessId = pid;
                    CamerConfigData.CamerasList[i].PlayUrl = BasePath + "\\" + CamerConfigData.CamerasList[i].Name + ".m3u8";
                }
            }
        }
        /// <summary>
        /// 摄像头推流
        /// </summary>
        private int PullFlowService(CameraConfigBaseDto item)
        {
            string M3u8FileName = item.Name + ".m3u8";
            string RtspPath = item.Url;

            if (!Directory.Exists(BasePath))
            {
                Directory.CreateDirectory(BasePath);
            }

            Process p = null;
            int Processid = 0;
            try
            {
                var startInfo = new ProcessStartInfo();
                startInfo.FileName = "ffmpeg.exe";

                startInfo.Arguments = " -f rtsp -rtsp_transport tcp -i " + RtspPath + " -fflags flush_packets -max_delay 1 -an -flags -global_header -hls_time 10 -hls_list_size 10 -hls_flags 10 -vcodec copy -s 216x384 -b 1024k -y  ";
                startInfo.Arguments += (BasePath + "\\" + M3u8FileName);

                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.Verb = "RunAs";//以管理员身份运行

                p = System.Diagnostics.Process.Start(startInfo);
                p.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataHandler);
                Processid = p != null ? p.Id : 0;//进出ID
            }
            catch (Exception ex)
            {
                p?.Close();//?的作用为:如果进出为空就不执行.如果进行不为空就执行
            }
            return Processid;
        }
        /// <summary>
        /// 异常处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ErrorDataHandler(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Logger.LogInformation($"异常时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}，Error：{e.Data}");
            }
        }
        /// <summary>
        /// 缓存摄像头信息
        /// </summary>
        private void InitVideoInfo(IServiceProvider serviceProvider)
        {
            var videoList = serviceProvider.GetService<ICameraConfigAppService>().GetCameraConfigs();

            if (videoList != null)
            {
                CamerConfigData.CamerasList = videoList.RtspAddresses.ToList();
            }
        }
        /// <summary>
        /// 关闭所有推流进程
        /// </summary>
        private void KillAllProcess()
        {
            List<Process> processList = System.Diagnostics.Process.GetProcessesByName("ffmpeg").ToList();
            if (processList != null && processList.Count > 0)
            {
                processList.ForEach(p =>
                {
                    p.Kill();
                });
            }
        }
        #endregion
    }
}
