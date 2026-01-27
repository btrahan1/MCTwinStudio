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

-- 2. Insert 50 Real US Cities
-- Format: RegionId (1=NE, 2=SW, 3=Central), Name, Lat, Lon
CREATE TABLE #TempLocations (RegionId INT, CityName NVARCHAR(100), Lat DECIMAL(9,6), Lon DECIMAL(9,6));

INSERT INTO #TempLocations (RegionId, CityName, Lat, Lon) VALUES
(1, 'New York (NY)', 40.7128, -74.0060),
(1, 'Boston (MA)', 42.3601, -71.0589),
(1, 'Philadelphia (PA)', 39.9526, -75.1652),
(1, 'Washington (DC)', 38.9072, -77.0369),
(1, 'Miami (FL)', 25.7617, -80.1918),
(1, 'Atlanta (GA)', 33.7490, -84.3880),
(1, 'Charlotte (NC)', 35.2271, -80.8431),
(1, 'Orlando (FL)', 28.5383, -81.3792),
(1, 'Pittsburgh (PA)', 40.4406, -79.9959),
(1, 'Buffalo (NY)', 42.8864, -78.8784),
(1, 'Richmond (VA)', 37.5407, -77.4360),
(1, 'Baltimore (MD)', 39.2904, -76.6122),
(1, 'Jacksonville (FL)', 30.3322, -81.6557),
(1, 'Tampa (FL)', 27.9506, -82.4572),
(1, 'Providence (RI)', 41.8240, -71.4128),

(2, 'Los Angeles (CA)', 34.0522, -118.2437),
(2, 'San Francisco (CA)', 37.7749, -122.4194),
(2, 'Seattle (WA)', 47.6062, -122.3321),
(2, 'Phoenix (AZ)', 33.4484, -112.0740),
(2, 'Las Vegas (NV)', 36.1699, -115.1398),
(2, 'San Diego (CA)', 32.7157, -117.1611),
(2, 'Portland (OR)', 45.5152, -122.6784),
(2, 'Salt Lake City (UT)', 40.7608, -111.8910),
(2, 'Denver (CO)', 39.7392, -104.9903),
(2, 'Albuquerque (NM)', 35.0844, -106.6504),
(2, 'Boise (ID)', 43.6150, -116.2023),
(2, 'Sacramento (CA)', 38.5816, -121.4944),
(2, 'Tucson (AZ)', 32.2226, -110.9747),
(2, 'Fresno (CA)', 36.7378, -119.7871),
(2, 'Spokane (WA)', 47.6588, -117.4260),

(3, 'Chicago (IL)', 41.8781, -87.6298),
(3, 'Houston (TX)', 29.7604, -95.3698),
(3, 'Dallas (TX)', 32.7767, -96.7970),
(3, 'Austin (TX)', 30.2672, -97.7431),
(3, 'Nashville (TN)', 36.1627, -86.7816),
(3, 'Detroit (MI)', 42.3314, -83.0458),
(3, 'Minneapolis (MN)', 44.9778, -93.2650),
(3, 'St. Louis (MO)', 38.6270, -90.1994),
(3, 'Kansas City (MO)', 39.0997, -94.5786),
(3, 'New Orleans (LA)', 29.9511, -90.0715),
(3, 'Cleveland (OH)', 41.4993, -81.6944),
(3, 'Cincinnati (OH)', 39.1031, -84.5120),
(3, 'Indianapolis (IN)', 39.7684, -86.1581),
(3, 'Milwaukee (WI)', 43.0389, -87.9065),
(3, 'Oklahoma City (OK)', 35.4676, -97.5164),
(3, 'San Antonio (TX)', 29.4241, -98.4936),
(3, 'Memphis (TN)', 35.1495, -90.0490),
(3, 'Louisville (KY)', 38.2527, -85.7585),
(3, 'Columbus (OH)', 39.9612, -82.9988),
(3, 'Omaha (NE)', 41.2565, -95.9345);


-- 3. Populate Tables
DECLARE @Reg INT, @Name NVARCHAR(100), @L DECIMAL(9,6), @Ln DECIMAL(9,6);

DECLARE loc_cursor CURSOR FOR SELECT RegionId, CityName, Lat, Lon FROM #TempLocations;
OPEN loc_cursor;
FETCH NEXT FROM loc_cursor INTO @Reg, @Name, @L, @Ln;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Insert Location
    INSERT INTO Dim_Locations (RegionId, Name, Address, Latitude, Longitude)
    VALUES (@Reg, 'Store ' + @Name, '1 Main St', @L, @Ln);

    DECLARE @LocId INT = SCOPE_IDENTITY();

    -- Insert Recycler
    INSERT INTO Dim_Recyclers (LocationId, Name, SerialNumber, Status)
    VALUES (@LocId, 'Recycler-' + CAST(@LocId AS NVARCHAR), 'SN-' + CAST(NEWID() AS NVARCHAR(50)), 'Online');

    DECLARE @RecId INT = SCOPE_IDENTITY();

    -- Insert Cassettes (6 Cassettes)
    INSERT INTO Fact_Cassettes (RecyclerId, CassetteIndex, Type, Denomination, CurrentCount, MaxCapacity) VALUES
    (@RecId, 1, 'Recycle', 100.00, CAST(RAND() * 2000 AS INT), 2000),
    (@RecId, 2, 'Recycle', 50.00,  CAST(RAND() * 2000 AS INT), 2000),
    (@RecId, 3, 'Recycle', 20.00,  CAST(RAND() * 2000 AS INT), 2000),
    (@RecId, 4, 'Recycle', 10.00,  CAST(RAND() * 2500 AS INT), 2500),
    (@RecId, 5, 'Recycle', 5.00,   CAST(RAND() * 3000 AS INT), 3000),
    (@RecId, 6, 'Recycle', 1.00,   CAST(RAND() * 3000 AS INT), 3000);

    FETCH NEXT FROM loc_cursor INTO @Reg, @Name, @L, @Ln;
END

CLOSE loc_cursor;
DEALLOCATE loc_cursor;
DROP TABLE #TempLocations;

PRINT 'Successfully populated 50 Real US Locations.';
GO
