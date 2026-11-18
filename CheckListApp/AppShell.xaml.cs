using CheckListApp.Pages;

namespace CheckListApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // ── Маршруты CheckList ──
        Routing.RegisterRoute("checklist",      typeof(ChecklistDetailPage));
        Routing.RegisterRoute("checklist-edit", typeof(ChecklistEditPage));
    }
}