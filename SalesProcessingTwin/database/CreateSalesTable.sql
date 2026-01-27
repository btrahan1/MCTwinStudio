USE InventoryDB;
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Sales' AND xtype='U')
BEGIN
    CREATE TABLE Sales (
        Id INT PRIMARY KEY IDENTITY,
        ProductSku NVARCHAR(50) NOT NULL,
        Quantity INT NOT NULL,
        SoldAt DATETIME2 DEFAULT GETDATE(),
        IsProcessed BIT DEFAULT 0 -- Watermark column to track processed state
    );
END
GO
