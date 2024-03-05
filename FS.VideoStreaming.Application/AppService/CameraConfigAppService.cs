using FS.VideoStreaming.Application.IAppService;
using FS.VideoStreaming.Infrastructure.Config;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace FS.VideoStreaming.Application.AppService
{
    public class CameraConfigAppService : ICameraConfigAppService
    {
        private readonly ILogger _logger;

        public CameraConfigAppService(ILogger<CameraConfigAppService> logger)
        {
            _logger = logger;
        }

        public bool Save(List<string> cameraAddresses)
        {
            if (cameraAddresses == null || cameraAddresses.Count <= 0)
            {
                _logger.LogError("保存摄像头配置信息失败：地址不存在！");
                return false;
            }
            if (!IsValidRtspAddress(cameraAddresses))
            {
                _logger.LogError("存在不合法的rtsp地址！");
                return false;
            }
            var cameraDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SystemConstant.CameraConfigPath);
            if (!Directory.Exists(cameraDir))
            {
                Directory.CreateDirectory(cameraDir);
            }
            var cameraFilePath = Path.Combine(cameraDir, SystemConstant.CameraConfigFileName);
            if (!File.Exists(cameraFilePath))
            {
                File.Create(cameraFilePath);
            }
            try
            {
                var cameraJson = JsonConvert.SerializeObject(cameraAddresses);
                File.WriteAllText(cameraFilePath, cameraJson);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"序列化摄像头地址异常：{ex.Message}");
            }
            return false;
        }



        #region 私有方法

        private bool IsValidRtspAddress(List<string> addresses)
        {
            Regex regex = new Regex(@"^rtsp:\/\/.+", RegexOptions.IgnoreCase);
            foreach (string address in addresses)
            {
                if (!regex.IsMatch(address))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion
    }
}
