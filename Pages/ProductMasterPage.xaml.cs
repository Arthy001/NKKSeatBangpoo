using NKKSeatBangpoo.Services;

namespace NKKSeatBangpoo.Pages;

public partial class ProductMasterPage : ContentPage
{
    private readonly DatabaseService _dbService = new(AppConfig.ConnectionString);
    private readonly ExcelImportService _excelService = new();
    private List<ProductMasterViewModel> _allProducts = new();
    private string? _selectedFilePath;
    private string _currentImportType = "ISUZU";
    private string _selectedBrandFilter = "ALL";

    public ProductMasterPage()
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
            var products = await _dbService.FetchProductMasterAllAsync();
            _allProducts = products.Select(p => new ProductMasterViewModel(p)).ToList();
            
            // Centralized indexing and filtering
            ApplyFilters();
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

    private void ApplyFilters()
    {
        string keyword = (SearchEntry.Text ?? string.Empty).Trim().ToLower();
        
        var filtered = _allProducts.AsEnumerable();

        // 1. Brand Filter
        if (_selectedBrandFilter != "ALL")
        {
            filtered = filtered.Where(p => p.CustomerName == _selectedBrandFilter);
        }

        // 2. Keyword Search
        if (!string.IsNullOrEmpty(keyword))
        {
            filtered = filtered.Where(p =>
                p.NHKPartNo.ToLower().Contains(keyword) ||
                p.KanbanCode.ToLower().Contains(keyword) ||
                p.CustomerPartNo.ToLower().Contains(keyword) ||
                p.CustomerPartName.ToLower().Contains(keyword) ||
                p.PartName.ToLower().Contains(keyword) ||
                p.Model.ToLower().Contains(keyword)
            );
        }

        var resultList = filtered.ToList();
        
        // Assign row index starting from 1
        for (int i = 0; i < resultList.Count; i++)
        {
            resultList[i].Index = i + 1;
        }

        ProductList.ItemsSource = resultList;
        LblTotalCount.Text = resultList.Count.ToString();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void OnFilterBrandClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            _selectedBrandFilter = btn.Text.ToUpper();
            
            // UI Visual Feedback
            BtnFilterAll.Opacity = (_selectedBrandFilter == "ALL") ? 1.0 : 0.5;
            BtnFilterIsuzu.Opacity = (_selectedBrandFilter == "ISUZU") ? 1.0 : 0.5;
            BtnFilterToyota.Opacity = (_selectedBrandFilter == "TOYOTA") ? 1.0 : 0.5;
            
            ApplyFilters();
        }
    }

    // =====================================================================
    // 🗂️ IMPORT POPUP HANDLERS
    // =====================================================================

    private void OnShowImportIsuzuPopupClicked(object sender, EventArgs e)
    {
        _currentImportType = "ISUZU";
        ShowImportPopup("🚚 Import ISUZU Master");
    }

    private void OnShowImportToyotaPopupClicked(object sender, EventArgs e)
    {
        _currentImportType = "TOYOTA";
        ShowImportPopup("🚗 Import TOYOTA Master");
    }

    private void ShowImportPopup(string title)
    {
        _selectedFilePath = null;
        LblSelectedFileName.Text = "ยังไม่ได้เลือกไฟล์";
        LblPopupTitle.Text = title;
        BtnConfirmImport.IsEnabled = false;
        ImportStatusLayout.IsVisible = false;
        ImportPopup.IsVisible = true;
    }

    private async void OnBrowseFileClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "เลือกไฟล์ Master Data (.xlsx)",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".xlsx" } }
                })
            });

            if (result != null)
            {
                _selectedFilePath = result.FullPath;
                LblSelectedFileName.Text = result.FileName;
                BtnConfirmImport.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Error picking file: " + ex.Message, "OK");
        }
    }

    private async void OnConfirmImportClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedFilePath)) return;

        try
        {
            ImportStatusLayout.IsVisible = true;
            BtnConfirmImport.IsEnabled = false;
            LblPopupImportStatus.Text = "กำลังอ่านข้อมูลจาก Excel...";

            List<NKKSeatBangpoo.Models.ProductMaster> products;
            if (_currentImportType == "TOYOTA")
            {
                products = await Task.Run(() => _excelService.ReadToyotaMasterData(_selectedFilePath));
            }
            else
            {
                products = await Task.Run(() => _excelService.ReadMasterData(_selectedFilePath));
            }
            
            LblPopupImportStatus.Text = $"กำลังบันทึกข้อมูล {products.Count} รายการ...";
            int saved = await _dbService.SaveProductMasterAsync(products);
            
            await DisplayAlert("นำเข้าสำเร็จ", $"✅ บันทึก/อัปเดตข้อมูล ({_currentImportType}) จำนวน {saved} รายการ เรียบร้อยแล้ว", "ตกลง");
            
            ImportPopup.IsVisible = false;
            await LoadData(); // Refresh list
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

// ViewModel สำหรับแสดงผลใน UI
public class ProductMasterViewModel
{
    public int Index { get; set; }
    public string NHKPartNo { get; set; } = string.Empty;
    public string KanbanCode { get; set; } = string.Empty;
    public string PartCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CustomerPartNo { get; set; } = string.Empty;
    public string CustomerPartName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Cover { get; set; } = string.Empty;
    public string KanbanSet { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string IsActiveText => IsActive ? "✔ Active" : "✘ Inactive";
    public Color IsActiveColor => IsActive ? Color.FromArgb("#44DD88") : Color.FromArgb("#FF6666");

    public Color CustomerColor => CustomerName == "TOYOTA" ? Color.FromArgb("#FF4444") : Color.FromArgb("#4488FF");

    public ProductMasterViewModel(NKKSeatBangpoo.Models.ProductMaster p)
    {
        PartCode = p.PartCode ?? "";
        NHKPartNo = p.NHKPartNo ?? "";
        KanbanCode = p.KanbanCode ?? "";
        Code = p.Code ?? "";
        CustomerPartNo = p.CustomerPartNo ?? "";
        CustomerPartName = p.CustomerPartName ?? "";
        CustomerName = p.CustomerName ?? "ISUZU";
        PartName = p.PartName ?? "";
        Model = p.Model ?? "";
        Type = p.Type ?? "";
        Cover = p.Cover ?? "";
        KanbanSet = p.KanbanSet ?? "";
        IsActive = p.IsActive ?? true;
    }
}
