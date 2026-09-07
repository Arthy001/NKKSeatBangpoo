using NKKSeatBangpoo.Models;
using NKKSeatBangpoo.Services;
using System.Collections.ObjectModel;

namespace NKKSeatBangpoo.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly DatabaseService _dbService = new(AppConfig.ConnectionString);
    public ObservableCollection<SeatTypeSummary> SeatTypeSummaries { get; set; } = new();

    public DashboardPage()
    {
        InitializeComponent();
        CompanySummaryList.ItemsSource = SeatTypeSummaries;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDashboardData();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadDashboardData();
    }

    private async Task LoadDashboardData()
    {
        LoadingOverlay.IsVisible = true;
        try
        {
            // 1. Fetch Overall Summary
            var summary = await _dbService.GetDailySummaryAsync();
            OverallTarget.Text = summary.TotalTargetToday.ToString("N0");
            OverallProduced.Text = summary.TotalProducedToday.ToString("N0");
            OverallRegistered.Text = summary.TotalRegisteredToday.ToString("N0");
            OverallInStock.Text = summary.TotalInStockToday.ToString("N0");
            OverallRemaining.Text = summary.TotalRemainingProduce.ToString("N0");
            OverallOrders.Text = summary.TotalOrdersToday.ToString("N0");

            // 2. Fetch Seat Type Summaries (ISUZU, TOYOTA, etc.)
            var seatData = await _dbService.GetSeatTypeSummariesAsync();
            SeatTypeSummaries.Clear();
            foreach (var s in seatData) 
            {
                SeatTypeSummaries.Add(s);
            }

            // 3. Fetch Hourly Data and Render Simple Chart
            var hourly = await _dbService.GetHourlyThroughputAsync();
            RenderHourlyChart(hourly);

            LblLastUpdate.Text = $"Last Updated: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private void RenderHourlyChart(List<HourlyThroughput> data)
    {
        ChartContainer.Children.Clear();
        if (data == null || data.Count == 0) return;

        int max = data.Count > 0 ? data.Select(x => x.Count).Max() : 0;
        if (max == 0) max = 1;

        // Display hours 08-20
        for (int h = 8; h <= 20; h++)
        {
            var hourData = data.FirstOrDefault(x => x.Hour == h);
            int count = hourData?.Count ?? 0;
            double heightPct = (double)count / max;

            var bar = new VerticalStackLayout
            {
                Spacing = 5,
                VerticalOptions = LayoutOptions.End,
                Children = {
                    new Label { 
                        Text = count > 0 ? count.ToString() : "", 
                        HorizontalOptions = LayoutOptions.Center, 
                        FontSize = 9, 
                        TextColor = Colors.White 
                    },
                    new Border {
                        BackgroundColor = Color.FromArgb("#4141FF"),
                        HeightRequest = Math.Max(2, heightPct * 120),
                        WidthRequest = 20,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(4,4,0,0) }
                    },
                    new Label { 
                        Text = $"{h:D2}", 
                        HorizontalOptions = LayoutOptions.Center, 
                        FontSize = 9, 
                        TextColor = Color.FromArgb("#8888BB") 
                    }
                }
            };

            ChartContainer.Children.Add(bar);
        }
    }
}
