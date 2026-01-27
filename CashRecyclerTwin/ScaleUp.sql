USE CashManagementDB;
GO

SET NOCOUNT ON;

-- 1. Clean Up
DELETE FROM Fact_Transactions;
DELETE FROM Fact_Cassettes;
DELETE FROM Dim_Recyclers;
DELETE FROM Dim_Locations;

DBCC CHECKIDENT ('Dim_Locations', RESEED, 0);
DBCC CHECKIDENT ('Dim_Recyclers', RESEED, 0);

IF NOT EXISTS (SELECT 1 FROM Dim_Regions)
BEGIN
    INSERT INTO Dim_Regions (Name) VALUES ('North East'), ('South West'), ('Central');
END

DECLARE @i INT = 1;
DECLARE @TotalToCreate INT = 50;

WHILE @i <= @TotalToCreate
BEGIN
    DECLARE @RegionSelector INT = CAST(RAND() * 3 AS INT); -- 0, 1, 2
    
    DECLARE @Lat DECIMAL(9,6);
    DECLARE @Lon DECIMAL(9,6);
    DECLARE @RegionId INT;

    -- Define "Safe" Clusters to hit landmass mostly
    IF @RegionSelector = 0 -- West (CA, WA, OR, NV, AZ)
    BEGIN
        SET @RegionId = 2; -- South West
        -- Lat: 33 to 47
        -- Lon: -123 to -110
        SET @Lat = 33.0 + (RAND() * (47.0 - 33.0));
        SET @Lon = -123.0 + (RAND() * (110.0 - 123.0)); -- -123 + (0..13) -> -123 to -110
    END
    ELSE IF @RegionSelector = 1 -- Central / Midwest (TX to IL)
    BEGIN
        SET @RegionId = 3; -- Central
        -- Lat: 30 to 48
        -- Lon: -104 to -88
        SET @Lat = 30.0 + (RAND() * (48.0 - 30.0));
        SET @Lon = -104.0 + (RAND() * (88.0 - 104.0)); -- -104 + (0..16) -> -104 to -88
    END
    ELSE -- East (NY, MA, FL, PA)
    BEGIN
        SET @RegionId = 1; -- North East
        -- Lat: 30 to 45
        -- Lon: -85 to -72
        SET @Lat = 30.0 + (RAND() * (45.0 - 30.0));
        SET @Lon = -85.0 + (RAND() * (72.0 - 85.0)); -- -85 + (0..13) -> -85 to -72
    END

    DECLARE @CityName NVARCHAR(100) = 'Store-' + CAST(@i AS NVARCHAR);

    INSERT INTO Dim_Locations (RegionId, Name, Address, Latitude, Longitude)
    VALUES (@RegionId, @CityName, 'Generated Blvd', @Lat, @Lon);

    DECLARE @LocId INT = SCOPE_IDENTITY();

    INSERT INTO Dim_Recyclers (LocationId, Name, SerialNumber, Status)
    VALUES (@LocId, 'Recycler-' + CAST(@LocId AS NVARCHAR), 'SN-' + CAST(NEWID() AS NVARCHAR(50)), 'Online');

    DECLARE @RecId INT = SCOPE_IDENTITY();

    INSERT INTO Fact_Cassettes (RecyclerId, CassetteIndex, Type, Denomination, CurrentCount, MaxCapacity) VALUES
    (@RecId, 1, 'Recycle', 100.00, CAST(RAND() * 2000 AS INT), 2000),
    (@RecId, 2, 'Recycle', 50.00,  CAST(RAND() * 2000 AS INT), 2000),
    (@RecId, 3, 'Recycle', 20.00,  CAST(RAND() * 2000 AS INT), 2000),
    (@RecId, 4, 'Recycle', 10.00,  CAST(RAND() * 2500 AS INT), 2500),
    (@RecId, 5, 'Recycle', 5.00,   CAST(RAND() * 3000 AS INT), 3000),
    (@RecId, 6, 'Recycle', 1.00,   CAST(RAND() * 3000 AS INT), 3000);

    SET @i = @i + 1;
END

PRINT 'Successfully created 50 clustered locations.';
GO
