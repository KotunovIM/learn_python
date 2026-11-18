using AndroidApp.Models;
using AndroidApp.PageModels;

namespace AndroidApp.Pages;

public partial class TemplatesPage : ContentPage
{
    private readonly TemplatesPageModel _model = new();

    public TemplatesPage()
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

    private async void OnChecklistSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Checklist checklist)
            return;

        await Shell.Current.GoToAsync($"checklist?id={checklist.Id}");
        ((CollectionView)sender).SelectedItem = null;
    }
}