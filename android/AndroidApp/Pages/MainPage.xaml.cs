using AndroidApp.Models;
using AndroidApp.PageModels;

namespace AndroidApp.Pages;

public partial class MainPage : ContentPage
{
    private readonly MainPageModel _model = new();

    public MainPage()
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

    private async void OnFilterClicked(object sender, EventArgs e)
    {
        var options = _model.FilterOptions.ToArray();
        var choice = await DisplayActionSheet("Режим", "Отмена", null, options);

        if (choice is not null && _model.FilterOptions.Contains(choice))
            _model.SelectedFilter = choice;
    }

    private async void OnChecklistSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Checklist checklist)
            return;

        await Shell.Current.GoToAsync($"checklist?id={checklist.Id}");
        ((CollectionView)sender).SelectedItem = null;
    }
}