using Impinj.OctaneSdk;

namespace NKKSeatBangpoo.Services
{
    public class ReaderTagEventArgs : EventArgs
    {
        public string Epc { get; set; } = string.Empty;
        public DateTime ReadTime { get; set; }
    }

    public class ImpinjReaderService
    {
        private ImpinjReader _reader;

        public event EventHandler<ReaderTagEventArgs>? OnTagRead;

        public bool IsConnected => _reader?.IsConnected ?? false;

        public ImpinjReaderService()
        {
            _reader = new ImpinjReader();
        }

        public void Connect(string hostname)
        {
            if (_reader.IsConnected) return;

            _reader.Connect(hostname);

            var settings = _reader.QueryDefaultSettings();
            
            // Adjust reader settings
            settings.Report.IncludeAntennaPortNumber = true;
            settings.Report.IncludeFirstSeenTime = true;
            settings.Report.Mode = ReportMode.Individual; 
            
            // Set Antenna Power from Preferences
            foreach (AntennaConfig ant in settings.Antennas)
            {
                // Default to max power (30.0) if not set
                double power = Preferences.Get($"Antenna{ant.PortNumber}Power", 30.0);
                ant.IsEnabled = true;
                ant.MaxTxPower = false;
                ant.MaxRxSensitivity = true;
                ant.TxPowerInDbm = power;
            }

            _reader.ApplySettings(settings);

            _reader.TagsReported += OnTagsReportedHandler;
            _reader.Start();
        }

        public void Disconnect()
        {
            if (_reader.IsConnected)
            {
                _reader.Stop();
                _reader.TagsReported -= OnTagsReportedHandler;
                _reader.Disconnect();
            }
        }

        private void OnTagsReportedHandler(ImpinjReader reader, TagReport report)
        {
            foreach (var tag in report.Tags)
            {
                OnTagRead?.Invoke(this, new ReaderTagEventArgs
                {
                    Epc = tag.Epc.ToHexString(),
                    ReadTime = tag.FirstSeenTime.LocalDateTime
                });
            }
        }
    }
}
