using Microsoft.AspNetCore.Mvc;

namespace MonitoringSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssemblyAchievementController : ControllerBase
    {
        // GET: api/AssemblyAchievement
        [HttpGet]
        public IActionResult Get()
        {
            // Data mockup disesuaikan dengan gambar UI untuk keperluan testing di Unity
            var data = new
            {
                cu = new
                {
                    plan = 89,
                    actual = 67,
                    speed = 75,
                    totalPlan = 2405
                },
                cs = new
                {
                    plan = 89,
                    actual = 94,
                    speed = 106,
                    totalPlan = 1732
                }
            };

            return Ok(data);
        }
    }
}
