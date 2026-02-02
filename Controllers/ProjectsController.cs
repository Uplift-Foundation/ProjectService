using System.Security.Claims;
using ProjectService.Data;
using ProjectService.DTOs;
using ProjectService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Asp.Versioning;

namespace ProjectService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly ProjectContext _context;
        private readonly IHabitServiceClient _habitServiceClient;
        private readonly ITaskServiceClient _taskServiceClient;

        public ProjectsController(
            ProjectContext context,
            IHabitServiceClient habitServiceClient,
            ITaskServiceClient taskServiceClient)
        {
            _context = context;
            _habitServiceClient = habitServiceClient;
            _taskServiceClient = taskServiceClient;
        }

        // POST: api/Projects
        [HttpPost]
        public async Task<ActionResult<Project>> PostProject(Project project)
        {
            try
            {
                project.UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                project.CreatedDate = DateTime.UtcNow;
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetProject", new { id = project.Id }, project);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine(ex.InnerException.Message);

                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Projects
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return await _context.Projects.Where(p => p.UserId == userId).ToListAsync();
        }

        // GET: api/Projects/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetProject(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (project == null)
            {
                return NotFound();
            }

            return project;
        }

        // GET: api/Projects/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<Project>>> GetActiveProjects()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return await _context.Projects
                .Where(p => p.UserId == userId && p.Status == ProjectStatus.Active)
                .ToListAsync();
        }

        // GET: api/Projects/5/with-items
        [HttpGet("{id}/with-items")]
        public async Task<ActionResult<ProjectWithItemsDto>> GetProjectWithItems(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (project == null)
            {
                return NotFound();
            }

            // Get auth token from current request
            var authToken = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            // Fetch habits and tasks for this project
            var habits = await _habitServiceClient.GetHabitsByProjectIdAsync(id, authToken);
            var tasks = await _taskServiceClient.GetTasksByProjectIdAsync(id, authToken);

            var result = new ProjectWithItemsDto
            {
                Id = project.Id,
                UserId = project.UserId,
                Name = project.Name,
                Description = project.Description,
                SelectedColorHexCode = project.SelectedColorHexCode,
                Status = project.Status,
                CreatedDate = project.CreatedDate,
                CompletedDate = project.CompletedDate,
                Habits = habits,
                Tasks = tasks
            };

            return result;
        }

        // GET: api/Projects/with-items
        [HttpGet("with-items")]
        public async Task<ActionResult<IEnumerable<ProjectWithItemsDto>>> GetProjectsWithItems()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var projects = await _context.Projects.Where(p => p.UserId == userId).ToListAsync();

            // Get auth token from current request
            var authToken = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            // Fetch all habits and tasks for this user
            var allHabits = await _habitServiceClient.GetHabitsByUserAsync(authToken);
            var allTasks = await _taskServiceClient.GetTasksByUserAsync(authToken);

            var result = projects.Select(project => new ProjectWithItemsDto
            {
                Id = project.Id,
                UserId = project.UserId,
                Name = project.Name,
                Description = project.Description,
                SelectedColorHexCode = project.SelectedColorHexCode,
                Status = project.Status,
                CreatedDate = project.CreatedDate,
                CompletedDate = project.CompletedDate,
                Habits = allHabits.Where(h => h.ProjectId == project.Id).ToList(),
                Tasks = allTasks.Where(t => t.ProjectId == project.Id).ToList()
            }).ToList();

            return result;
        }

        // PUT: api/Projects/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProject(int id, Project project)
        {
            // 1. Retrieve the authenticated user's ID from the JWT token.
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 2. Verify that the Project ID provided in the URL matches the Project object's ID from the request body.
            if (id != project.Id)
            {
                return BadRequest("The provided ID in the URL does not match the ID of the Project object.");
            }

            // 3. Fetch the Project from the database using the provided ID.
            var existingProject = await _context.Projects.FindAsync(id);

            // 4. Verify that the fetched Project exists and belongs to the authenticated user.
            if (existingProject == null || existingProject.UserId != userId)
            {
                return NotFound("The Project does not exist or does not belong to the authenticated user.");
            }

            // 5. Update each property of the Project with the corresponding property from the request body.
            existingProject.Name = project.Name;
            existingProject.Description = project.Description;
            existingProject.SelectedColorHexCode = project.SelectedColorHexCode;
            existingProject.Status = project.Status;

            // If status changed to Completed, set CompletedDate
            if (project.Status == ProjectStatus.Completed && existingProject.CompletedDate == null)
            {
                existingProject.CompletedDate = DateTime.UtcNow;
            }
            else if (project.Status != ProjectStatus.Completed)
            {
                existingProject.CompletedDate = null;
            }

            // 6. Mark the Project as modified in the database context.
            _context.Entry(existingProject).State = EntityState.Modified;

            // 7. Attempt to save the changes.
            try
            {
                await _context.SaveChangesAsync();
            }
            // 8. Handle potential concurrency issues.
            catch (DbUpdateConcurrencyException)
            {
                if (!await ProjectExists(id))
                {
                    return NotFound("The Project no longer exists in the database.");
                }
                else
                {
                    throw;
                }
            }

            // 9. Return the appropriate response.
            return NoContent();
        }

        // Utility method to check if a Project exists based on its ID.
        private async Task<bool> ProjectExists(int id)
        {
            return await _context.Projects.AnyAsync(p => p.Id == id);
        }

        // DELETE: api/Projects/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (project == null)
            {
                return NotFound();
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
