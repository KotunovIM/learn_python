using AndroidApp.Pages;

namespace AndroidApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("checklist",      typeof(ChecklistDetailPage));
        Routing.RegisterRoute("checklist-edit", typeof(ChecklistEditPage));
    }
}