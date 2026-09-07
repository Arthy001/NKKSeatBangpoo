using System.Collections.ObjectModel;
using NKKSeatBangpoo.Models;
using NKKSeatBangpoo.Services;

namespace NKKSeatBangpoo.Pages;

public partial class TagManagementPage : ContentPage
{
    private readonly DatabaseService _dbService = new(AppConfig.ConnectionString);
    private List<TagBindingViewModel> _allTransactions = new();

    public TagManagementPage()
    {
        InitializeComponent();
    }

    private async void OnPageLoaded(object sender, EventArgs e)
    {
        await LoadData();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        try
        {
            var data = await _dbService.GetAllTagBindingTransactionsAsync();
            _allTransactions = data.Select(t => new TagBindingViewModel(t)).ToList();
            BindingList.ItemsSource = _allTransactions;

            LblTotal.Text = _allTransactions.Count.ToString();
            LblPending.Text = _allTransactions.Count(t => !(t.IsExported ?? false)).ToString();
            LblExported.Text = _allTransactions.Count(t => t.IsExported ?? false).ToString();
        }
        catch (Exception ex)
        {
            await DisplayAlert("ข้อผิดพลาด", ex.Message, "ตกลง");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        string keyword = (e.NewTextValue ?? string.Empty).Trim().ToLower();
        if (string.IsNullOrEmpty(keyword))
        {
            BindingList.ItemsSource = _allTransactions;
        }
        else
        {
            BindingList.ItemsSource = _allTransactions.Where(t =>
                t.TagEPC.ToLower().Contains(keyword) ||
                t.OrderNo.ToLower().Contains(keyword)
            ).ToList();
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedCount = e.CurrentSelection.Count;
        LblSelection.Text = $"เลือก {selectedCount} รายการ";
        BtnRecycle.IsEnabled = selectedCount > 0;
    }

    private async void OnRecycleClicked(object sender, EventArgs e)
    {
        var selected = BindingList.SelectedItems.Cast<TagBindingViewModel>().ToList();
        if (selected.Count == 0) return;

        bool confirm = await DisplayAlert("ยืนยันการคืนบัตร", 
            $"คุณต้องการคืนบัตร (Recycle) จำนวน {selected.Count} ใบ กลับเข้าคลังบัตรว่างสำหรับใช้งานใหม่หรือไม่?", 
            "ใช่ (Recycle)", "ไม่");

        if (confirm)
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            try
            {
                var ids = selected.Select(s => s.TransactionID).ToList();
                bool success = await _dbService.ClearTagSelectionAsync(ids);
                if (success)
                {
                    await DisplayAlert("สำเร็จ", $"คืนบัตร {selected.Count} ใบเรียบร้อยแล้ว", "ตกลง");
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("เกิดข้อผิดพลาด", ex.Message, "ตกลง");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }
    }
}

// =====================================================================
// ViewModel สำหรับแสดงผลหน้า Tag Management
// =====================================================================
public class TagBindingViewModel
{
    public int TransactionID { get; set; }
    public string TagEPC { get; set; } = string.Empty;
    public string OrderNo { get; set; } = string.Empty;
    public string NHKPartNo { get; set; } = string.Empty;
    public DateTime? BindingTime { get; set; } // Changed
    public bool? IsExported { get; set; }
    public bool IsSelected { get; set; }

    public string RegisterTimeStr => BindingTime?.ToString("dd/MM/yy HH:mm") ?? "-";
    public string StatusText => (IsExported ?? false) ? "📦 Exported" : "🏗️ Active";
    public Color StatusBgColor => (IsExported ?? false) ? Color.FromArgb("#1A4A2A") : Color.FromArgb("#2A2A6E");

    public TagBindingViewModel(TagBindingTransaction t)
    {
        TransactionID = t.TransactionID;
        TagEPC = t.TagEPC ?? "";
        OrderNo = t.OrderNo ?? "";
        NHKPartNo = t.NHKPartNo ?? "";
        BindingTime = t.BindingTime;
        IsExported = t.IsExported ?? false; // Force to bool value
    }
}
