using Microsoft.AspNetCore.Mvc;
using WondeApiAggregator.Services;

namespace WondeApiAggregator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WondeController : ControllerBase
    {
        private readonly WondeService _service;

        public WondeController(WondeService service)
        {
            _service = service;
        }

        [HttpGet("aggregate")]
        public async Task<IActionResult> Aggregate()
        {
            var result = await _service.AggregateAsync();
            return new JsonResult(result);
        }
    }
}
