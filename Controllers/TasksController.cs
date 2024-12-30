// Controllers/TasksController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TaskManagementApi.Data;
using TaskManagementApi.Models;

namespace YourProjectName.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/tasks")]
    public class TasksController : ControllerBase
    {
        private readonly UserManager<UserModel> _userManager;
        private readonly DataContext _context;

        public TasksController(UserManager<UserModel> userManager, DataContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: api/tasks
        [HttpGet]
        public async Task<IActionResult> GetTask()
        {
            var userId = _userManager.GetUserId(User);
            var task = await _context.Tasks
                .Where(t => t.UserId == userId || User.IsInRole("Admin"))
                .Select(t => new { t.ExternalId, t.Title, t.Description, t.CreatedAt, t.UserId, t.IsActive  }) // Select only the desired properties
                .ToListAsync();

            if (task == null)
            {
                return NotFound();
            }

            return Ok(task);
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin")] // Only allow admins to fetch all users
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            return Ok(users);
        }


        [HttpPut("{taskId}/user/{userId}")]
        [Authorize(Roles = "Admin")] // Only allow admins to update the associated user of a task
        public async Task<IActionResult> UpdateTaskUser(Guid taskId, string userId)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.ExternalId == taskId);
            if (task == null)
            {
                return NotFound();
            }

            task.UserId = userId!;

            _context.Entry(task).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }


        // GET: api/tasks/1
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")] // Only allow admins to get a specific task
        public async Task<IActionResult> GetTask(Guid externalId)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.ExternalId == externalId);

            if (task == null)
            {
                return NotFound();
            }

            return Ok(task);
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] Task task)
        {
            var userId = _userManager.GetUserId(User);

            if (task != null)
            {
                task.UserId = userId!;
                task.ExternalId = Guid.NewGuid();
            }
            else
            {
                return BadRequest();
            }

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTask), new { id = task.ExternalId }, task);
        }

        // PUT: api/tasks/1
        [HttpPut("{externalId}")]
        public async Task<IActionResult> UpdateTask(Guid externalId, [FromBody] Task task)
        {
            if (externalId != task.ExternalId)
            {
                return BadRequest();
            }

            var userId = _userManager.GetUserId(User);
            if (!_context.Tasks.Any(t => t.ExternalId == externalId && (t.UserId == userId || User.IsInRole("Admin"))))
            {
                return NotFound();
            }

            var existingTask = await _context.Tasks.FirstOrDefaultAsync(t => t.ExternalId == externalId);
            if (existingTask == null)
            {
                return NotFound();
            }

            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.IsActive = task.IsActive;
            existingTask.CreatedAt = task.CreatedAt;

            _context.Entry(existingTask).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/tasks/1
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.ExternalId == id && (t.UserId == userId || User.IsInRole("Admin")));

            if (task == null)
            {
                return NotFound();
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
