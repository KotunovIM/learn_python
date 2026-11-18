using AndroidApp.PageModels;

namespace AndroidApp.Pages;

public partial class ArchivePage : ContentPage
{
    private readonly ArchivePageModel _model = new();

    public ArchivePage()
    {
        InitializeComponent();
        BindingContext = _model;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _model.InitAsync();
            _model.RefreshView();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка инициализации", ex.Message, "OK");
        }
    }
}