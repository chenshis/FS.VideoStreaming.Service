using System;
using System.Collections.Generic;
using System.Text;

namespace FS.VideoStreaming.Infrastructure.Config
{
    /// <summary>
    /// 系统常量
    /// </summary>
    public class SystemConstant
    {
        public const string CameraConfigPath = "CameraConfigurations";

        public const string CameraConfigFileName = "Camera.json";

        /// <summary>
        /// 宿主地址列表
        /// </summary>
        public const string HostPort = "hostPort";

        /// <summary>
        /// 端口
        /// </summary>
        public const string HostFileName = "host.json";

        /// <summary>
        /// 进程名称
        /// </summary>
        public const string FfmpegProcessName = "ffmpeg";

        /// <summary>
        /// 摄像头集合缓存
        /// </summary>
        public const string CameraCacheKey = "CameraCollectionCache";

        /// <summary>
        /// 摄像头文件格式
        /// </summary>
        public const string CameraFormat = ".m3u8";

        /// <summary>
        /// Nginx
        /// </summary>
        public const string Nginx = "nginx";

        /// <summary>
        /// html
        /// </summary>
        public const string Html = "html";
    }
}
