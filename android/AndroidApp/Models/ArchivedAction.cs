using System.Text.Json.Serialization;
namespace AndroidApp.Models;

public class ArchivedAction
{
    public string   Id             { get; set; } = Guid.NewGuid().ToString();
    public string   ChecklistId    { get; set; } = string.Empty;
    public string   ChecklistName  { get; set; } = string.Empty;
    public string   Text           { get; set; } = string.Empty;
    public DateTime CompletedAt    { get; set; } = DateTime.Now;
    public string   AccentColorHex { get; set; } = "#512BD4";

    [JsonIgnore] public Color  AccentColor     => Color.FromArgb(AccentColorHex);
    [JsonIgnore] public string CompletedAtText => CompletedAt.ToString("dd MMM, HH:mm");
}