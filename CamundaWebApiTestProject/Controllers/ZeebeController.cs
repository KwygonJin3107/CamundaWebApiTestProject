using CamundaWebApiTestProject.Services;
using Microsoft.AspNetCore.Mvc;

namespace CamundaWebApiTestProject.Controllers
{
    [Route("zeebe")]
    [ApiController]
    public class ZeebeController : ControllerBase
    {
        private readonly IZeebeService _zeebeService;

        public ZeebeController(IZeebeService zeebeService)
        {
            _zeebeService = zeebeService;
        }

        [Route("/status")]
        [HttpGet]
        public async Task<string> Get()
        {
            return (await _zeebeService.Status()).ToString();
        }

        [Route("/start")]
        [HttpGet]
        public async Task<string> StartWorkflowInstance()
        {
            string processId = "test-process";

            string instance = await _zeebeService.StartWorkflowInstance(processId);

            return instance;
        }
    }
}
