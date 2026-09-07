namespace NKKSeatBangpoo
{
    /// <summary>
    /// ค่าคงที่สำหรับโปรแกรม (Connection String, Paths, ฯลฯ)
    /// แก้ไข ConnectionString ให้ตรงกับ SQL Server ของคุณ
    /// </summary>
    public static class AppConfig
    {
        // === Database ===
        // ตัวอย่าง: ใช้ Windows Authentication
        // public const string ConnectionString = @"Server=.\SQLEXPRESS;Database=NHKBangpooSeat;Trusted_Connection=True;TrustServerCertificate=True";

        // ตัวอย่าง: ใช้ SQL Authentication
        public const string ConnectionString = @"Server=.\MSSQLSERVER2019;Database=NHKBangpooSeat;User Id=sa;Password=sa;TrustServerCertificate=True";

        // ⚠️ แก้ไข Server Name ให้ตรงกับเครื่องคุณ
        //public const string ConnectionString = @"Server=.\RFID;Database=NHKBangpooSeat;User Id=sa;Password=Passw0rd;TrustServerCertificate=True";

        // === Paths ===
        public const string TemplateFolder = @"Templates";

        // === Export (Phase 4) ===
        public const string SharedDrivePath = @"\\ServerPath\Project_NHK_Bangpoo";
    }
}
