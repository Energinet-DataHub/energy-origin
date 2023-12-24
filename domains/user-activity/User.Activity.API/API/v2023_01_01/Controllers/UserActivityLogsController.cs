using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Models;
using API.Shared.Data;

namespace API.v2023_01_01.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserActivityLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserActivityLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/UserActivityLogs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserActivityLog>>> GetUserActivityLogs()
        {
            return await _context.UserActivityLogs.ToListAsync();
        }

        // GET: api/UserActivityLogs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserActivityLog>> GetUserActivityLog(Guid id)
        {
            var userActivityLog = await _context.UserActivityLogs.FindAsync(id);

            if (userActivityLog == null)
            {
                return NotFound();
            }

            return userActivityLog;
        }
    }
}
