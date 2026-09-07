using System.Collections.ObjectModel;
using NKKSeatBangpoo.Models;
using NKKSeatBangpoo.Services;

namespace NKKSeatBangpoo.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly DatabaseService _dbService = new(AppConfig.ConnectionString);

    private List<OrderHeaderVM> _allOrders = new();
    private List<OrderDetailVM> _currentDetails = new();
    
    private OrderDetailVM? _selectedDetail;
    private int _selectedBatchQty = 0;

    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnPageLoaded(object sender, EventArgs e)
    {
        await LoadWorkOrders();
    }

    private async Task LoadWorkOrders()
    {
        try
        {
            var orders = await _dbService.GetWorkOrdersAsync("Open");
            _allOrders = orders.Select(o => new OrderHeaderVM(o)).ToList();
            OrderList.ItemsSource = _allOrders;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Load Orders Failed: {ex.Message}", "OK");
        }
    }

    // =====================================================================
    // Search & Selection
    // =====================================================================
    private void OnOrderSearchChanged(object sender, TextChangedEventArgs e)
    {
        string filter = (e.NewTextValue ?? "").Trim().ToLower();
        if (string.IsNullOrEmpty(filter))
        {
            OrderList.ItemsSource = _allOrders;
        }
        else
        {
            OrderList.ItemsSource = _allOrders.Where(o => 
                o.OrderNo.ToLower().Contains(filter) || 
                o.WorkCenter.ToLower().Contains(filter)).ToList();
        }
    }

    private async void OnOrderSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not OrderHeaderVM selected) return;

        try
        {
            LblDetailHeader.Text = $"📋 Order: {selected.OrderNo}";
            LblDetailSubHeader.Text = $"Work Center: {selected.WorkCenter}";

            var details = await _dbService.GetWorkOrderDetailsAsync(selected.OrderNo);
            _currentDetails = details.Select((d, index) => new OrderDetailVM(d) { RowIndex = index + 1 }).ToList();
            DetailList.ItemsSource = _currentDetails;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Load Details Failed: {ex.Message}", "OK");
        }
    }

    private void OnDetailSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not OrderDetailVM selected) return;
        
        // Open Popup
        _selectedDetail = selected;
        PopupPartNo.Text = selected.ItemNumber;
        PopupLineInfo.Text = $"Line {selected.LineNo} | Order: {selected.OrderNo}";
        
        // Reset Popup State
        _selectedBatchQty = 0;
        EntryRfid.Text = "";
        LblPopupStatus.Text = "กรุณาเลือก Batch และสแกน Tag";
        LblPopupStatus.TextColor = Color.FromArgb("#888888");
        
        // Smart Batch Logic
        int rem = (selected.TargetQty ?? 0) - (selected.RegisteredQty ?? 0);
        bool isToyota = selected.WorkCenter?.ToUpper().Contains("TOYOTA") ?? false;

        if (isToyota)
        {
            Btn1.IsVisible = rem >= 1;
            Btn7.IsVisible = Btn8.IsVisible = Btn15.IsVisible = false;
        }
        else
        {
            Btn1.IsVisible = false;

            // ISUZU Smart Filtering
            if (rem == 8)
            {
                Btn8.IsVisible = true;
                Btn7.IsVisible = Btn15.IsVisible = false;
            }
            else if (rem == 7)
            {
                Btn7.IsVisible = true;
                Btn8.IsVisible = Btn15.IsVisible = false;
            }
            else
            {
                Btn7.IsVisible = rem >= 7;
                Btn8.IsVisible = rem >= 8;
                Btn15.IsVisible = rem >= 15;
            }
        }

        PopupOverlay.IsVisible = true;
        
        // Clear selection so user can click same row again
        ((CollectionView)sender).SelectedItem = null;
    }

    // =====================================================================
    // Popup Logic
    // =====================================================================
    private void OnClosePopupClicked(object sender, EventArgs e)
    {
        PopupOverlay.IsVisible = false;
    }

    private void OnBatchClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.CommandParameter?.ToString(), out int qty))
        {
            _selectedBatchQty = qty;
            
            // Highlight
            Btn1.BackgroundColor = qty == 1 ? Color.FromArgb("#3D3D9E") : Color.FromArgb("#2A2A5E");
            Btn7.BackgroundColor = qty == 7 ? Color.FromArgb("#3D3D9E") : Color.FromArgb("#2A2A5E");
            Btn8.BackgroundColor = qty == 8 ? Color.FromArgb("#3D3D9E") : Color.FromArgb("#2A2A5E");
            Btn15.BackgroundColor = qty == 15 ? Color.FromArgb("#3D3D9E") : Color.FromArgb("#2A2A5E");
            
            EntryRfid.Focus();
        }
    }

    private void OnPopupRfidChanged(object sender, TextChangedEventArgs e)
    {
        if (EntryRfid.Text?.Length >= 24)
        {
            LblPopupStatus.Text = "สแกนสำเร็จ (96-bit EPC)";
            LblPopupStatus.TextColor = Color.FromArgb("#44DD88");
        }
    }

    private void OnPopupRfidCompleted(object sender, EventArgs e)
    {
        // Handled by Confirm button mainly
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        string epc = EntryRfid.Text?.Trim() ?? "";
        
        if (_selectedBatchQty == 0)
        {
            await DisplayAlert("เตือน", "กรุณาเลือกจำนวน Batch (1, 7, 8 หรือ 15)", "OK");
            return;
        }
        if (string.IsNullOrEmpty(epc))
        {
            await DisplayAlert("เตือน", "กรุณาสแกน RFID Tag", "OK");
            return;
        }

        if (epc.Length < 24)
        {
            await DisplayAlert("เตือน", "RFID Tag สแกนข้อมูลได้ไม่ครบถ้วน (ต้องครบ 24 หลัก)", "OK");
            EntryRfid.Focus();
            return;
        }

        try
        {
            // 1. Check Duplicate Tag in this Order
            bool isDuplicate = await _dbService.IsTagEpcExistsAsync(epc, _selectedDetail!.OrderNo);
            if (isDuplicate)
            {
                await DisplayAlert("สแกนไม่สำเร็จ", "Tag นี้ถูกใช้งานไปแล้วใน Order นี้", "OK");
                EntryRfid.Text = "";
                EntryRfid.Focus();
                return;
            }

            // 2. Save Binding
            var transaction = new TagBindingTransaction
            {
                TagEPC = epc,
                PartCode = _selectedDetail.PartCode,
                WorkOrderDetailId = _selectedDetail.Id,
                QtyPerSet = _selectedBatchQty,
                BindingTime = DateTime.Now
            };
            await _dbService.SaveTagBindingAsync(transaction);

            // 3. Update Status and fetch for UI
            int newQty = (_selectedDetail.RegisteredQty ?? 0) + _selectedBatchQty;
            await _dbService.UpdateRegisteredQtyAsync(_selectedDetail.Id, newQty);

            string? cardNo = await _dbService.GetCardNoByEpcAsync(epc);

            // 4. Update UI List
            _selectedDetail.RegisteredQty = newQty;
            _selectedDetail.Status = newQty >= (_selectedDetail.TargetQty ?? 0) ? "Completed" : "In-Progress";
            
            string newCard = cardNo ?? epc;
            if (string.IsNullOrEmpty(_selectedDetail.CardNo) || _selectedDetail.CardNo == "- ไม่พบข้อมูลบัตร -")
                _selectedDetail.CardNo = newCard;
            else
                _selectedDetail.CardNo += ", " + newCard;

            if (string.IsNullOrEmpty(_selectedDetail.TagEPC))
                _selectedDetail.TagEPC = epc;
            else
                _selectedDetail.TagEPC += ", " + epc;
            
            // Close and Refresh
            PopupOverlay.IsVisible = false;
            await DisplayAlert("สำเร็จ", $"ลงทะเบียน {_selectedDetail.ItemNumber} จำนวน {_selectedBatchQty} ตัวเรียบร้อย", "OK");
            
            // Refetch details to ensure UI sync if multiple users
            // (Optional, just updating local model for speed)
            DetailList.ItemsSource = null;
            DetailList.ItemsSource = _currentDetails;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Database Error", ex.Message, "OK");
        }
    }
}
