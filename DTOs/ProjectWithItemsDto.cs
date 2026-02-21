using ProjectService.Data;

namespace ProjectService.DTOs;

public class ProjectWithItemsDto
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SelectedColorHexCode { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public List<HabitDto> Habits { get; set; } = new();
    public List<FMNTaskDto> Tasks { get; set; } = new();
}
