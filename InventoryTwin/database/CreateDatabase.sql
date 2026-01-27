
USE master;
GO

IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'InventoryDB')
BEGIN
    CREATE DATABASE InventoryDB;
END
GO

USE InventoryDB;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InventoryItems')
BEGIN
    CREATE TABLE InventoryItems (
        Id INT PRIMARY KEY IDENTITY(1,1),
        RackIndex INT NOT NULL,
        ShelfLevel INT NOT NULL,
        BinIndex INT NOT NULL,
        ProductSku NVARCHAR(50) NOT NULL,
        Quantity INT NOT NULL DEFAULT 0,
        LastUpdated DATETIME2 DEFAULT GETDATE()
    );
END
GO
