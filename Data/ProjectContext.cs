using Microsoft.EntityFrameworkCore;

namespace ProjectService.Data;

public class ProjectContext : DbContext
{
    public ProjectContext(DbContextOptions<ProjectContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>().ToTable("Projects");
    }

    public DbSet<Project> Projects { get; set; }
}
