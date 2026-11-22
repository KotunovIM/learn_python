namespace CheckApps.Models;

public class Checklist
{
    public string Name { get; set; } = "";
    public Color AccentColor { get; set; } = Colors.Gray;
    public List<ChecklistItem> Items { get; set; } = new();
}