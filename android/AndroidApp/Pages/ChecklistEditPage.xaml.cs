using AndroidApp.PageModels;

namespace AndroidApp.Pages;

[QueryProperty(nameof(ChecklistId), "id")]
public partial class ChecklistEditPage : ContentPage
{
    private readonly ChecklistEditPageModel _model = new();

    public string? ChecklistId { get; set; }

    public ChecklistEditPage()
    {
        InitializeComponent();
        BindingContext = _model;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _model.LoadOrCreate(ChecklistId);
    }
}