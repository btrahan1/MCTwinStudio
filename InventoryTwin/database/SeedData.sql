USE InventoryDB;
GO

-- Clear existing data
TRUNCATE TABLE InventoryItems;
GO

-- Seed data for a small warehouse section (2 Racks, 3 Shelves, 5 Bins)
INSERT INTO InventoryItems (RackIndex, ShelfLevel, BinIndex, ProductSku, Quantity)
VALUES 
(0, 0, 0, 'WIDGET-A', 10),
(0, 0, 1, 'WIDGET-A', 5),
(0, 0, 2, 'WIDGET-B', 8),
(0, 0, 3, 'GADGET-X', 12),
(0, 0, 4, 'GADGET-Y', 3),

(0, 1, 0, 'WIDGET-A', 15),
(0, 1, 1, 'WIDGET-C', 7),
(0, 1, 2, 'GADGET-Z', 20),
(0, 1, 3, 'PART-99', 4),
(0, 1, 4, 'PART-88', 6),

(0, 2, 0, 'SPOOL-1', 2),
(0, 2, 1, 'SPOOL-2', 9),
(0, 2, 2, 'WIRE-R', 50),
(0, 2, 3, 'WIRE-G', 45),
(0, 2, 4, 'WIRE-B', 60),

(1, 0, 0, 'BOX-M', 100),
(1, 0, 1, 'BOX-L', 50),
(1, 0, 2, 'TAPE-1', 200),
(1, 0, 3, 'TAPE-2', 150),
(1, 0, 4, 'LABEL-1', 500);
GO
