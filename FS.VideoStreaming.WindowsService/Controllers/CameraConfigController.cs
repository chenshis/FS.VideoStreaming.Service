using FS.VideoStreaming.Application.Dto;
using FS.VideoStreaming.Application.IAppService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FS.VideoStreaming.WindowsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CameraConfigController : ControllerBase
    {
        private readonly ILogger<CameraConfigController> _logger;
        private readonly ICameraConfigAppService _cameraConfigAppService;

        public CameraConfigController(ILogger<CameraConfigController> logger, ICameraConfigAppService cameraConfigAppService)
        {
            _logger = logger;
            _cameraConfigAppService = cameraConfigAppService;
        }

        /// <summary>
        /// 获取所有摄像头
        /// </summary>
        /// <returns></returns>
        [HttpGet(Name = "cameraconfigs")]
        public CameraConfigDto Get()
        {
            var cameraConfigs = _cameraConfigAppService.GetCameraConfigs();
            _logger.LogInformation($"读取摄像头配置信息：{JsonConvert.SerializeObject(cameraConfigs)}");
            return cameraConfigs;
        }

        /// <summary>
        /// 保存摄像头配置信息
        /// </summary>
        /// <param name="cameraConfigs"></param>
        /// <returns></returns>
        [HttpPost(Name = "cameraconfigs")]
        public bool Save([FromBody] CameraConfigDto cameraConfigs)
        {
            _logger.LogInformation($"调用接口保存摄像头配置信息：{JsonConvert.SerializeObject(cameraConfigs)}");
            var result = _cameraConfigAppService.Save(cameraConfigs);
            return result;
        }

        /// <summary>
        /// 删除摄像头配置（可能不会立刻移除进程，需要等待下次轮询服务时移除进程 最多间隔一分钟左右）
        /// </summary>
        /// <param name="cameraConfig">摄像头对象</param>
        /// <returns></returns>
        [HttpPost("deleteconfig")]
        public bool Delete([FromBody] CameraConfigDeleteDto cameraConfig)
        {
            if (cameraConfig == null || string.IsNullOrWhiteSpace(cameraConfig.RtspUrl))
            {
                _logger.LogInformation($"移除摄像头失败摄像头地址不能为空");
                return false;
            }
            _logger.LogInformation($"调用接口移除摄像头地址：{cameraConfig.RtspUrl}");
            return _cameraConfigAppService.DeleteRtspAddress(cameraConfig.RtspUrl);
        }
    }
}
