using NKKSeatBangpoo.Models;
using NKKSeatBangpoo.Services;
using System.Collections.ObjectModel;

namespace NKKSeatBangpoo.Pages;

public partial class CardMasterPage : ContentPage
{
    private readonly DatabaseService _dbService = new(AppConfig.ConnectionString);
    private List<CardViewModel> _allCards = new();

    public CardMasterPage()
    {
        InitializeComponent();
    }

    private async void OnPageLoaded(object sender, EventArgs e)
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        try
        {
            var cards = await _dbService.GetAllRfidCardsAsync();
            _allCards = cards.Select(c => new CardViewModel(c)).ToList();
            CardList.ItemsSource = _allCards;
            LblTotalCards.Text = _allCards.Count.ToString();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = true; // Still visible if we want it to persist, but IsRunning should be false
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    // --- ADD POPUP HANDLERS ---
    private void OnNewCardClicked(object sender, EventArgs e)
    {
        AddTagEpc.Text = "";
        AddCardNo.Text = "";
        AddPopup.IsVisible = true;
        AddTagEpc.Focus();
    }

    private void OnCloseAddClicked(object sender, EventArgs e)
    {
        AddPopup.IsVisible = false;
    }

    private async void OnSaveNewClicked(object sender, EventArgs e)
    {
        string epc = AddTagEpc.Text?.Trim() ?? "";
        string cardNo = AddCardNo.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(epc))
        {
            await DisplayAlert("Warning", "Please scan or enter Tag EPC", "OK");
            return;
        }

        try
        {
            var card = new RfidCardMaster
            {
                TagEPC = epc,
                CardNo = cardNo,
                CurrentStatus = "Available"
            };

            await _dbService.SaveRfidCardAsync(card);
            
            AddPopup.IsVisible = false;
            await LoadData();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    // --- EDIT POPUP HANDLERS ---
    private void OnRowTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is CardViewModel vm)
        {
            EditTagEpcLabel.Text = vm.TagEPC;
            EditCardNo.Text = vm.CardNo;
            EditStatusPicker.SelectedItem = vm.CurrentStatus;
            EditPopup.IsVisible = true;
            EditCardNo.Focus();
        }
    }

    private void OnCloseEditClicked(object sender, EventArgs e)
    {
        EditPopup.IsVisible = false;
    }

    private async void OnSaveUpdateClicked(object sender, EventArgs e)
    {
        string epc = EditTagEpcLabel.Text;
        string cardNo = EditCardNo.Text?.Trim() ?? "";
        string status = EditStatusPicker.SelectedItem?.ToString() ?? "Available";

        try
        {
            var card = new RfidCardMaster
            {
                TagEPC = epc,
                CardNo = cardNo,
                CurrentStatus = status
            };

            await _dbService.SaveRfidCardAsync(card);
            
            EditPopup.IsVisible = false;
            await LoadData();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CardViewModel vm)
        {
            bool confirm = await DisplayAlert("ยืนยันการลบ", $"คุณต้องการลบบัตรหมายเลข {vm.CardNo} ({vm.TagEPC}) ใช่หรือไม่?", "ใช่", "ไม่ใช่");
            if (confirm)
            {
                try
                {
                    await _dbService.DeleteRfidCardAsync(vm.TagEPC);
                    await LoadData();
                }
                catch (Exception ex)
                {
                    // Check for SQL Foreign Key Constraint Conflict (Error number 547)
                    if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && sqlEx.Number == 547)
                    {
                        await DisplayAlert("ไม่สามารถลบได้", 
                            "ไม่สามารถลบบัตรใบนี้ได้ เนื่องจากประวัติการใช้งาน (Transaction) ในระบบยังอ้างอิงถึงบัตรใบนี้อยู่\n\n" +
                            "แนะนำ: ให้เปลี่ยนสถานะบัตรเป็น 'Lost' หรือ 'Damaged' แทนการลบครับ", "ตกลง");
                    }
                    else if (ex is Microsoft.Data.SqlClient.SqlException sEx && sEx.Number == 547)
                    {
                        await DisplayAlert("ไม่สามารถลบได้", 
                            "ไม่สามารถลบบัตรใบนี้ได้ เนื่องจากประวัติการใช้งาน (Transaction) ในระบบยังอ้างอิงถึงบัตรใบนี้อยู่\n\n" +
                            "แนะนำ: ให้เปลี่ยนสถานะบัตรเป็น 'Lost' หรือ 'Damaged' แทนการลบครับ", "ตกลง");
                    }
                    else
                    {
                        await DisplayAlert("เกิดข้อผิดพลาด", ex.Message, "ตกลง");
                    }
                }
            }
        }
    }

    
}

public class CardViewModel
{
    public string TagEPC { get; set; }
    public string CardNo { get; set; }
    public string CurrentStatus { get; set; }
    public DateTime? RegisterTime { get; set; }
    
    public string RegDateStr => RegisterTime?.ToString("dd/MM/yy") ?? "-";
    public Color StatusBgColor => CurrentStatus?.ToLower() switch
    {
        "available" => Color.FromArgb("#1A4A2A"),
        "in-use"    => Color.FromArgb("#5A4400"),
        "lost"      => Color.FromArgb("#5A1A1A"),
        "damaged"   => Color.FromArgb("#4A1A4A"),
        _           => Color.FromArgb("#333333")
    };

    public CardViewModel(RfidCardMaster c)
    {
        TagEPC = c.TagEPC;
        CardNo = c.CardNo ?? "";
        CurrentStatus = c.CurrentStatus ?? "Unknown";
        RegisterTime = c.RegisterTime;
    }
}
