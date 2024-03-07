using FS.VideoStreaming.Application.Dto;
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
        /// <param name="cameraConfigDto">摄像头地址</param>
        /// <returns></returns>
        bool Save(CameraConfigDto cameraConfigDto);

        /// <summary>
        /// 获取所有摄像头配置
        /// </summary>
        /// <returns></returns>
        CameraConfigDto GetCameraConfigs();

        /// <summary>
        /// 验证摄像头地址是否合法
        /// </summary>
        /// <param name="addresses">地址校验</param>
        /// <returns></returns>
        bool IsValidRtspAddress(List<string> addresses);
    }
}
