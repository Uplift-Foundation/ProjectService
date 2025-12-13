using System.ComponentModel.DataAnnotations;

namespace ProjectService.Data;

public enum ProjectStatus
{
    Active,
    Archived,
    Completed
}

public class Project
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public required string SelectedColorHexCode { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedDate { get; set; }
}
