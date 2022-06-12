namespace WebApp.Controllers
{
    using System;
    using Cw.Sig;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [ApiController]
    [Route("")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Snowflake19IdGenerator _idGenerator;

        public HomeController(ILogger<HomeController> logger, Snowflake19IdGenerator idGenerator)
        {
            _logger = logger;
            _idGenerator = idGenerator;
        }

        [HttpGet]
        public IActionResult Get()
        {
            for (int i = 0; i < 20; i++)
            {
                _logger.LogInformation($"====begin=={i}====");
                var id = _idGenerator.NextId();
                _logger.LogInformation(id.ToString());
                var (timestamp, workerId, seq) = Snowflake19IdGenerator.Parse(id);
                var dt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).AddHours(8);
                _logger.LogInformation($"{dt.ToString("yyyy-MM-dd HH:mm:ss.fff")}\t{workerId}\t{seq}");
                _logger.LogInformation($"====end=={i}====");
            }


            return Ok("ok");
        }
    }


}
