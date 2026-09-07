using System.Collections.ObjectModel;
using System.Text;
using System.IO;
using NKKSeatBangpoo.Models;
using NKKSeatBangpoo.Services;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Net;

namespace NKKSeatBangpoo.Pages;

public partial class GateEntryPage : ContentPage
{
    private readonly DatabaseService _dbService = new(AppConfig.ConnectionString);
    private readonly ImpinjReaderService _readerService = new();
    
    // Caches
    private Dictionary<string, TagBindingTransaction> _bindingCache = new();
    private Dictionary<string, string> _cardMasterCache = new(); // TagEPC -> CardNo
    private HashSet<string> _scannedSessionEpc = new();
    private Dictionary<string, DateTime> _lastSeenEpc = new(); // For 5-second same-batch filter
    
    // Main List
    public ObservableCollection<GateScanItem> ScanItems { get; set; } = new();

    // Status Lists
    public ObservableCollection<GateScanItem> InStockItems { get; set; } = new();
    public ObservableCollection<GateScanItem> NotRegItems { get; set; } = new();
    public ObservableCollection<GateScanItem> UnknownItems { get; set; } = new();

    public GateEntryPage()
    {
        InitializeComponent();
        BindingContext = this;
        ScanList.ItemsSource = ScanItems;

        InitPickers();
    }

    private void InitPickers()
    {
        var powers = new List<string>();
        for (int i = 10; i <= 30; i++)
        {
            powers.Add(i.ToString());
        }

        PickerAnt1.ItemsSource = powers;
        PickerAnt2.ItemsSource = powers;
        PickerAnt3.ItemsSource = powers;
        PickerAnt4.ItemsSource = powers;
    }

    private async void OnPageLoaded(object sender, EventArgs e)
    {
        await ReloadCacheAsync();
        _readerService.OnTagRead += ReaderService_OnTagRead;

        // Load saved IP
        EntryIp.Text = Preferences.Get("ReaderIP", "192.168.1.104");
    }

    private void OnPageUnloaded(object sender, EventArgs e)
    {
        _readerService.OnTagRead -= ReaderService_OnTagRead;
        _readerService.Disconnect();
    }

