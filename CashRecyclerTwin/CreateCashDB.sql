
USE CashManagementDB;
GO

-- 1. Clean Up (Drop Tables to force Schema Re-creation)
IF OBJECT_ID('Fact_Transactions', 'U') IS NOT NULL DROP TABLE Fact_Transactions;
IF OBJECT_ID('Fact_Cassettes', 'U') IS NOT NULL DROP TABLE Fact_Cassettes;
IF OBJECT_ID('Dim_Recyclers', 'U') IS NOT NULL DROP TABLE Dim_Recyclers;
IF OBJECT_ID('Dim_Locations', 'U') IS NOT NULL DROP TABLE Dim_Locations;
IF OBJECT_ID('Dim_Regions', 'U') IS NOT NULL DROP TABLE Dim_Regions;
GO

-- 2. Schema Definition (Tables will be created fresh)

-- 3. Ensure Tables Exist (If not already)
IF OBJECT_ID('Dim_Regions', 'U') IS NULL
CREATE TABLE Dim_Regions (
    RegionId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);

IF OBJECT_ID('Dim_Locations', 'U') IS NULL
CREATE TABLE Dim_Locations (
    LocationId INT IDENTITY(1,1) PRIMARY KEY,
    RegionId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Address NVARCHAR(200),
    Latitude DECIMAL(9,6),
    Longitude DECIMAL(9,6),
    FOREIGN KEY (RegionId) REFERENCES Dim_Regions(RegionId)
);

IF OBJECT_ID('Dim_Recyclers', 'U') IS NULL
CREATE TABLE Dim_Recyclers (
    RecyclerId INT IDENTITY(1,1) PRIMARY KEY,
    LocationId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    SerialNumber NVARCHAR(50),
    Model NVARCHAR(50) DEFAULT 'Glory-RB-200',
    Status NVARCHAR(20) DEFAULT 'Online', -- Online, Offline, Error
    FOREIGN KEY (LocationId) REFERENCES Dim_Locations(LocationId)
);

IF OBJECT_ID('Fact_Cassettes', 'U') IS NULL
CREATE TABLE Fact_Cassettes (
    RecyclerId INT NOT NULL,
    CassetteIndex INT NOT NULL, -- 1-6 usually
    Type NVARCHAR(20) NOT NULL, -- 'Recycle', 'Deposit', 'Reject', 'Dispense'
    Denomination DECIMAL(10,2) NOT NULL,
    CurrentCount INT DEFAULT 0,
    MaxCapacity INT DEFAULT 2000,
    Status NVARCHAR(20) DEFAULT 'OK', -- OK, Low, Full
    PRIMARY KEY (RecyclerId, CassetteIndex),
    FOREIGN KEY (RecyclerId) REFERENCES Dim_Recyclers(RecyclerId)
);

IF OBJECT_ID('Fact_Transactions', 'U') IS NULL
CREATE TABLE Fact_Transactions (
    TransactionId INT IDENTITY(1,1) PRIMARY KEY,
    RecyclerId INT NOT NULL,
    Timestamp DATETIME DEFAULT GETDATE(),
    Type NVARCHAR(50) NOT NULL, -- 'Deposit', 'Withdrawal', 'CIT Pickup', 'CIT Delivery'
    TotalAmount DECIMAL(18,2) NOT NULL,
    UserId NVARCHAR(50),
    FOREIGN KEY (RecyclerId) REFERENCES Dim_Recyclers(RecyclerId)
);
GO

-- 4. Seed Data
INSERT INTO Dim_Regions (Name) VALUES ('North East'), ('South West'), ('Central');

-- Region 1 (North East)
INSERT INTO Dim_Locations (RegionId, Name, Address, Latitude, Longitude) VALUES 
(1, 'Store NY-1 (New York)', '123 Broadway, NY', 40.7128, -74.0060),
(1, 'Store MA-1 (Boston)', '456 State St, MA', 42.3601, -71.0589),
(1, 'Store PA-1 (Philadelphia)', '789 Market St, PA', 39.9526, -75.1652);

-- Region 2 (South West)
INSERT INTO Dim_Locations (RegionId, Name, Address, Latitude, Longitude) VALUES 
(2, 'Store AZ-1 (Phoenix)', '101 Desert Blvd, AZ', 33.4484, -112.0740),
(2, 'Store NV-1 (Las Vegas)', '202 Strip Blvd, NV', 36.1699, -115.1398),
(2, 'Store CA-1 (San Diego)', '303 Ocean Dr, CA', 32.7157, -117.1611);

-- Region 3 (Central)
INSERT INTO Dim_Locations (RegionId, Name, Address, Latitude, Longitude) VALUES 
(3, 'Store IL-1 (Chicago)', '404 Windy City Way, IL', 41.8781, -87.6298),
(3, 'Store TX-1 (Dallas)', '505 Lone Star Ln, TX', 32.7767, -96.7970),
(3, 'Store CO-1 (Denver)', '606 Mile High Ct, CO', 39.7392, -104.9903);


-- Fix: Explicit Cast length for GUID to avoid overflow
INSERT INTO Dim_Recyclers (LocationId, Name, SerialNumber)
SELECT LocationId, 'Recycler-' + CAST(LocationId AS NVARCHAR), 'SN-' + CAST(NEWID() AS NVARCHAR(50))
FROM Dim_Locations;

INSERT INTO Fact_Cassettes (RecyclerId, CassetteIndex, Type, Denomination, CurrentCount, MaxCapacity)
SELECT RecyclerId, 1, 'Recycle', 100.00, 500, 2000 FROM Dim_Recyclers
UNION ALL
SELECT RecyclerId, 2, 'Recycle', 50.00, 200, 2000 FROM Dim_Recyclers 
UNION ALL
SELECT RecyclerId, 3, 'Recycle', 20.00, 1200, 2000 FROM Dim_Recyclers
UNION ALL
SELECT RecyclerId, 4, 'Recycle', 10.00, 800, 2500 FROM Dim_Recyclers
UNION ALL
SELECT RecyclerId, 5, 'Recycle', 5.00, 500, 3000 FROM Dim_Recyclers
UNION ALL
SELECT RecyclerId, 6, 'Recycle', 1.00, 1500, 3000 FROM Dim_Recyclers;

INSERT INTO Fact_Cassettes (RecyclerId, CassetteIndex, Type, Denomination, CurrentCount, MaxCapacity)
SELECT RecyclerId, 7, 'Reject', 0.00, 0, 500 FROM Dim_Recyclers;

UPDATE Fact_Cassettes SET CurrentCount = CAST(CurrentCount * (0.8 + (RAND(CHECKSUM(NEWID())) * 0.4)) AS INT);

SELECT * FROM Dim_Recyclers;
