using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Models;
using API.Shared.Data;

namespace API.v2023_01_01.Controllers
{
    [Route("api/user-activity-logs")]
    [ApiController]
    public class UserActivityLogsController(ApplicationDbContext context) : ControllerBase
    {
        // GET: api/UserActivityLogs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserActivityLog>>> GetUserActivityLogs()
        {
            return await context.UserActivityLogs.ToListAsync();
        }
    }
}
