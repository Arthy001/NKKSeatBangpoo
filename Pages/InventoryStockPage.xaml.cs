using NKKSeatBangpoo.Models;
using NKKSeatBangpoo.Services;

namespace NKKSeatBangpoo.Pages;

public partial class InventoryStockPage : ContentPage
{
    private readonly DatabaseService _dbService = new(AppConfig.ConnectionString);
    private bool _isOverviewMode = true;

    public InventoryStockPage()
    {
        InitializeComponent();
    }

    private async void OnPageLoaded(object sender, EventArgs e)
    {
        await LoadDataAsync();
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        await LoadDataAsync(e.NewTextValue);
    }

    private async void OnOverviewClicked(object sender, EventArgs e)
    {
        _isOverviewMode = true;
        OverviewContainer.IsVisible = true;
        HistoryContainer.IsVisible = false;

        BtnOverview.BackgroundColor = Color.FromArgb("#4444AA");
        BtnOverview.TextColor = Colors.White;
        BtnHistory.BackgroundColor = Color.FromArgb("#2A2A4E");
        BtnHistory.TextColor = Color.FromArgb("#AAAAFF");

        await LoadDataAsync(SearchEntry.Text);
    }

    private async void OnHistoryClicked(object sender, EventArgs e)
    {
        _isOverviewMode = false;
        OverviewContainer.IsVisible = false;
        HistoryContainer.IsVisible = true;

        BtnHistory.BackgroundColor = Color.FromArgb("#4444AA");
        BtnHistory.TextColor = Colors.White;
        BtnOverview.BackgroundColor = Color.FromArgb("#2A2A4E");
        BtnOverview.TextColor = Color.FromArgb("#AAAAFF");

        await LoadDataAsync(SearchEntry.Text);
    }

    private async Task LoadDataAsync(string searchQuery = "")
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            if (_isOverviewMode)
            {
                var data = await _dbService.GetInventoryStockAsync(searchQuery);
                StockList.ItemsSource = data;
                LblTotalCount.Text = data.Sum(x => x.TotalQty).ToString("N0") + " pcs / " + data.Sum(x => x.TotalTags).ToString("N0") + " tags";
            }
            else
            {
                var historyData = await _dbService.GetInventoryHistoryAsync(searchQuery);
                HistoryList.ItemsSource = historyData;
                LblTotalCount.Text = historyData.Count.ToString("N0");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"ไม่สามารถโหลดข้อมูลได้: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        try
        {
            var dataCount = _isOverviewMode ? StockList.ItemsSource?.Cast<object>().Count() : HistoryList.ItemsSource?.Cast<object>().Count();
            
            if (dataCount == null || dataCount == 0)
            {
                await DisplayAlert("แจ้งเตือน", "ไม่มีข้อมูลสำหรับ Export", "OK");
                return;
            }

            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string projectFolder = Path.Combine(folderPath, "Project_NHK_Bangpoo");
            if (!Directory.Exists(projectFolder))
                Directory.CreateDirectory(projectFolder);

            string filePrefix = _isOverviewMode ? "InventoryStock" : "InventoryTraceability";
            string fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            string filePath = Path.Combine(projectFolder, fileName);
            
            using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                // Write BOM for Excel UTF-8 display
                writer.Write("\xFEFF");

                if (_isOverviewMode)
                {
                    writer.WriteLine("Order No,NHK Part No,Part Name,Total Qty,Tags,Last Entry Time");
                    foreach (InventoryStockItem item in StockList.ItemsSource)
                    {
                        // Escape quotes if PartName contains commas or quotes
                        string partName = item.PartName?.Replace("\"", "\"\"") ?? "";
                        string line = $"{item.OrderNo},{item.NHKPartNo},\"{partName}\",{item.TotalQty},{item.TotalTags},{item.LastGateEntryTime:yyyy-MM-dd HH:mm:ss}";
                        writer.WriteLine(line);
                    }
                }
                else
                {
                    writer.WriteLine("Card No,Tag EPC,Order No,NHK Part No,Qty,Gate Entry Time,Status");
                    foreach (InventoryHistoryItem item in HistoryList.ItemsSource)
                    {
                        string line = $"{item.CardNo},{item.TagEPC},{item.OrderNo},{item.NHKPartNo},{item.QtyPerSet},{item.GateEntryTime:yyyy-MM-dd HH:mm:ss},{item.StatusText}";
                        writer.WriteLine(line);
                    }
                }
            }
            
            await DisplayAlert("สำเร็จ", $"Export ไฟล์ CSV เรียบร้อยแล้ว:\n{filePath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("ข้อผิดพลาด", $"ไม่สามารถ Export ไฟล์ได้: {ex.Message}", "OK");
        }
    }
}
