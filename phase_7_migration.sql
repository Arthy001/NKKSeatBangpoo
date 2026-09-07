-- Phase 7 Schema Update: Metadata Upgrade (A-Q)
USE [NHKBangpooSeat];

-- 1. Update ProductMaster
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ProductMaster' AND COLUMN_NAME = 'PartCode')
BEGIN
    ALTER TABLE [ProductMaster] ADD 
        [PartCode] NVARCHAR(100), 
        [Type] NVARCHAR(100), 
        [Cover] NVARCHAR(100), 
        [Code] NVARCHAR(100),
        [BarcodePI] NVARCHAR(100), 
        [LineID] NVARCHAR(100), 
        [SeatTypeID] NVARCHAR(100), 
        [MainPartFlag] NVARCHAR(100),
        [WeldingBackNo] NVARCHAR(100), 
        [WeldingCusionNo] NVARCHAR(100), 
        [WeldBack] NVARCHAR(100), 
        [WeldCusion] NVARCHAR(100), 
        [Sawing] NVARCHAR(100);
END
GO

-- 2. Populate PartCode (NHK + Kanban)
UPDATE [ProductMaster] SET [PartCode] = [NHKPartNo] + ISNULL([KanbanCode], '');
GO

-- 3. Create Unique Index
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UIX_ProductMaster_PartCode')
BEGIN
    CREATE UNIQUE INDEX UIX_ProductMaster_PartCode ON [ProductMaster](PartCode);
END
GO

-- 4. Add KanbanNo to TagBindingTransaction to avoid join ambiguity in the future
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TagBindingTransaction' AND COLUMN_NAME = 'KanbanNo')
BEGIN
    ALTER TABLE [TagBindingTransaction] ADD [KanbanNo] NVARCHAR(100);
END
GO

-- 5. Backward compatibility: populate KanbanNo from existing join if possible (best effort)
UPDATE t SET t.[KanbanNo] = wd.[KanbanNo]
FROM [TagBindingTransaction] t
JOIN [WorkOrderDetail] wd ON t.[OrderNo] = wd.[OrderNo] AND t.[NHKPartNo] = wd.[NHKPartNo]
WHERE t.[KanbanNo] IS NULL;
GO

-- 5. Backward compatibility: populate KanbanNo from existing join if possible (best effort)
UPDATE t SET t.[KanbanNo] = wd.[KanbanNo]
FROM [TagBindingTransaction] t
JOIN [WorkOrderDetail] wd ON t.[OrderNo] = wd.[OrderNo] AND t.[NHKPartNo] = wd.[NHKPartNo]
WHERE t.[KanbanNo] IS NULL;
