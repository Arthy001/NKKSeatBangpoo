using NKKSeatBangpoo.Pages;

namespace NKKSeatBangpoo;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(ProductMasterPage), typeof(ProductMasterPage));
        Routing.RegisterRoute(nameof(WorkOrderPage),     typeof(WorkOrderPage));
        Routing.RegisterRoute(nameof(RegisterPage),      typeof(RegisterPage));
        Routing.RegisterRoute(nameof(TagManagementPage), typeof(TagManagementPage));
        Routing.RegisterRoute(nameof(CardMasterPage),    typeof(CardMasterPage));
        Routing.RegisterRoute(nameof(GateEntryPage),     typeof(GateEntryPage));
        Routing.RegisterRoute(nameof(InventoryStockPage),typeof(InventoryStockPage));
        Routing.RegisterRoute(nameof(DashboardPage),     typeof(DashboardPage));
    }
}
