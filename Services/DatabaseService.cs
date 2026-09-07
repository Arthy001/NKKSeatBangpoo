using Microsoft.Data.SqlClient;
using NKKSeatBangpoo.Models;

namespace NKKSeatBangpoo.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> SaveProductMasterAsync(List<ProductMaster> products)
        {
            int affectedRows = 0;
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                MERGE INTO [ProductMaster] AS target
                USING (SELECT @PartCode AS PartCode) AS source
                ON target.PartCode = source.PartCode
                WHEN MATCHED THEN
                    UPDATE SET 
                        [NHKPartNo]   = @NHKPartNo,
                        [PartName]    = @PartName,
                        [KanbanCode]  = @KanbanCode,
                        [CustomerPartNo]   = @CustomerPartNo,
                        [CustomerPartName] = @CustomerPartName,
                        [Model]       = @Model,
                        [Type]        = @Type,
                        [Cover]       = @Cover,
                        [Code]        = @Code,
                        [BarcodePI]   = @BarcodePI,
                        [LineID]      = @LineID,
                        [SeatTypeID]  = @SeatTypeID,
                        [MainPartFlag]= @MainPartFlag,
                        [WeldingBackNo]   = @WeldingBackNo,
                        [WeldingCusionNo] = @WeldingCusionNo,
                        [WeldBack]    = @WeldBack,
                        [WeldCusion]  = @WeldCusion,
                        [Sawing]      = @Sawing,
                        [IsActive]    = @IsActive,
                        [Color]       = @Color,
                        [Side]        = @Side,
                        [CustomerName]= @CustomerName,
                        [GroupStage]  = @GroupStage,
                        [KanbanSet]   = @KanbanSet
                WHEN NOT MATCHED THEN
                    INSERT ([PartCode], [NHKPartNo], [PartName], [KanbanCode], [CustomerPartNo], [CustomerPartName], [Model], [Type], [Cover], [Code], [BarcodePI], [LineID], [SeatTypeID], [MainPartFlag], [WeldingBackNo], [WeldingCusionNo], [WeldBack], [WeldCusion], [Sawing], [IsActive], [Color], [Side], [CustomerName], [GroupStage], [KanbanSet])
                    VALUES (@PartCode, @NHKPartNo, @PartName, @KanbanCode, @CustomerPartNo, @CustomerPartName, @Model, @Type, @Cover, @Code, @BarcodePI, @LineID, @SeatTypeID, @MainPartFlag, @WeldingBackNo, @WeldingCusionNo, @WeldBack, @WeldCusion, @Sawing, @IsActive, @Color, @Side, @CustomerName, @GroupStage, @KanbanSet);";

            foreach (var p in products)
            {
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@PartCode", p.PartCode ?? p.NHKPartNo);
                cmd.Parameters.AddWithValue("@NHKPartNo", (object?)p.NHKPartNo ?? "");
                cmd.Parameters.AddWithValue("@PartName", (object?)p.PartName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KanbanCode", (object?)p.KanbanCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CustomerPartNo", (object?)p.CustomerPartNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CustomerPartName", (object?)p.CustomerPartName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Model", (object?)p.Model ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Type", (object?)p.Type ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Cover", (object?)p.Cover ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Code", (object?)p.Code ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@BarcodePI", (object?)p.BarcodePI ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LineID", (object?)p.LineID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SeatTypeID", (object?)p.SeatTypeID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MainPartFlag", (object?)p.MainPartFlag ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@WeldingBackNo", (object?)p.WeldingBackNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@WeldingCusionNo", (object?)p.WeldingCusionNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@WeldBack", (object?)p.WeldBack ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@WeldCusion", (object?)p.WeldCusion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Sawing", (object?)p.Sawing ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsActive", (object?)p.IsActive ?? true);
                cmd.Parameters.AddWithValue("@Color", (object?)p.Color ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Side", (object?)p.Side ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CustomerName", (object?)p.CustomerName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GroupStage", (object?)p.GroupStage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KanbanSet", (object?)p.KanbanSet ?? DBNull.Value);
                affectedRows += await cmd.ExecuteNonQueryAsync();
            }
            return affectedRows;
        }

        public async Task SaveWorkOrderHeaderAsync(WorkOrderHeader header)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                MERGE INTO [WorkOrderHeader] AS target
                USING (SELECT @OrderNo AS OrderNo) AS source
                ON target.OrderNo = source.OrderNo
                WHEN MATCHED THEN
                    UPDATE SET 
                        [WorkCenter] = @WorkCenter,
                        [DueDate]    = @DueDate,
                        [OrderDate]  = @OrderDate,
                        [ImportTimestamp] = @ImportTimestamp,
                        [Status]     = @Status
                WHEN NOT MATCHED THEN
                    INSERT ([OrderNo], [WorkCenter], [DueDate], [OrderDate], [ImportTimestamp], [Status])
                    VALUES (@OrderNo, @WorkCenter, @DueDate, @OrderDate, @ImportTimestamp, @Status);";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@OrderNo", header.OrderNo);
            cmd.Parameters.AddWithValue("@WorkCenter", (object?)header.WorkCenter ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@DueDate", (object?)header.DueDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@OrderDate", (object?)header.OrderDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ImportTimestamp", header.ImportTimestamp ?? DateTime.Now);
            cmd.Parameters.AddWithValue("@Status", header.Status ?? "Open");

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<int> SaveWorkOrderDetailsAsync(string orderNo, List<WorkOrderDetail> details, bool isToyotaFile = false)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // 1. Validation: Verify all Parts exist in ProductMaster
            var missingParts = new List<string>(); // Format: LineNo|NHK|Kanban
            var validatedParts = new Dictionary<string, string>(); // Key: NHK|Kanban, Value: PartCode

            foreach (var d in details)
            {
                // Determine which field to use for lookup based on brand
                string lookupKey = isToyotaFile ? (d.CustomerPartNo ?? "").Trim() : (d.NHKPartNo ?? d.ItemNumber ?? "").Trim();
                
                if (string.IsNullOrEmpty(lookupKey)) continue;

                if (validatedParts.ContainsKey(lookupKey)) continue;

                string lookupSql = "SELECT [PartCode] FROM [ProductMaster] WHERE [PartCode] = @pc";
                using (var cmdLookup = new SqlCommand(lookupSql, conn))
                {
                    cmdLookup.Parameters.AddWithValue("@pc", lookupKey);
                    var res = await cmdLookup.ExecuteScalarAsync();
                    if (res != null)
                    {
                        validatedParts[lookupKey] = res.ToString();
                    }
                    else
                    {
                        string nhk = (d.NHKPartNo ?? d.ItemNumber ?? "").Trim();
                        string kanban = (d.KanbanNo ?? "").Trim();
                        string missingInfo = $"{d.LineNo}|{nhk}|{kanban}";
                        if (!missingParts.Contains(missingInfo))
                            missingParts.Add(missingInfo);
                    }
                }
            }

            if (missingParts.Any())
            {
                var errorMsg = "ไม่พบข้อมูลใน Product Master ดังนี้:\n";
                foreach(var m in missingParts)
                {
                    var parts = m.Split('|');
                    errorMsg += $"• แถว {parts[0]}: NHK {parts[1]}, Kanban {parts[2]}\n";
                }
                errorMsg += "\nกรุณาไปสร้างข้อมูลที่หน้า Product Master ก่อนนำเข้า Work Order";
                throw new Exception(errorMsg);
            }

            // 2. Clear existing details
            string deleteSql = "DELETE FROM [WorkOrderDetail] WHERE [OrderNo] = @OrderNo";
            using (var deleteCmd = new SqlCommand(deleteSql, conn))
            {
                deleteCmd.Parameters.AddWithValue("@OrderNo", orderNo);
                await deleteCmd.ExecuteNonQueryAsync();
            }

            // 3. Insert Details
            string insertSql = @"
                INSERT INTO [WorkOrderDetail] 
                ([OrderNo], [PartCode], [CodeNo], [LineNo], [ItemNumber], [ItemName], [Slip], [KanbanNo], [KBNo], [KanbanQty], [PalletQty], [TargetQty], [RegisteredQty], [Package], [Status])
                VALUES 
                (@OrderNo, @PartCode, @CodeNo, @LineNo, @ItemNumber, @ItemName, @Slip, @KanbanNo, @KBNo, @KanbanQty, @PalletQty, @TargetQty, @RegisteredQty, @Package, @Status)";

            int inserted = 0;
            foreach (var d in details)
            {
                string lookupKey = isToyotaFile ? (d.CustomerPartNo ?? "").Trim() : (d.NHKPartNo ?? d.ItemNumber ?? "").Trim();
                if (string.IsNullOrEmpty(lookupKey)) continue;
                
                string partCode = validatedParts[lookupKey];

                using var cmd = new SqlCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("@OrderNo", orderNo);
                cmd.Parameters.AddWithValue("@PartCode", partCode);
                cmd.Parameters.AddWithValue("@CodeNo", (object?)d.CodeNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LineNo", (object?)d.LineNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ItemNumber", (object?)d.ItemNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ItemName", (object?)d.ItemName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Slip", (object?)d.Slip ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KanbanNo", (object?)d.KanbanNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KBNo", (object?)d.KBNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KanbanQty", (object?)d.KanbanQty ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PalletQty", (object?)d.PalletQty ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TargetQty", (object?)d.TargetQty ?? 0);
                cmd.Parameters.AddWithValue("@RegisteredQty", d.RegisteredQty ?? 0);
                cmd.Parameters.AddWithValue("@Package", (object?)d.Package ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Status", d.Status ?? "Pending");

                inserted += await cmd.ExecuteNonQueryAsync();
            }
            return inserted;
        }

        private async Task<int> GetNextDetailIdAsync(SqlConnection conn)
        {
            string sql = "SELECT ISNULL(MAX(DetailID), 0) + 1 FROM WorkOrderDetail";
            using var cmd = new SqlCommand(sql, conn);
            return (int)await cmd.ExecuteScalarAsync();
        }

        public async Task<List<WorkOrderHeader>> GetWorkOrdersAsync(string? statusFilter = null)
        {
            var orders = new List<WorkOrderHeader>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "SELECT [OrderNo], [WorkCenter], [DueDate], [Status], [OrderDate] FROM [WorkOrderHeader]";
            if (!string.IsNullOrEmpty(statusFilter))
                sql += " WHERE [Status] = @Status";
            sql += " ORDER BY [DueDate] DESC";

            using var cmd = new SqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(statusFilter))
                cmd.Parameters.AddWithValue("@Status", statusFilter);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(new WorkOrderHeader
                {
                    OrderNo = reader.GetString(0),
                    WorkCenter = reader.IsDBNull(1) ? null : reader.GetString(1),
                    DueDate = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                    Status = reader.IsDBNull(3) ? null : reader.GetString(3),
                    OrderDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
                });
            }
            return orders;
        }

        public async Task<List<WorkOrderDetail>> GetWorkOrderDetailsAsync(string orderNo)
        {
            var details = new List<WorkOrderDetail>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
SELECT d.[Id], d.[OrderNo], d.[PartCode], d.[CodeNo], d.[LineNo], 
       d.[ItemNumber], d.[ItemName], d.[Slip], d.[KanbanNo], d.[KBNo], 
       d.[KanbanQty], d.[PalletQty], d.[TargetQty], d.[RegisteredQty], 
       d.[Package], d.[Status], p.[PartName] AS MasterPartName, p.[NHKPartNo] AS MasterNHKPartNo,
       Tags.EPCs, Tags.CardNos, h.[WorkCenter]
FROM [WorkOrderDetail] d
LEFT JOIN [ProductMaster] p ON d.[PartCode] = p.[PartCode]
LEFT JOIN [WorkOrderHeader] h ON d.[OrderNo] = h.[OrderNo]
OUTER APPLY (
    SELECT 
        STRING_AGG(CAST(t.[TagEPC] AS VARCHAR(MAX)), ', ') WITHIN GROUP (ORDER BY t.[BindingTime]) AS EPCs,
        STRING_AGG(CAST(ISNULL(rm.[CardNo], t.[TagEPC]) AS VARCHAR(MAX)), ', ') WITHIN GROUP (ORDER BY t.[BindingTime]) AS CardNos
    FROM [TagBindingTransaction] t
    LEFT JOIN [RfidCardMaster] rm ON t.[TagEPC] = rm.[TagEPC]
    WHERE t.[WorkOrderDetailId] = d.[Id]
) AS Tags
WHERE d.[OrderNo] = @OrderNo 
ORDER BY d.[LineNo]";

    using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@OrderNo", orderNo);

    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
                details.Add(new WorkOrderDetail
                {
                    Id = reader.GetInt32(0),
                    OrderNo = reader.GetString(1),
                    PartCode = reader.IsDBNull(2) ? null : reader.GetString(2),
                    CodeNo = reader.IsDBNull(3) ? null : reader.GetString(3),
                    LineNo = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                    ItemNumber = reader.IsDBNull(5) ? null : reader.GetString(5),
                    ItemName = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Slip = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                    KanbanNo = reader.IsDBNull(8) ? null : reader.GetString(8),
                    KBNo = reader.IsDBNull(9) ? null : reader.GetString(9),
                    KanbanQty = reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10),
                    PalletQty = reader.IsDBNull(11) ? (int?)null : reader.GetInt32(11),
                    TargetQty = reader.IsDBNull(12) ? (int?)null : reader.GetInt32(12),
                    RegisteredQty = reader.IsDBNull(13) ? (int?)null : reader.GetInt32(13),
                    Package = reader.IsDBNull(14) ? null : reader.GetString(14),
                    Status = reader.IsDBNull(15) ? null : reader.GetString(15),
                    PartName = reader.IsDBNull(16) ? null : reader.GetString(16),
                    NHKPartNo = reader.IsDBNull(17) ? null : reader.GetString(17),
                    TagEPC = reader.IsDBNull(18) ? null : reader.GetString(18),
                    CardNo = reader.IsDBNull(19) ? null : reader.GetString(19),
                    WorkCenter = reader.IsDBNull(20) ? null : reader.GetString(20)
                });
    }
    return details;
}
        public async Task<List<TagBindingTransaction>> GetAllTagBindingTransactionsAsync()
        {
            var list = new List<TagBindingTransaction>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
SELECT t.[TransactionID], t.[TagEPC], t.[PartCode], t.[QtyPerSet], 
       t.[BindingTime], t.[GateEntryTime], t.[IsExported], t.[ExportFileName],
       wd.[OrderNo], pm.[PartName], wd.[LineNo], wd.[KanbanNo], h.[WorkCenter], pm.[NHKPartNo], rm.[CardNo],
       wd.[ItemNumber], wd.[ItemName], wd.[CodeNo], wd.[KBNo],
       pm.[KanbanCode], pm.[CustomerPartNo], pm.[KanbanSet]
FROM [TagBindingTransaction] t
LEFT JOIN [WorkOrderDetail] wd ON t.[WorkOrderDetailId] = wd.[Id]
LEFT JOIN [ProductMaster] pm ON t.[PartCode] = pm.[PartCode]
LEFT JOIN [WorkOrderHeader] h ON wd.[OrderNo] = h.[OrderNo]
LEFT JOIN [RfidCardMaster] rm ON t.[TagEPC] = rm.[TagEPC]
ORDER BY t.[BindingTime] DESC";

    using var cmd = new SqlCommand(sql, conn);
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
list.Add(new TagBindingTransaction
{
    TransactionID = reader.GetInt32(0),
    TagEPC = reader.IsDBNull(1) ? null : reader.GetString(1),
    PartCode = reader.IsDBNull(2) ? null : reader.GetString(2),
    QtyPerSet = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
    BindingTime = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
    GateEntryTime = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
    IsExported = reader.IsDBNull(6) ? (bool?)null : reader.GetBoolean(6),
    ExportFileName = reader.IsDBNull(7) ? null : reader.GetString(7),
    OrderNo = reader.IsDBNull(8) ? null : reader.GetString(8),
    PartName = reader.IsDBNull(9) ? null : reader.GetString(9),
    LineNo = reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10),
    KanbanNo = reader.IsDBNull(11) ? null : reader.GetString(11),
    WorkCenter = reader.IsDBNull(12) ? null : reader.GetString(12),
    NHKPartNo = reader.IsDBNull(13) ? null : reader.GetString(13),
    CardNo = reader.IsDBNull(14) ? null : reader.GetString(14),
    ItemNumber = reader.IsDBNull(15) ? null : reader.GetString(15),
    ItemName = reader.IsDBNull(16) ? null : reader.GetString(16),
    CodeNo = reader.IsDBNull(17) ? null : reader.GetString(17),
    KBNo = reader.IsDBNull(18) ? null : reader.GetString(18),
    KanbanCode = reader.IsDBNull(19) ? null : reader.GetString(19),
    CustomerPartNo = reader.IsDBNull(20) ? null : reader.GetString(20),
    KanbanSet = reader.IsDBNull(21) ? null : reader.GetString(21)
});
    }
    return list;
}
        public async Task<List<ProductMaster>> FetchProductMasterAllAsync()
        {
            var products = new List<ProductMaster>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"    SELECT [PartCode], [NHKPartNo], [PartName], [KanbanCode], [CustomerPartNo], 
                        [CustomerPartName], [Model], [Type], [Cover], [Code], [BarcodePI], 
                        [LineID], [SeatTypeID], [MainPartFlag], [WeldingBackNo], [WeldingCusionNo], 
                        [WeldBack], [WeldCusion], [Sawing], [IsActive], [Color], [Side], 
                        [CustomerName], [GroupStage], [KanbanSet]
                      FROM [ProductMaster] ORDER BY [NHKPartNo]";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                products.Add(new ProductMaster
                {
                    PartCode = reader.GetString(0),
                    NHKPartNo = reader.GetString(1),
                    PartName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    KanbanCode = reader.IsDBNull(3) ? null : reader.GetString(3),
                    CustomerPartNo = reader.IsDBNull(4) ? null : reader.GetString(4),
                    CustomerPartName = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Model = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Type = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Cover = reader.IsDBNull(8) ? null : reader.GetString(8),
                    Code = reader.IsDBNull(9) ? null : reader.GetString(9),
                    BarcodePI = reader.IsDBNull(10) ? null : reader.GetString(10),
                    LineID = reader.IsDBNull(11) ? null : reader.GetString(11),
                    SeatTypeID = reader.IsDBNull(12) ? null : reader.GetString(12),
                    MainPartFlag = reader.IsDBNull(13) ? null : reader.GetString(13),
                    WeldingBackNo = reader.IsDBNull(14) ? null : reader.GetString(14),
                    WeldingCusionNo = reader.IsDBNull(15) ? null : reader.GetString(15),
                    WeldBack = reader.IsDBNull(16) ? null : reader.GetString(16),
                    WeldCusion = reader.IsDBNull(17) ? null : reader.GetString(17),
                    Sawing = reader.IsDBNull(18) ? null : reader.GetString(18),
                    IsActive = reader.IsDBNull(19) ? (bool?)null : reader.GetBoolean(19),
                    Color = reader.IsDBNull(20) ? null : reader.GetString(20),
                    Side = reader.IsDBNull(21) ? null : reader.GetString(21),
                    CustomerName = reader.IsDBNull(22) ? null : reader.GetString(22),
                    GroupStage = reader.IsDBNull(23) ? null : reader.GetString(23),
                    KanbanSet = reader.IsDBNull(24) ? null : reader.GetString(24)
                });
            }
            return products;
        }



        public async Task SaveTagBindingAsync(TagBindingTransaction transaction)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();
            try
            {
                // 1. Insert Transaction
        string sqlBinding = @"
            INSERT INTO [TagBindingTransaction] ([TagEPC], [PartCode], [QtyPerSet], [BindingTime], [IsExported], [WorkOrderDetailId])
            VALUES (@TagEPC, @PartCode, @QtyPerSet, @BindingTime, 0, @WorkOrderDetailId)";

        using var cmd1 = new SqlCommand(sqlBinding, conn, tran);
        cmd1.Parameters.AddWithValue("@TagEPC", (object?)transaction.TagEPC ?? DBNull.Value);
        cmd1.Parameters.AddWithValue("@PartCode", (object?)transaction.PartCode ?? DBNull.Value);
        cmd1.Parameters.AddWithValue("@QtyPerSet", (object?)transaction.QtyPerSet ?? 0);
        cmd1.Parameters.AddWithValue("@BindingTime", transaction.BindingTime ?? DateTime.Now);
        cmd1.Parameters.AddWithValue("@WorkOrderDetailId", (object?)transaction.WorkOrderDetailId ?? DBNull.Value);
        await cmd1.ExecuteNonQueryAsync();

        // 2. Update Card Status to 'In-Use'
        string sqlCard = "UPDATE [RfidCardMaster] SET [CurrentStatus] = 'In-Use' WHERE [TagEPC] = @TagEPC";
                using var cmd2 = new SqlCommand(sqlCard, conn, tran);
                cmd2.Parameters.AddWithValue("@TagEPC", (object?)transaction.TagEPC ?? DBNull.Value);
                await cmd2.ExecuteNonQueryAsync();

                await tran.CommitAsync();
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateRegisteredQtyAsync(int id, int? newQty)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                UPDATE [WorkOrderDetail]
                SET [RegisteredQty] = @Qty,
                    [Status] = CASE WHEN @Qty >= [TargetQty] THEN 'Completed' ELSE 'In-Progress' END
                WHERE [Id] = @Id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Qty", newQty);
            cmd.Parameters.AddWithValue("@Id", id);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> ClearTagSelectionAsync(List<int> transactionIds)
        {
            if (transactionIds == null || transactionIds.Count == 0) return true;
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();
            try
            {
                string ids = string.Join(",", transactionIds);

                // 1. Update Card Status back to Available
                string sqlCard = $@"
                    UPDATE [RfidCardMaster] 
                    SET [CurrentStatus] = 'Available', 
                        [FateInTime] = NULL 
                    WHERE [TagEPC] IN (SELECT [TagEPC] FROM [TagBindingTransaction] WHERE [TransactionID] IN ({ids}))";
                using var cmd1 = new SqlCommand(sqlCard, conn, tran);
                await cmd1.ExecuteNonQueryAsync();

                // 2. Delete Transaction
                string sqlDelete = $"DELETE FROM [TagBindingTransaction] WHERE [TransactionID] IN ({ids})";
                using var cmd2 = new SqlCommand(sqlDelete, conn, tran);
                int rows = await cmd2.ExecuteNonQueryAsync();

                await tran.CommitAsync();
                return rows > 0;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }

        public async Task<int> ConfirmStockInAsync(List<int> transactionIds)
        {
            if (transactionIds == null || transactionIds.Count == 0) return 0;
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tran = conn.BeginTransaction();
            try
            {
                string ids = string.Join(",", transactionIds);

                // 1. Update Transaction Entry Time
                string sqlTran = $"UPDATE [TagBindingTransaction] SET [GateEntryTime] = GETDATE() WHERE [TransactionID] IN ({ids})";
                using var cmd1 = new SqlCommand(sqlTran, conn, tran);
                int rows = await cmd1.ExecuteNonQueryAsync();

                // 2. Update Card Status to 'In Stock'
                string sqlCard = $@"
                    UPDATE [RfidCardMaster] 
                    SET [CurrentStatus] = 'In Stock', 
                        [FateInTime] = GETDATE() 
                    WHERE [TagEPC] IN (SELECT [TagEPC] FROM [TagBindingTransaction] WHERE [TransactionID] IN ({ids}))";
                using var cmd2 = new SqlCommand(sqlCard, conn, tran);
                await cmd2.ExecuteNonQueryAsync();

                await tran.CommitAsync();
                return rows;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }

        public async Task<int> MarkAsExportedAsync(List<int> transactionIds, string exportFileName)
        {
            if (transactionIds == null || transactionIds.Count == 0) return 0;
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            string ids = string.Join(",", transactionIds);
            string sql = $"UPDATE [TagBindingTransaction] SET [IsExported] = 1, [ExportFileName] = @ExportFileName WHERE [TransactionID] IN ({ids})";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ExportFileName", exportFileName);
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> IsTagEpcExistsAsync(string tagEpc, string orderNo)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    string sql = @"
        SELECT COUNT(1) 
        FROM [TagBindingTransaction] t
        JOIN [WorkOrderDetail] d ON t.[WorkOrderDetailId] = d.[Id]
        WHERE t.[TagEPC] = @TagEPC AND d.[OrderNo] = @OrderNo";
    
    using var cmd = new SqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@TagEPC", tagEpc);
    cmd.Parameters.AddWithValue("@OrderNo", orderNo);
    int count = (int)(await cmd.ExecuteScalarAsync() ?? 0);
    return count > 0;
}
        // =====================================================================
        // 13. จัดการทะเบียนบัตร (RfidCardMaster)
        // =====================================================================
        public async Task<List<RfidCardMaster>> GetAllRfidCardsAsync()
        {
            var list = new List<RfidCardMaster>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = "SELECT [Id], [TagEPC], [CardNo], [CurrentStatus], [RegisterTime], [FateInTime] FROM [RfidCardMaster] ORDER BY [CardNo]";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new RfidCardMaster
                {
                    Id = reader.GetInt32(0),
                    TagEPC = reader.GetString(1),
                    CardNo = reader.IsDBNull(2) ? null : reader.GetString(2),
                    CurrentStatus = reader.IsDBNull(3) ? null : reader.GetString(3),
                    RegisterTime = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    GateInTime = reader.IsDBNull(5) ? null : reader.GetDateTime(5) // Map FateInTime to GateInTime in Model
                });
            }
            return list;
        }

        public async Task SaveRfidCardAsync(RfidCardMaster card)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                MERGE INTO [RfidCardMaster] AS target
                USING (SELECT @TagEPC AS TagEPC) AS source
                ON target.TagEPC = source.TagEPC
                WHEN MATCHED THEN
                    UPDATE SET 
                        [CardNo] = @CardNo,
                        [CurrentStatus] = @Status
                WHEN NOT MATCHED THEN
                    INSERT ([TagEPC], [CardNo], [CurrentStatus], [RegisterTime])
                    VALUES (@TagEPC, @CardNo, @Status, GETDATE());";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TagEPC", card.TagEPC);
            cmd.Parameters.AddWithValue("@CardNo", (object?)card.CardNo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", (object?)card.CurrentStatus ?? "Available");

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<string?> GetCardNoByEpcAsync(string epc)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            string sql = "SELECT [CardNo] FROM [RfidCardMaster] WHERE [TagEPC] = @EPC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EPC", epc);
            var res = await cmd.ExecuteScalarAsync();
            return res?.ToString();
        }

        public async Task DeleteRfidCardAsync(string tagEpc)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            string sql = "DELETE FROM [RfidCardMaster] WHERE [TagEPC] = @TagEPC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TagEPC", tagEpc);
            await cmd.ExecuteNonQueryAsync();
        }

        // =====================================================================
        // 14. ระบบจัดการคลังสินค้า (Inventory Stock Management)
        // =====================================================================
        public async Task<List<InventoryStockItem>> GetInventoryStockAsync(string searchFilter = "")
{
    var list = new List<InventoryStockItem>();
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();

    string sql = @"
        SELECT 
            wd.OrderNo,
            p.NHKPartNo,
            wd.KanbanNo,
            MAX(p.PartName) AS PartName,
            SUM(t.QtyPerSet) AS TotalQty,
            COUNT(t.TransactionID) AS TotalTags,
            MAX(t.GateEntryTime) AS LastGateEntryTime
        FROM [TagBindingTransaction] t
        LEFT JOIN [WorkOrderDetail] wd ON t.WorkOrderDetailId = wd.Id
        LEFT JOIN [ProductMaster] p ON t.PartCode = p.PartCode
        WHERE t.GateEntryTime IS NOT NULL AND (t.IsExported = 0 OR t.IsExported IS NULL)";

    if (!string.IsNullOrEmpty(searchFilter))
    {
        sql += " AND (wd.[OrderNo] LIKE @Search OR p.[NHKPartNo] LIKE @Search OR p.[PartName] LIKE @Search) ";
    }

    sql += @" 
        GROUP BY wd.OrderNo, p.NHKPartNo, wd.KanbanNo
        ORDER BY wd.OrderNo, p.NHKPartNo, wd.KanbanNo";

    using var cmd = new SqlCommand(sql, conn);
    if (!string.IsNullOrEmpty(searchFilter))
    {
        cmd.Parameters.AddWithValue("@Search", "%" + searchFilter + "%");
    }

    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        list.Add(new InventoryStockItem
        {
            OrderNo = reader.IsDBNull(0) ? "" : reader.GetString(0),
            NHKPartNo = reader.IsDBNull(1) ? "" : reader.GetString(1),
            KanbanNo = reader.IsDBNull(2) ? "" : reader.GetString(2),
            PartName = reader.IsDBNull(3) ? "" : reader.GetString(3),
            TotalQty = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
            TotalTags = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
            LastGateEntryTime = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
        });
    }
    return list;
}
        public async Task<List<InventoryHistoryItem>> GetInventoryHistoryAsync(string searchFilter = "")
        {
            var list = new List<InventoryHistoryItem>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
        SELECT t.[TransactionID], t.[TagEPC], wd.[OrderNo], pm.[NHKPartNo], wd.[KanbanNo], t.[QtyPerSet], t.[GateEntryTime], t.[IsExported], rm.[CardNo]
        FROM [TagBindingTransaction] t
        LEFT JOIN [WorkOrderDetail] wd ON t.[WorkOrderDetailId] = wd.[Id]
        LEFT JOIN [ProductMaster] pm ON t.[PartCode] = pm.[PartCode]
        LEFT JOIN [RfidCardMaster] rm ON t.[TagEPC] = rm.[TagEPC]
        WHERE t.[GateEntryTime] IS NOT NULL ";

    if (!string.IsNullOrEmpty(searchFilter))
    {
        sql += " AND (wd.[OrderNo] LIKE @Search OR pm.[NHKPartNo] LIKE @Search OR t.[TagEPC] LIKE @Search OR rm.[CardNo] LIKE @Search) ";
    }
    
    sql += " ORDER BY t.[GateEntryTime] DESC";

    using var cmd = new SqlCommand(sql, conn);
    if (!string.IsNullOrEmpty(searchFilter))
    {
        cmd.Parameters.AddWithValue("@Search", "%" + searchFilter + "%");
    }

    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        list.Add(new InventoryHistoryItem
        {
            TransactionID = reader.GetInt32(0),
            TagEPC = reader.IsDBNull(1) ? null : reader.GetString(1),
            OrderNo = reader.IsDBNull(2) ? null : reader.GetString(2),
            NHKPartNo = reader.IsDBNull(3) ? null : reader.GetString(3),
            KanbanNo = reader.IsDBNull(4) ? null : reader.GetString(4),
            QtyPerSet = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
            GateEntryTime = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
            IsExported = reader.IsDBNull(7) ? (bool?)null : reader.GetBoolean(7),
            CardNo = reader.IsDBNull(8) ? null : reader.GetString(8)
        });
    }
            return list;
        }
        // =====================================================================
        // 14. Daily Dashboard Operations (Updated Aggregations)
        // =====================================================================
        public async Task<DailySummary> GetDailySummaryAsync()
        {
            var summary = new DailySummary();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string  sql = @"    SELECT 
                    ISNULL(SUM(d.TargetQty), 0) as TotalTargetToday,
                    ISNULL(SUM(d.RegisteredQty), 0) as TotalProducedToday,
                    (SELECT COUNT(1) FROM [TagBindingTransaction] WHERE [GateEntryTime] IS NULL) as TotalPendingGate,
                    (SELECT ISNULL(SUM(QtyPerSet), 0) FROM [TagBindingTransaction] WHERE CAST([GateEntryTime] AS DATE) = CAST(GETDATE() AS DATE)) as TotalInStockToday,
                    (SELECT COUNT(DISTINCT h.OrderNo) FROM [WorkOrderHeader] h WHERE h.Status = 'Open' OR CAST(h.OrderDate AS DATE) = CAST(GETDATE() AS DATE)) as TotalOrders,
                    (SELECT ISNULL(SUM(QtyPerSet), 0) FROM [TagBindingTransaction] WHERE CAST([BindingTime] AS DATE) = CAST(GETDATE() AS DATE)) as TotalRegisteredToday
                FROM [WorkOrderHeader] h
                JOIN [WorkOrderDetail] d ON h.OrderNo = d.OrderNo
                WHERE h.Status = 'Open' OR CAST(h.OrderDate AS DATE) = CAST(GETDATE() AS DATE)";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                summary.TotalTargetToday = reader.GetInt32(0);
                summary.TotalProducedToday = reader.GetInt32(1);
                summary.TotalPendingGate = reader.GetInt32(2);
                summary.TotalInStockToday = reader.GetInt32(3);
                summary.TotalOrdersToday = reader.GetInt32(4);
                summary.TotalRegisteredToday = reader.GetInt32(5);
                
                summary.TotalRemainingProduce = Math.Max(0, summary.TotalTargetToday - summary.TotalProducedToday);
            }
            return summary;
        }

        public async Task<List<SeatTypeSummary>> GetSeatTypeSummariesAsync()
        {
            var list = new List<SeatTypeSummary>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                SELECT 
                    CASE 
                        WHEN h.WorkCenter LIKE '%ISUZU%' THEN 'ISUZU'
                        WHEN h.WorkCenter LIKE '%TOYOTA%' THEN 'TOYOTA'
                        ELSE h.WorkCenter 
                    END as SeatType,
                    COUNT(DISTINCT h.OrderNo) as OrderCount,
                    SUM(d.TargetQty) as TargetQty,
                    SUM(d.RegisteredQty) as ProducedQty,
                    (SELECT ISNULL(SUM(QtyPerSet), 0) FROM TagBindingTransaction t 
                     JOIN WorkOrderDetail wd ON t.WorkOrderDetailId = wd.Id
                     JOIN WorkOrderHeader h2 ON wd.OrderNo = h2.OrderNo 
                     WHERE (CASE WHEN h2.WorkCenter LIKE '%ISUZU%' THEN 'ISUZU' WHEN h2.WorkCenter LIKE '%TOYOTA%' THEN 'TOYOTA' ELSE h2.WorkCenter END) = 
                           (CASE WHEN h.WorkCenter LIKE '%ISUZU%' THEN 'ISUZU' WHEN h.WorkCenter LIKE '%TOYOTA%' THEN 'TOYOTA' ELSE h.WorkCenter END)
                     AND t.GateEntryTime IS NULL) as PendingGateQty,
                    (SELECT ISNULL(SUM(QtyPerSet), 0) FROM TagBindingTransaction t 
                     JOIN WorkOrderDetail wd ON t.WorkOrderDetailId = wd.Id
                     JOIN WorkOrderHeader h2 ON wd.OrderNo = h2.OrderNo 
                     WHERE (CASE WHEN h2.WorkCenter LIKE '%ISUZU%' THEN 'ISUZU' WHEN h2.WorkCenter LIKE '%TOYOTA%' THEN 'TOYOTA' ELSE h2.WorkCenter END) = 
                           (CASE WHEN h.WorkCenter LIKE '%ISUZU%' THEN 'ISUZU' WHEN h.WorkCenter LIKE '%TOYOTA%' THEN 'TOYOTA' ELSE h.WorkCenter END)
                     AND CAST(t.GateEntryTime AS DATE) = CAST(GETDATE() AS DATE)) as InStockQty
                FROM [WorkOrderHeader] h
                JOIN [WorkOrderDetail] d ON h.OrderNo = d.OrderNo
                WHERE h.Status = 'Open' OR CAST(h.OrderDate AS DATE) = CAST(GETDATE() AS DATE)
                GROUP BY CASE WHEN h.WorkCenter LIKE '%ISUZU%' THEN 'ISUZU' WHEN h.WorkCenter LIKE '%TOYOTA%' THEN 'TOYOTA' ELSE h.WorkCenter END";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var target = reader.GetInt32(2);
                var produced = reader.GetInt32(3);
                list.Add(new SeatTypeSummary
                {
                    SeatTypeName = reader.GetString(0),
                    OrderCount = reader.GetInt32(1),
                    TargetQty = target,
                    ProducedQty = produced,
                    PendingGateQty = reader.GetInt32(4),
                    InStockQty = reader.GetInt32(5),
                    RemainingQty = Math.Max(0, target - produced)
                });
            }
            return list;
        }

        public async Task<List<HourlyThroughput>> GetHourlyThroughputAsync()
        {
            var list = new List<HourlyThroughput>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
                SELECT DATEPART(HOUR, [GateEntryTime]) as Hour, SUM(QtyPerSet) as Count
                FROM [TagBindingTransaction]
                WHERE CAST([GateEntryTime] AS DATE) = CAST(GETDATE() AS DATE)
                GROUP BY DATEPART(HOUR, [GateEntryTime])
                ORDER BY Hour";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new HourlyThroughput
                {
                    Hour = reader.GetInt32(0),
                    Count = reader.GetInt32(1)
                });
            }
            return list;
        }
    }
}
