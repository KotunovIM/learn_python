using CheckApps.Models;
using CheckApps.PageModels;
namespace CheckApps.Pages;

public partial class TemplatesPage : ContentPage
{
    public TemplatesPage()
    {
        InitializeComponent();
		//BindingContext = new TemplatesPageModel();
		BindingContext = new CheckApps.PageModels.TemplatesPageModel();
	   // для теста
       // ChecklistsCollectionView.ItemsSource = new List<Checklist>
       // {
       //      new Checklist { Name = "Тест 1" },
       //     new Checklist { Name = "Тест 2" },
       //      new Checklist { Name = "Тест 8" }
       //};
    }
}