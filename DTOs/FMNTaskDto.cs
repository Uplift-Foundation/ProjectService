namespace ProjectService.DTOs;

public enum IsPriorityStatus
{
    None,
    Low,
    Med,
    High
}

public enum IsTNC
{
    onTNC,
    offTNC
}

public class FMNTaskDto
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Repetitions { get; set; }
    public int CountDaysUserCompleted { get; set; }
    public IsActiveStatus IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public string SelectedColorHexCode { get; set; } = string.Empty;
    public IsPriorityStatus PriorityStatus { get; set; }
    public IsTNC TNC { get; set; }
    public int? ProjectId { get; set; }
}
