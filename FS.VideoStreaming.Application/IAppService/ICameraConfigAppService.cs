using System;
using System.Collections.Generic;
using System.Text;

namespace FS.VideoStreaming.Application.IAppService
{
    /// <summary>
    /// 摄像头配置
    /// </summary>
    public interface ICameraConfigAppService
    {
        /// <summary>
        /// 保存摄像头地址
        /// </summary>
        /// <param name="cameraAddresses">摄像头地址</param>
        /// <returns></returns>
        bool Save(List<string> cameraAddresses);
    }
}
