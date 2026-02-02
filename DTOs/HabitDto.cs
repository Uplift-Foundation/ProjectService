namespace ProjectService.DTOs;

public enum FrequencyToggleStatus
{
    Weekly,
    Monthly
}

public enum IsActiveStatus
{
    Active,
    Inactive
}

public class HabitDto
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Repetitions { get; set; }
    public int CountDaysUserCompleted { get; set; }
    public IsActiveStatus IsActive { get; set; }
    public DateTime StartDate { get; set; }
    public string SelectedColorHexCode { get; set; } = string.Empty;
    public FrequencyToggleStatus FrequencyToggle { get; set; }
    public List<string> SelectedDays { get; set; } = new();
    public int? ProjectId { get; set; }
}
