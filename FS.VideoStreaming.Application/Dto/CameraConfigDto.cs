
using System.Collections.Generic;

namespace FS.VideoStreaming.Application.Dto
{
    /// <summary>
    /// 摄像头配置数据传输对象
    /// </summary>
    public class CameraConfigDto
    {
        /// <summary>
        /// rtsp协议地址集合
        /// </summary>
        public List<CameraConfigBaseDto> RtspAddresses { get; set; }
    }

    /// <summary>
    /// 摄像头基础信息
    /// </summary>
    public class CameraConfigBaseDto
    {
        /// <summary>
        /// 推流进程ID
        /// </summary>
        public int ProcessId { get; set; }
        /// <summary>
        /// 可播放地址
        /// </summary>
        public string PlayUrl { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// url地址
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
    }
}