    private async Task ReloadCacheAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        try
        {
            var allBindings = await _dbService.GetAllTagBindingTransactionsAsync();
            
            _bindingCache.Clear();
            foreach (var b in allBindings)
            {
                if (!string.IsNullOrEmpty(b.TagEPC))
                {
                    // Cache the newest entry for each tag
                    if (!_bindingCache.ContainsKey(b.TagEPC))
                    {
                        _bindingCache[b.TagEPC] = b;
                    }
                }
            }

            var allCards = await _dbService.GetAllRfidCardsAsync();
            _cardMasterCache.Clear();
            foreach (var c in allCards)
            {
                if (!string.IsNullOrEmpty(c.TagEPC) && !string.IsNullOrEmpty(c.CardNo))
                {
                    _cardMasterCache[c.TagEPC] = c.CardNo;
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Cache Load Failed: " + ex.Message, "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private void OnConnectClicked(object sender, EventArgs e)
    {
        try
        {
            if(string.IsNullOrEmpty(EntryIp.Text))
            {
                DisplayAlert("Warning", "Please enter Impinj Reader IP", "OK");
                return;
            }

            _readerService.Connect(EntryIp.Text.Trim());
            LedStatus.Fill = Color.FromArgb("#44DD66"); // Green
            BtnConnect.IsEnabled = false;
            BtnDisconnect.IsEnabled = true;
            EntryIp.IsEnabled = false;
        }
        catch (Exception ex)
        {
            DisplayAlert("Connection Failed", ex.Message, "OK");
        }
    }

    private void OnDisconnectClicked(object sender, EventArgs e)
    {
        try
        {
            _readerService.Disconnect();
            LedStatus.Fill = Color.FromArgb("#555555"); // Gray
            BtnConnect.IsEnabled = true;
            BtnDisconnect.IsEnabled = false;
            EntryIp.IsEnabled = true;
        }
        catch (Exception ex)
        {
            DisplayAlert("Disconnect Failed", ex.Message, "OK");
        }
    }

    private void ReaderService_OnTagRead(object? sender, ReaderTagEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ProcessTag(e.Epc, e.ReadTime);
        });
    }

    private void ProcessTag(string epc, DateTime readTime)
    {
        // 1. Same-Batch Filter: Ignore if scanned within 5 seconds (prevent spamming same EPC)
        if (_lastSeenEpc.TryGetValue(epc, out var lastTime))
        {
            if ((readTime - lastTime).TotalSeconds < 5) return;
        }
        _lastSeenEpc[epc] = readTime;

        // 2. Prevent UI duplication in the current session list
        if (_scannedSessionEpc.Contains(epc)) return; 
        _scannedSessionEpc.Add(epc);

        var item = new GateScanItem
        {
            Epc = epc,
            ReadTime = readTime,
            CardNo = _cardMasterCache.ContainsKey(epc) ? _cardMasterCache[epc] : "Unknown"
        };

        if (_bindingCache.TryGetValue(epc, out var binding))
        {
            item.OrderNo = binding.OrderNo ?? "-";
            item.PartNo = binding.ItemNumber ?? "-";
            item.PartName = binding.ItemName ?? "-";
            item.LineNo = binding.LineNo;
            item.KanbanNo = binding.KanbanNo;
            item.CodeNo = binding.CodeNo;
            item.KBNo = binding.KBNo;
            item.QtyPerSet = binding.QtyPerSet ?? 0;
            item.WorkCenter = binding.WorkCenter;
            item.TransactionId = binding.TransactionID;
            
            item.CustomerPartNo = binding.CustomerPartNo;
            item.KanbanSet = binding.KanbanSet;
            item.KanbanCode = binding.KanbanCode;

            if (binding.GateEntryTime.HasValue)
            {
                item.Status = "Already In-Stock";
                item.RowColorCode = "#333300"; // Yellow tint
                InStockItems.Insert(0, item);
                LblInStock.Text = InStockItems.Count.ToString();
                
                ShowToast("พบ Tag ที่เคยเข้าสต็อกแล้ว!", "#AA8800", "📦");
                FlashCounter(LblInStock.Parent as VisualElement);
            }
            else
            {
                item.Status = "New Scan";
                item.RowColorCode = "#1A1A35"; // Normal
                ScanItems.Insert(0, item);
                LblNewScans.Text = ScanItems.Count.ToString();
                FlashCounter(LblNewScans.Parent as VisualElement);
            }
        }
        else if (_cardMasterCache.ContainsKey(epc))
        {
            item.OrderNo = "N/A";
            item.PartNo = "Not Bound";
            item.Status = "Not Registered";
            item.RowColorCode = "#402000"; // Orange tint
            NotRegItems.Insert(0, item);
            LblNotReg.Text = NotRegItems.Count.ToString();

            ShowToast("Tag นี้ยังไม่ได้ผูกกับ Order!", "#FF8800", "⚠️");
            FlashCounter(LblNotReg.Parent as VisualElement);
        }
        else
        {
            item.OrderNo = "N/A";
            item.PartNo = "Unknown Tag";
            item.Status = "Unknown Tag";
            item.RowColorCode = "#402020"; // Red tint
            UnknownItems.Insert(0, item);
            LblUnknown.Text = UnknownItems.Count.ToString();

            ShowToast("ตรวจพบ Tag นอกระบบ!", "#FF4444", "❓");
            FlashCounter(LblUnknown.Parent as VisualElement);
        }
    }

    private async void ShowToast(string message, string colorHex, string icon)
    {
        ToastMessage.Text = message;
        ToastNotification.BackgroundColor = Color.FromArgb(colorHex);
        ToastIcon.Text = icon;
        
        ToastNotification.IsVisible = true;
        ToastNotification.Opacity = 0;
        ToastNotification.TranslationY = -50;

        await Task.WhenAll(
            ToastNotification.FadeTo(1, 200),
            ToastNotification.TranslateTo(0, 0, 200, Easing.SpringOut)
        );

        await Task.Delay(2500);

        if (ToastNotification.IsVisible)
        {
            await ToastNotification.FadeTo(0, 300);
            ToastNotification.IsVisible = false;
        }
    }

    private async void FlashCounter(VisualElement? element)
    {
        if (element == null) return;
        await element.ScaleTo(1.2, 100);
        await element.ScaleTo(1.0, 100);
    }

    private void OnClearClicked(object sender, EventArgs e)
    {
        ScanItems.Clear();
        InStockItems.Clear();
        NotRegItems.Clear();
        UnknownItems.Clear();
        
        _scannedSessionEpc.Clear();
        
        LblNewScans.Text = "0";
        LblInStock.Text = "0";
        LblNotReg.Text = "0";
        LblUnknown.Text = "0";
    }

    private async void OnStockInClicked(object sender, EventArgs e)
    {
        var newScans = ScanItems.Where(i => i.Status == "New Scan" && i.TransactionId > 0).ToList();
        
        if (newScans.Count == 0)
        {
            await DisplayAlert("Info", "ไม่มีแท็กใหม่สำหรับรับเข้าสต็อก", "OK");
            return;
        }

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            var ids = newScans.Select(i => i.TransactionId).ToList();
            int rows = await _dbService.ConfirmStockInAsync(ids);
            
            await DisplayAlert("Success", $"รับเข้าสต็อกสำเร็จ {rows} รายการ", "OK");

            // Update UI status instantly
            foreach (var s in newScans)
            {
                s.Status = "Stocked In";
                s.RowColorCode = "#1A351A"; // Green tint
            }
            ScanList.ItemsSource = null;
            ScanList.ItemsSource = ScanItems;

            await ReloadCacheAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        if (ScanItems.Count == 0)
        {
            await DisplayAlert("Info", "ไม่มีข้อมูลสำหรับ Export", "OK");
            return;
        }

        try
        {
            // Get path from preferences or default to MyDocuments
            string exportPath = Preferences.Get("ExportFolderPath", string.Empty);
            if (string.IsNullOrEmpty(exportPath))
            {
                exportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NKK_Export");
            }
            
            if (!Directory.Exists(exportPath))
                Directory.CreateDirectory(exportPath);

            // Group by OrderNo and Manufacturer (toyota/isuzu/other)
            var exportGroups = ScanItems
                .GroupBy(i => {
                    string wc = (i.WorkCenter ?? "").ToUpper();
                    string manufacturer = wc.Contains("TOYOTA") ? "toyota" : (wc.Contains("ISUZU") ? "isuzu" : "other");
                    
                    // Sanitize OrderNo for file name
                    string orderNo = string.IsNullOrEmpty(i.OrderNo) || i.OrderNo == "-" || i.OrderNo == "N/A" ? "NoOrder" : i.OrderNo;
                    foreach (char c in Path.GetInvalidFileNameChars())
                    {
                        orderNo = orderNo.Replace(c, '_');
                    }
                    
                    return new { OrderNo = orderNo, Manufacturer = manufacturer };
                })
                .ToList();

            string fileSummary = "";

            foreach (var g in exportGroups)
            {
                var itemsInGroup = g.ToList();
                if (itemsInGroup.Count == 0) continue;

                // User requested format: xxx_toyota_yyyyMMddfff (fff = milliseconds)
                string timeStr = DateTime.Now.ToString("yyyyMMddfff");
                string fName = $"{g.Key.OrderNo}_{g.Key.Manufacturer}_{timeStr}.csv";
                string fPath = Path.Combine(exportPath, fName);

                StringBuilder sb = new StringBuilder();
                sb.Append('\uFEFF'); // UTF-8 BOM

                if (g.Key.Manufacturer == "isuzu")
                {
                    sb.AppendLine("site, Work order,Line,Slip,Item Number,Receipt Qty");
                    
                    var isuzuSummary = itemsInGroup
                        .GroupBy(i => new { i.OrderNo, i.LineNo, i.KanbanNo, i.PartNo })
                        .Select(gr => new {
                            OrderNo = gr.Key.OrderNo,
                            Line = gr.Key.LineNo,
                            Slip = gr.Key.KanbanNo,
                            ItemNo = gr.Key.PartNo,
                            TotalQty = gr.Sum(x => x.QtyPerSet)
                        });

                    foreach (var summary in isuzuSummary)
                    {
                        sb.AppendLine($"122,{summary.OrderNo},{summary.Line},{summary.Slip},{summary.ItemNo},{summary.TotalQty}");
                    }
                }
                else if (g.Key.Manufacturer == "toyota")
                {
                    sb.AppendLine("Site,Work Order,Part no.,Pallet,Skid No.,PW No.,PW Part no.");
                    int palletCounter = 1;
                    foreach (var item in itemsInGroup)
                    {
                        sb.AppendLine($"122,{item.OrderNo},{item.CustomerPartNo},{palletCounter},8,{item.KanbanSet},{item.KanbanCode}");
                        
                        palletCounter++;
                        if (palletCounter > 4) palletCounter = 1;
                    }
                }
                else
                {
                    sb.AppendLine("EPC,Order No,Part No,Part Name,Line,Kanban,Read Time,Status,WorkCenter");
                    foreach (var item in itemsInGroup)
                    {
                        sb.AppendLine($"{item.Epc},{item.OrderNo},{item.PartNo},{item.PartName},{item.LineNo},{item.KanbanNo},{item.ReadTime:yyyy-MM-dd HH:mm:ss},{item.Status},{item.WorkCenter}");
                    }
                }

                // Write file logic
#if WINDOWS
                bool isNetworkPath = exportPath.StartsWith("\\\\");
                if (isNetworkPath)
                {
                    string user = Preferences.Get("DomainUsername", "");
                    string pass = Preferences.Get("DomainPassword", "");
                    
                    var credentials = new NetworkCredential(user, pass);
                    using (new NetworkConnection(exportPath, credentials))
                    {
                        await File.WriteAllTextAsync(fPath, sb.ToString(), Encoding.UTF8);
                    }
                }
                else
                {
                    await File.WriteAllTextAsync(fPath, sb.ToString(), Encoding.UTF8);
                }
#else
                await File.WriteAllTextAsync(fPath, sb.ToString(), Encoding.UTF8);
#endif

                // Mark as exported in DB
                var ids = itemsInGroup.Where(i => i.TransactionId > 0).Select(i => i.TransactionId).ToList();
                if (ids.Count > 0)
                {
                    await _dbService.MarkAsExportedAsync(ids, fName);
                }
                fileSummary += $" - {fName}\n";
                
                // Add a small delay to ensure Milliseconds (fff) change if multiple files are processed
                await Task.Delay(1);
            }

            await DisplayAlert("Success", $"Export รายงานสำเร็จ!\n{fileSummary}\nโฟลเดอร์: {exportPath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Export Failed: " + ex.Message, "OK");
        }
    }

    // --- Popup Handlers ---

    private void OnInStockCardTapped(object sender, TappedEventArgs e)
    {
        ShowPopup("📦 In Stock", InStockItems);
    }

    private void OnNotRegisteredCardTapped(object sender, TappedEventArgs e)
    {
        ShowPopup("⚠️ Not Registered", NotRegItems);
    }

    private void OnUnknownCardTapped(object sender, TappedEventArgs e)
    {
        ShowPopup("❓ Unknown", UnknownItems);
    }

    private void ShowPopup(string title, ObservableCollection<GateScanItem> source)
    {
        if (source.Count == 0) return;
        LblPopupTitle.Text = title;
        LblPopupCount.Text = $"({source.Count})";
        PopupList.ItemsSource = source;
        OverlayPopup.IsVisible = true;
    }

    private void OnClosePopupClicked(object sender, EventArgs e)
    {
        OverlayPopup.IsVisible = false;
        PopupList.ItemsSource = null;
    }

    // --- Antenn+6-+9999999999999999999999**99a Power Settings Popup ---

    private void OnCloseSettingClicked(object sender, EventArgs e)
    {
        SettingPopup.IsVisible = false;
    }

    private void OnSettingClicked(object sender, EventArgs e)
    {
        // Load from preferences
        EntryIp.Text = Preferences.Get("ReaderIP", "192.168.1.104");
        PickerAnt1.SelectedItem = Preferences.Get("Antenna1Power", 30.0).ToString("F0");
        PickerAnt2.SelectedItem = Preferences.Get("Antenna2Power", 30.0).ToString("F0");
        PickerAnt3.SelectedItem = Preferences.Get("Antenna3Power", 30.0).ToString("F0");
        PickerAnt4.SelectedItem = Preferences.Get("Antenna4Power", 30.0).ToString("F0");
        EntryExportPath.Text = Preferences.Get("ExportFolderPath", "Default (MyDocuments/NKK_Export)");
        
        EntryDomainUsername.Text = Preferences.Get("DomainUsername", "");
        EntryDomainPassword.Text = Preferences.Get("DomainPassword", "");

        SettingPopup.IsVisible = true;
    }

    private async void OnSaveSettingClicked(object sender, EventArgs e)
    {
        // Save IP
        if (!string.IsNullOrWhiteSpace(EntryIp.Text))
        {
            Preferences.Set("ReaderIP", EntryIp.Text.Trim());
        }

        if (PickerAnt1.SelectedItem != null) Preferences.Set("Antenna1Power", double.Parse(PickerAnt1.SelectedItem.ToString()!));
        if (PickerAnt2.SelectedItem != null) Preferences.Set("Antenna2Power", double.Parse(PickerAnt2.SelectedItem.ToString()!));
        if (PickerAnt3.SelectedItem != null) Preferences.Set("Antenna3Power", double.Parse(PickerAnt3.SelectedItem.ToString()!));
        if (PickerAnt4.SelectedItem != null) Preferences.Set("Antenna4Power", double.Parse(PickerAnt4.SelectedItem.ToString()!));

        // Save domain credentials
        Preferences.Set("DomainUsername", EntryDomainUsername.Text ?? "");
        Preferences.Set("DomainPassword", EntryDomainPassword.Text ?? "");

        // ExportFolderPath is saved immediately in OnBrowseFolderClicked, no need to save here.
        
        SettingPopup.IsVisible = false;
        
        if (_readerService.IsConnected)
        {
            await DisplayAlert("Settings Saved", "การตั้งค่า Power จะมีผลในรอบการเชื่อมต่อครั้งถัดไป (กรุณา Disconnect แล้ว Connect ใหม่)", "OK");
        }
    }

    private async void OnBrowseFolderClicked(object sender, EventArgs e)
    {
        try
        {
#if WINDOWS
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");

            // Get the current window's HWND by utilizing the Page's Window property
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this.Window.Handler.PlatformView);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                string path = folder.Path;
                EntryExportPath.Text = path;
                Preferences.Set("ExportFolderPath", path);
            }
#else
            await DisplayAlert("Info", "Folder Browser รองรับเฉพาะบน Windows เท่านั้นในเวอร์ชันนี้", "OK");
#endif
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Cannot open folder picker: " + ex.Message, "OK");
        }
    }

    private void AddEpc_Clicked(object sender, EventArgs e)
    {
        if (textEpc != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ProcessTag(textEpc.Text.Trim() , DateTime.Now);
            });
        }
       
    }
}

