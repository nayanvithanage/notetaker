namespace Notetaker.Api.DTOs;

public class AutomationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ExampleText { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateAutomationDto
{
    public string Name { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ExampleText { get; set; }
    public bool Enabled { get; set; } = true;
}

public class UpdateAutomationDto
{
    public string Name { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ExampleText { get; set; }
    public bool Enabled { get; set; }
}


