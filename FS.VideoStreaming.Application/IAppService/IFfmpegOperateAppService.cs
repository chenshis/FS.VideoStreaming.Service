using FS.VideoStreaming.Application.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace FS.VideoStreaming.Application.IAppService
{
    /// <summary>
    /// ffmpeg工具操作接口
    /// </summary>
    public interface IFfmpegOperateAppService
    {
        /// <summary>
        /// 设置摄像头缓存
        /// </summary>
        /// <param name="pid">进程id</param>
        /// <param name="dto">数据传输对象</param>
        /// <param name="isDelete">是否删除</param>
        /// <returns></returns>
        bool SetCameraCache(int pid, CameraConfigBaseDto dto, bool isDelete);

        /// <summary>
        /// 检测摄像头数据信息是否变更
        /// </summary>
        /// <param name="addConfigBaseDtos">新增摄像头信息</param>
        /// <param name="deleteConfigBaseDtos">移除摄像头信息</param>
        /// <returns></returns>
        bool IsValidate(out List<CameraConfigBaseDto> addConfigBaseDtos, out List<CameraConfigBaseDto> deleteConfigBaseDtos);

        /// <summary>
        /// 启动摄像头拉流
        /// </summary>
        /// <param name="dto"></param>
        int Start(CameraConfigBaseDto dto);

        /// <summary>
        /// 检测进程是否活着
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        bool IsProcessRunning(int pid);

        /// <summary>
        /// 杀死ffmpeg进程
        /// </summary>
        void KillProcess(int pid = -1);

        /// <summary>
        /// 运行ffmpeg进程
        /// </summary>
        void Run();

    }
}