public class GateScanItem
{
    public int RowIndex { get; set; }
    public int TransactionId { get; set; }
    public string Epc { get; set; } = string.Empty;
    public string EpcDisplay => Epc; // Show full EPC
    public string? CardNo { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string PartNo { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? CodeNo { get; set; }
    public string? KBNo { get; set; }
    public int QtyPerSet { get; set; }
    public int? LineNo { get; set; }
    public string? KanbanNo { get; set; }
    public string? WorkCenter { get; set; }
    
    public string? CustomerPartNo { get; set; }
    public string? KanbanSet { get; set; }
    public string? KanbanCode { get; set; }
    
    public DateTime ReadTime { get; set; }
    public string ReadTimeStr => ReadTime.ToString("HH:mm:ss");
    public string Status { get; set; } = string.Empty;
    
    public string RowColorCode { get; set; } = "#1A1A35";
    public Color RowColor => Color.FromArgb(RowColorCode);

    public Color StatusColor => Status switch
    {
        "New Scan" => Color.FromArgb("#3D3D9E"),
        "Already In-Stock" => Color.FromArgb("#AA8800"), 
        "Stocked In" => Color.FromArgb("#2E7D4F"), 
        _ => Color.FromArgb("#AA3333") 
    };
}

#if WINDOWS
public class NetworkConnection : IDisposable
{
    private string _networkName;

    public NetworkConnection(string networkName, NetworkCredential credentials)
    {
        _networkName = networkName;

        var netResource = new NetResource()
        {
            Scope = ResourceScope.GlobalNetwork,
            ResourceType = ResourceType.Disk,
            DisplayType = ResourceDisplaytype.Share,
            RemoteName = GetNetworkPath(networkName)
        };

        var userName = string.IsNullOrEmpty(credentials.Domain)
            ? credentials.UserName
            : string.Format(@"{0}\{1}", credentials.Domain, credentials.UserName);

        var result = WNetAddConnection2(
            netResource,
            credentials.Password,
            userName,
            0);

        if (result != 0)
        {
            throw new Win32Exception(result, "Error connecting to network path: " + result);
        }
    }

    private string GetNetworkPath(string path)
    {
        if (path.StartsWith(@"\\"))
        {
            int thirdSlash = path.IndexOf('\\', 2);
            if (thirdSlash > 0)
            {
                int fourthSlash = path.IndexOf('\\', thirdSlash + 1);
                if (fourthSlash > 0)
                {
                    return path.Substring(0, fourthSlash);
                }
            }
            return path;
        }
        return path;
    }

    ~NetworkConnection()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        WNetCancelConnection2(_networkName, 0, true);
    }

    [DllImport("mpr.dll")]
    private static extern int WNetAddConnection2(NetResource netResource,
        string password, string username, int flags);

    [DllImport("mpr.dll")]
    private static extern int WNetCancelConnection2(string name, int flags,
        bool force);

    [StructLayout(LayoutKind.Sequential)]
    public class NetResource
    {
        public ResourceScope Scope;
        public ResourceType ResourceType;
        public ResourceDisplaytype DisplayType;
        public int Usage;
        public string LocalName;
        public string RemoteName;
        public string Comment;
        public string Provider;
    }

    public enum ResourceScope : int
    {
        Connected = 1,
        GlobalNetwork,
        Remembered,
        Recent,
        Context
    };

    public enum ResourceType : int
    {
        Any = 0,
        Disk = 1,
        Print = 2,
        Reserved = 8,
    }

    public enum ResourceDisplaytype : int
    {
        Generic = 0x0,
        Domain = 0x01,
        Server = 0x02,
        Share = 0x03,
        File = 0x04,
        Group = 0x05,
        Network = 0x06,
        Root = 0x07,
        Shareadmin = 0x08,
        Directory = 0x09,
        Tree = 0x0a,
        Ndscontainer = 0x0b
    }
}
#endif
