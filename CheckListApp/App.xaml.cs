namespace CheckListApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        Storage.InitDb();
        MainPage = new NavigationPage(new Views.OperationsPage());
    }
}