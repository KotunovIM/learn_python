using AndroidApp.Models;
using AndroidApp.PageModels;

namespace AndroidApp.Pages;

[QueryProperty(nameof(ChecklistId), "id")]
public partial class ChecklistDetailPage : ContentPage
{
    private readonly ChecklistDetailPageModel _model = new();

    public string? ChecklistId { get; set; }

    public ChecklistDetailPage()
    {
        InitializeComponent();
        BindingContext = _model;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!string.IsNullOrEmpty(ChecklistId))
            _model.LoadChecklist(ChecklistId);
    }

    /// <summary>
    /// При закрытии карточки сбрасываем визуальные галочки —
    /// при следующем открытии шаблон выглядит «чистым».
    /// В архив уже всё записано, сам шаблон (Items) не менялся.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_model.Checklist is not null)
        {
            foreach (var item in _model.Checklist.Items)
                item.IsDone = false;
        }
    }

    /// <summary>
    /// Тап по пункту: ставим ✓ + strike-through, пишем в архив.
    /// Галочка НЕ сбрасывается — пользователь видит, что уже отмечено,
    /// пока карточка открыта.
    /// </summary>
    private async void OnItemTapped(object sender, TappedEventArgs e)
    {
        if (sender is not Grid grid || grid.BindingContext is not ChecklistItem item)
            return;

        if (item.IsDone) return;      // повторный тап по уже отмеченному игнорируем

        item.IsDone = true;           // ✓ + strike-through остаётся до OnDisappearing
        await _model.ArchiveItemAsync(item);
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (_model.Checklist is null) return;
        await Shell.Current.GoToAsync($"checklist-edit?id={_model.Checklist.Id}");
    }
}