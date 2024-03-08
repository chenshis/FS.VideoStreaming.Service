﻿using FS.VideoStreaming.Application.Dto;
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
                _logger.LogInformation($"Process {pid}不存在！");
                return true;
            }
            foreach (Process process in ffmpegs)
            {
                if (process.Id == pid)
                {
                    return true;
                }
            }
            _logger.LogInformation($"Process {pid}不存在！");
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
                _logger.LogInformation($"检测当前ffmpeg进程不存在！");
            }
        }

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

            Process process = null;
            int Processid = 0;
            try
            {
                var startInfo = new ProcessStartInfo();
                startInfo.FileName = "lib\\ffmpeg.exe";

                startInfo.Arguments = " -f rtsp -rtsp_transport tcp -i " + RtspPath + " -fflags flush_packets -max_delay 1 -an -flags -global_header -hls_time 10 -hls_list_size 10 -hls_flags 10 -vcodec copy -s 216x384 -b 1024k -y  ";
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
