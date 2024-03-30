using System;
using System.Collections.Generic;
using System.Text;

namespace FS.VideoStreaming.Application.Dto
{
    /// <summary>
    /// 摄像头删除配置模型
    /// </summary>
    public class CameraConfigDeleteDto
    {
        /// <summary>
        /// 待删除地址
        /// </summary>
        public string RtspUrl { get; set; }
    }
}
