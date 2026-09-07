namespace NKKSeatBangpoo.Pages;

public partial class MainMenuPage : ContentPage
{
    public MainMenuPage()
    {
        InitializeComponent();
    }

    private async void OnProductMasterClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(ProductMasterPage));

    private async void OnWorkOrderClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(WorkOrderPage));

    private async void OnRegisterClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(RegisterPage));

    private async void OnTagManagementClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(TagManagementPage));

    private async void OnCardMasterClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(CardMasterPage));

    private async void OnGateEntryClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(GateEntryPage));

    private async void OnInventoryStockClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(InventoryStockPage));

    private async void OnDashboardClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(DashboardPage));
}
