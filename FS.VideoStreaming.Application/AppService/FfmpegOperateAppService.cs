using FS.VideoStreaming.Application.Dto;
using FS.VideoStreaming.Application.IAppService;
using FS.VideoStreaming.Infrastructure.Config;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FS.VideoStreaming.Application.AppService
{
    public class FfmpegOperateAppService : IFfmpegOperateAppService
    {
        private readonly ILogger<FfmpegOperateAppService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly ICameraConfigAppService _cameraConfigAppService;

        public FfmpegOperateAppService(ILogger<FfmpegOperateAppService> logger, IMemoryCache memoryCache, ICameraConfigAppService cameraConfigAppService)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _cameraConfigAppService = cameraConfigAppService;
        }

        public bool IsProcessRunning(int pid)
        {
            var ffmpegs = Process.GetProcessesByName(SystemConstant.FfmpegProcessName);
            if (ffmpegs == null || ffmpegs.Count() <= 0)
            {
                _logger.LogInformation($"检测已经启动的进程{pid}未存活！");
                return false;
            }
            foreach (Process process in ffmpegs)
            {
                if (process.Id == pid)
                {
                    return true;
                }
            }
            _logger.LogInformation($"Process {pid}不在进程列表中！");
            return false;
        }

        public bool IsDirectoryFilesExists(CameraConfigBaseDto item)
        {
            var generatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SystemConstant.Nginx, SystemConstant.Html, item.Name);
            if (Directory.Exists(generatePath))
            {
                string[] files = Directory.GetFiles(generatePath);
                // 预留五分钟左右时间 用于ffmpeg 程序内部处理耗时
                // 影响文件读写进度
                if (item.CreateDate.AddMinutes(5) < DateTime.Now)
                {
                    // 检测是否存在缓存文件
                    if (files.Length <= 0)
                    {
                        _logger.LogError($"缓存中存在执行的进程，但是没有写入缓存文件；地址：{generatePath}");
                        return false;
                    }
                    else
                    {
                        // 遍历文件 检查是否存在长时间不更新的文件
                        foreach (string file in files)
                        {
                            var lastWriteTime = File.GetLastWriteTime(file);
                            // 计算时间差
                            TimeSpan timeDifference = DateTime.Now - lastWriteTime;
                            if (timeDifference.TotalMinutes < 10)
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    return true;
                }
            }
            _logger.LogError($"缓存文件超过30分钟不更新；地址：{generatePath}");
            return false;
        }


        public bool IsValidate(out List<CameraConfigBaseDto> addConfigBaseDtos, out List<CameraConfigBaseDto> deleteConfigBaseDtos)
        {
            var flag = false;
            // 添加的项
            addConfigBaseDtos = new List<CameraConfigBaseDto>();
            // 移除的项
            deleteConfigBaseDtos = new List<CameraConfigBaseDto>();
            // 缓存字典
            var dicCache = _memoryCache.Get<Dictionary<int, CameraConfigBaseDto>>(SystemConstant.CameraCacheKey);
            if (dicCache != null)
            {
                var cameraConfigDto = _cameraConfigAppService.GetCameraConfigs();
                // 检测缓存中的摄像头是否在文件中都存在
                foreach (var item in dicCache.Values)
                {
                    var result = cameraConfigDto?.RtspAddresses?.FirstOrDefault(t => t.Name == item.Name && t.Url == item.Url);
                    if (result == null)
                    {
                        deleteConfigBaseDtos.Add(item);
                        flag = true;
                        continue;
                    }
                    if (!IsProcessRunning(item.ProcessId))
                    {
                        deleteConfigBaseDtos.Add(item);
                        flag = true;
                    }

                    if (!IsDirectoryFilesExists(item))
                    {
                        deleteConfigBaseDtos.Add(item);
                        flag = true;
                    }
                }

                if (cameraConfigDto != null && cameraConfigDto.RtspAddresses != null && cameraConfigDto.RtspAddresses.Count > 0)
                {
                    foreach (var item in cameraConfigDto.RtspAddresses)
                    {
                        var result = dicCache.Values.FirstOrDefault(t => t.Name == item.Name && t.Url == item.Url);
                        if (result == null)
                        {
                            addConfigBaseDtos.Add(item);
                            flag = true;
                        }
                    }
                }
            }
            else
            {
                _logger.LogInformation($"检测当时摄像头缓存不存在！");
            }
            return flag;
        }

        public void KillProcess(int pid = -1)
        {
            var processes = Process.GetProcessesByName(SystemConstant.FfmpegProcessName);
            if (processes != null && processes.Count() > 0)
            {
                if (pid > 0)
                {
                    foreach (var process in processes)
                    {
                        if (process.Id == pid)
                        {
                            process.Kill();
                            _logger.LogInformation($"ffmpeg删除指定进程：{process.Id}被杀掉！");
                            return;
                        }
                    }
                }
                else
                {
                    foreach (var process in processes)
                    {
                        process.Kill();
                        _logger.LogInformation($"ffmpeg删除所有进程：{process.Id}被杀掉！");
                    }
                }
            }
            else
            {
                _logger.LogInformation($"没有需要查杀的ffmpeg进程！");
            }
        }

        /// <summary>
        /// 设置摄像头缓存
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="dto"></param>
        /// <param name="isDelete">添加 or 删除</param>
        /// <returns></returns>
        public bool SetCameraCache(int pid, CameraConfigBaseDto dto, bool isDelete)
        {
            if (isDelete)
            {
                var dicCache = _memoryCache.Get<Dictionary<int, CameraConfigBaseDto>>(SystemConstant.CameraCacheKey);
                if (dicCache == null)
                {
                    _logger.LogInformation($"摄像头缓存信息删除失败!不存在进程：{pid}；摄像头信息：address:{dto.Url}；名称：{dto.Name}");
                    return false;
                }
                if (dicCache.TryGetValue(pid, out _))
                {
                    dicCache.Remove(pid);
                    try
                    {
                        var generatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SystemConstant.Nginx, SystemConstant.Html, dto.Name);
                        if (Directory.Exists(generatePath))
                        {
                            DirectoryInfo directoryInfo = new DirectoryInfo(generatePath);
                            var files = directoryInfo.EnumerateFiles();
                            if (files != null && files.Count() > 0)
                            {
                                foreach (var item in files)
                                {
                                    item.Delete();
                                    _logger.LogInformation($"删除指定文件：{item.Name}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"删除文件异常：{ex.Message}");
                    }

                    return true;
                }
                else
                {
                    _logger.LogInformation($"摄像头缓存信息删除失败!不存在进程：{pid}；摄像头信息：address:{dto.Url}；名称：{dto.Name}");
                    return false;
                }
            }
            else
            {
                dto.CreateDate = DateTime.Now;
                var dicCache = _memoryCache.Get<Dictionary<int, CameraConfigBaseDto>>(SystemConstant.CameraCacheKey);
                if (dicCache == null)
                {
                    dicCache = new Dictionary<int, CameraConfigBaseDto>();
                    dicCache[pid] = dto;
                    _memoryCache.Set(SystemConstant.CameraCacheKey, dicCache);
                }
                else
                {
                    dicCache[pid] = dto;
                }
                _logger.LogInformation($"摄像头缓存信息保存成功!进程：{pid}；摄像头信息：address:{dto.Url}；名称：{dto.Name}");
                return true;
            }
        }

        public int Start(CameraConfigBaseDto item)
        {
            if (item == null)
            {
                _logger.LogError($"进程启动失败；摄像头名称：{item.Name}；摄像头地址：{item.Url}。");
                return -1;
            }
            string M3u8FileName = string.Concat(item.Name, SystemConstant.CameraFormat);
            var generatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SystemConstant.Nginx, SystemConstant.Html, item.Name);
            string RtspPath = item.Url;
            if (!Directory.Exists(generatePath))
            {
                Directory.CreateDirectory(generatePath);
            }
            //_logger.LogInformation($"ffmpeg进程启动成功；进程ID：{Processid}，摄像头名称：{item.Name}；摄像头地址：{item.Url}");

            Process process = null;
            int Processid = 0;
            try
            {
                var startInfo = new ProcessStartInfo();
                startInfo.FileName = "lib\\ffmpeg.exe";

                //startInfo.Arguments = " -f rtsp -rtsp_transport tcp -i " + RtspPath + " -fflags flush_packets -max_delay 1 -flags -global_header -hls_time 10 -hls_list_size 10 -hls_flags 10 -c:v libx264 -c:a aac -b 1024k -y  ";

                startInfo.Arguments = " -f rtsp -rtsp_transport tcp -i " + RtspPath + " -fflags flush_packets -max_delay 1 -flags -global_header -hls_time 30 -hls_list_size 10 -hls_flags 10 -c:v libx264 -c:a aac -b:v 1024k -y  ";
                startInfo.Arguments += (generatePath + "\\" + M3u8FileName);

                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.Verb = "RunAs";//以管理员身份运行

                process = Process.Start(startInfo);
                process.ErrorDataReceived += ErrorDataReceived;
                Processid = process?.Id ?? Processid;//进出ID
                item.ProcessId = Processid;
                _logger.LogInformation($"ffmpeg进程启动成功；进程ID：{Processid}，摄像头名称：{item.Name}；摄像头地址：{item.Url}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ffmpeg进程启动异常；摄像头名称：{item.Name}；摄像头地址：{item.Url}；异常信息：{ex.Message}");
                process?.Close();//?的作用为:如果进出为空就不执行.如果进行不为空就执行
            }
            return Processid;
        }

        /// <summary>
        /// 订阅异常事件
        /// </summary>
        /// <param name="sender">对象</param>
        /// <param name="e">参数</param>
        /// <exception cref="NotImplementedException"></exception>
        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e == null)
            {
                _logger.LogError($"ffmpeg进程异常接收参数是空！");
            }
            if (!string.IsNullOrEmpty(e.Data))
            {
                _logger.LogInformation($"ffmpeg进程接收异常数据：{e.Data}");
            }
        }


        public void Run()
        {
            try
            {
                this.KillProcess();
                var configDto = _cameraConfigAppService.GetCameraConfigs();
                if (configDto == null || configDto.RtspAddresses == null || configDto.RtspAddresses.Count <= 0)
                {
                    _logger.LogInformation($"摄像头配置信息不存在；请配置摄像头rtsp协议及摄像头名称！");
                    return;
                }
                // 检测摄像头地址是否合法
                var urls = configDto.RtspAddresses.Select(t => t.Url).ToList();
                if (!_cameraConfigAppService.IsValidRtspAddress(urls))
                {
                    _logger.LogError($"存在不合法的摄像头配置信息；请检查配置文件，确认是正确的rtsp协议！");
                }
                foreach (var item in configDto.RtspAddresses)
                {
                    var pid = this.Start(item);
                    if (pid <= 0)
                    {
                        _logger.LogError($"进程运行失败，摄像头名称：{item.Name}；摄像头地址：{item.Url}");
                    }
                    else
                    {
                        SetCameraCache(pid, item, false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to run {ex.Message}");
            }

        }

    }
}
