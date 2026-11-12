namespace CheckListApp.Views;

public partial class LogPage : ContentPage
{
    public LogPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private void Refresh_Click(object sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        CvActions.ItemsSource = Storage.GetRecentActions(100);
        CvOps.ItemsSource = Storage.GetRecentOperations(50);
    }
}