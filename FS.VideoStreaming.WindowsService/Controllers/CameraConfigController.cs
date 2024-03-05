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
        [HttpGet(Name = "GetCameraConfigs")]
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
        [HttpPost(Name = "CameraConfigs")]
        public bool Save([FromBody] CameraConfigDto cameraConfigs)
        {
            _logger.LogInformation($"调用接口保存摄像头配置信息：{JsonConvert.SerializeObject(cameraConfigs)}");
            var result = _cameraConfigAppService.Save(cameraConfigs);
            return result;
        }
    }
}
