using AndroidApp.Pages;   // ← вот так не должно быть

namespace AndroidApp;       // ← второй namespace — тоже ошибка

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("checklist",      typeof(ChecklistDetailPage));
        Routing.RegisterRoute("checklist-edit", typeof(ChecklistEditPage));
    }
}