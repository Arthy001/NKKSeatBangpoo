USE [NHKBangpooSeat];
GO

-- Drop existing foreign keys to allow dropping tables
IF OBJECT_ID('dbo.WorkOrderDetail', 'F') IS NOT NULL ALTER TABLE [dbo].[WorkOrderDetail] DROP CONSTRAINT FK_WorkOrderDetail_ProductMaster;
IF OBJECT_ID('dbo.TagBindingTransaction', 'F') IS NOT NULL ALTER TABLE [dbo].[TagBindingTransaction] DROP CONSTRAINT FK_TagBinding_ProductMaster;
IF OBJECT_ID('dbo.WorkOrderDetail', 'F') IS NOT NULL ALTER TABLE [dbo].[WorkOrderDetail] DROP CONSTRAINT FK_WorkOrderDetail_Header;

-- Drop tables
IF OBJECT_ID('dbo.TagBindingTransaction', 'U') IS NOT NULL DROP TABLE dbo.TagBindingTransaction;
IF OBJECT_ID('dbo.WorkOrderDetail', 'U') IS NOT NULL DROP TABLE dbo.WorkOrderDetail;
IF OBJECT_ID('dbo.WorkOrderHeader', 'U') IS NOT NULL DROP TABLE dbo.WorkOrderHeader;
IF OBJECT_ID('dbo.ProductMaster', 'U') IS NOT NULL DROP TABLE dbo.ProductMaster;
IF OBJECT_ID('dbo.RfidCardMaster', 'U') IS NOT NULL DROP TABLE dbo.RfidCardMaster;
GO

-- 1. ProductMaster
CREATE TABLE [dbo].[ProductMaster] (
    [NHKPartNo]       NVARCHAR(100) NOT NULL,
    [KanbanCode]      NVARCHAR(100) NOT NULL,
    [PartCode]        AS ([NHKPartNo] + [KanbanCode]) PERSISTED,
    [IsuzuPartNo]     NVARCHAR(100),
    [PartName]        NVARCHAR(255),
    [Model]           NVARCHAR(100),
    [Type]            NVARCHAR(100),
    [Cover]           NVARCHAR(100),
    [Code]            NVARCHAR(100),
    [BarcodePI]       NVARCHAR(100),
    [LineID]          NVARCHAR(100),
    [SeatTypeID]      NVARCHAR(100),
    [MainPartFlag]    NVARCHAR(100),
    [WeldingBackNo]   NVARCHAR(100),
    [WeldingCusionNo] NVARCHAR(100),
    [WeldBack]        NVARCHAR(100),
    [WeldCusion]      NVARCHAR(100),
    [Sawing]          NVARCHAR(100),
    [IsActive]        BIT DEFAULT 1,
    CONSTRAINT PK_ProductMaster PRIMARY KEY ([NHKPartNo], [KanbanCode])
);
GO

-- 2. WorkOrderHeader
CREATE TABLE [dbo].[WorkOrderHeader] (
    [OrderNo]         NVARCHAR(100) PRIMARY KEY,
    [WorkCenter]      NVARCHAR(100),
    [DueDate]         DATETIME,
    [OrderDate]       DATETIME,
    [ImportTimestamp] DATETIME DEFAULT GETDATE(),
    [Status]          NVARCHAR(50) DEFAULT 'Open'
);
GO

-- 3. WorkOrderDetail
CREATE TABLE [dbo].[WorkOrderDetail] (
    [DetailID]      INT IDENTITY(1,1) PRIMARY KEY,
    [OrderNo]       NVARCHAR(100) NOT NULL,
    [NHKPartNo]     NVARCHAR(100) NOT NULL,
    [KanbanNo]      NVARCHAR(100) NOT NULL,
    [LineNo]        INT,
    [TargetQty]     INT DEFAULT 0,
    [RegisteredQty] INT DEFAULT 0,
    [Status]        NVARCHAR(50) DEFAULT 'Pending',
    CONSTRAINT FK_WorkOrderDetail_Header FOREIGN KEY (OrderNo) REFERENCES WorkOrderHeader(OrderNo) ON DELETE CASCADE,
    CONSTRAINT FK_WorkOrderDetail_ProductMaster FOREIGN KEY (NHKPartNo, KanbanNo) REFERENCES ProductMaster(NHKPartNo, KanbanCode)
);
GO

-- 4. RfidCardMaster
CREATE TABLE [dbo].[RfidCardMaster] (
    [Id]            INT IDENTITY(1,1) PRIMARY KEY,
    [TagEPC]        NVARCHAR(100) UNIQUE NOT NULL,
    [CardNo]        NVARCHAR(50),
    [CurrentStatus] NVARCHAR(50) DEFAULT 'Available',
    [RegisterTime]  DATETIME DEFAULT GETDATE(),
    [FateInTime]    DATETIME
);
GO

-- 5. TagBindingTransaction
CREATE TABLE [dbo].[TagBindingTransaction] (
    [TransactionID]  INT IDENTITY(1,1) PRIMARY KEY,
    [TagEPC]         NVARCHAR(100) NOT NULL,
    [OrderNo]        NVARCHAR(100),
    [NHKPartNo]      NVARCHAR(100) NOT NULL,
    [KanbanNo]       NVARCHAR(100) NOT NULL,
    [QtyPerSet]      INT,
    [BindingTime]    DATETIME DEFAULT GETDATE(),
    [GateEntryTime]  DATETIME,
    [IsExported]     BIT DEFAULT 0,
    [ExportFileName] NVARCHAR(255),
    CONSTRAINT FK_TagBinding_ProductMaster FOREIGN KEY (NHKPartNo, KanbanNo) REFERENCES ProductMaster(NHKPartNo, KanbanCode)
);
GO
