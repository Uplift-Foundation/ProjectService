using System.Security.Claims;
using ProjectService.Data;
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

        public ProjectsController(ProjectContext context)
        {
            _context = context;
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
