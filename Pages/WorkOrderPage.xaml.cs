using NKKSeatBangpoo.Models;
using NKKSeatBangpoo.Services;

namespace NKKSeatBangpoo.Pages;

public partial class WorkOrderPage : ContentPage
{
    private readonly DatabaseService _dbService = new(AppConfig.ConnectionString);
    private readonly ExcelImportService _excelService = new();
    private readonly ToyotaImportService _toyotaService = new();
    private List<OrderHeaderVM> _allOrders = new();
    
    private string? _selectedFilePath;
    private bool _isToyotaFile;

    public WorkOrderPage()
    {
        InitializeComponent();
    }

    private async void OnPageLoaded(object sender, EventArgs e)
    {
        await LoadOrders();
    }

    private async Task LoadOrders()
    {
        try
        {
            var orders = await _dbService.GetWorkOrdersAsync();
            _allOrders = orders.Select(o => new OrderHeaderVM(o)).ToList();
            OrderList.ItemsSource = _allOrders;
        }
        catch (Exception ex)
        {
            await DisplayAlert("ข้อผิดพลาด", ex.Message, "ตกลง");
        }
    }

    private void OnFilterClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        string filter = btn.CommandParameter?.ToString() ?? "All";

        // Reset button styles
        foreach (var b in new[] { BtnAll, BtnOpen, BtnInProgress, BtnClosed })
        {
            b.BackgroundColor = Color.FromArgb("#1C1C4E");
            b.TextColor = Color.FromArgb("#8888CC");
        }
        btn.BackgroundColor = Color.FromArgb("#3D3D9E");
        btn.TextColor = Colors.White;

        var filtered = filter == "All"
            ? _allOrders
            : _allOrders.Where(o => o.Status == filter).ToList();

        OrderList.ItemsSource = filtered;
    }

    private async void OnOrderSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not OrderHeaderVM selected) return;

        LblDetailHeader.Text = $"📋 {selected.OrderNo}";
        LblDetailHeader.TextColor = Colors.White;

        LblWC.Text = selected.WorkCenter;
        LblDue.Text = selected.DueDateStr;
        PanelSummary.IsVisible = true;

        try
        {
            var details = await _dbService.GetWorkOrderDetailsAsync(selected.OrderNo);
            var detailVMs = details.Select(d => new OrderDetailVM(d)).ToList();
            DetailList.ItemsSource = detailVMs;

            int totalTarget = (int) detailVMs.Sum(d => d.TargetQty);
            int totalRegistered = (int) detailVMs.Sum(d => d.RegisteredQty);
            LblProgress.Text = $"{totalRegistered} / {totalTarget} ชิ้น";
            LblProgress.TextColor = totalRegistered >= totalTarget
                ? Color.FromArgb("#FF8844")
                : Color.FromArgb("#44DD88");
        }
        catch (Exception ex)
        {
            await DisplayAlert("ข้อผิดพลาด", ex.Message, "ตกลง");
        }
    }

    // =====================================================================
    // 🚚 IMPORT POPUP LOGIC
    // =====================================================================

    private void OnShowImportPopupClicked(object sender, EventArgs e)
    {
        _selectedFilePath = null;
        _isToyotaFile = false;
        LblSelectedFileName.Text = "ยังไม่ได้เลือกไฟล์";
        BtnConfirmImport.IsEnabled = false;
        ImportStatusLayout.IsVisible = false;
        ImportPopup.IsVisible = true;
    }

    private async void OnBrowseIsuzuClicked(object sender, EventArgs e)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "เลือกไฟล์ ISUZU Work Order (.xlsx)",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".xlsx" } }
            })
        });

        if (result != null)
        {
            _selectedFilePath = result.FullPath;
            _isToyotaFile = false;
            LblSelectedFileName.Text = $"[ISUZU] {result.FileName}";
            BtnConfirmImport.IsEnabled = true;
        }
    }

    private async void OnBrowseToyotaClicked(object sender, EventArgs e)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "เลือกไฟล์ TOYOTA Work Order (.txt)",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".txt" } }
            })
        });

        if (result != null)
        {
            _selectedFilePath = result.FullPath;
            _isToyotaFile = true;
            LblSelectedFileName.Text = $"[TOYOTA] {result.FileName}";
            BtnConfirmImport.IsEnabled = true;
        }
    }

    private async void OnConfirmImportClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedFilePath)) return;

        ImportStatusLayout.IsVisible = true;
        BtnConfirmImport.IsEnabled = false;
        LblPopupImportStatus.Text = "กำลังนำเข้าข้อมูล...";

        try
        {
            WorkOrderHeader header;
            List<WorkOrderDetail> details;

            if (_isToyotaFile)
            {
                LblPopupImportStatus.Text = "กำลังอ่านข้อมูล TOYOTA (Text)...";
                (header, details) = await Task.Run(() => _toyotaService.ReadToyotaWorkOrder(_selectedFilePath));
            }
            else
            {
                LblPopupImportStatus.Text = "กำลังอ่านข้อมูล ISUZU (Excel)...";
                (header, details) = await Task.Run(() => _excelService.ReadWorkOrder(_selectedFilePath));
            }

            await _dbService.SaveWorkOrderHeaderAsync(header);
            int saved = await _dbService.SaveWorkOrderDetailsAsync(header.OrderNo, details, _isToyotaFile);

            await DisplayAlert("นำเข้าสำเร็จ", $"✅ บันทึก Work Order: {header.OrderNo} สำเร็จภ ({saved} รายการ)", "ตกลง");
            
            ImportPopup.IsVisible = false;
            await LoadOrders(); // Refresh list
        }
        catch (Exception ex)
        {
            await DisplayAlert("ข้อผิดพลาด", ex.Message, "ตกลง");
            ImportStatusLayout.IsVisible = false;
            BtnConfirmImport.IsEnabled = true;
        }
    }

    private void OnCloseImportPopupClicked(object sender, EventArgs e)
    {
        ImportPopup.IsVisible = false;
    }
}
