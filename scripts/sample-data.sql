--# scripts/sample-data.sql
-- Sample Data for Testing

USE VehicleTrackingDb;
GO

-- Insert sample vehicles
INSERT INTO Vehicles (DeviceId, VehicleName, LicensePlate, DriverName, IsActive,CreatedAt) VALUES
('MV730001', 'Fleet Vehicle 01', 'ABC-1234', 'John Doe', 1,GETUTCDATE()),
('MV730002', 'Fleet Vehicle 02', 'DEF-5678', 'Jane Smith', 1,GETUTCDATE()),
('MV730003', 'Fleet Vehicle 03', 'GHI-9012', 'Mike Johnson', 1,GETUTCDATE()),
('MV730004', 'Fleet Vehicle 04', 'JKL-3456', 'Sarah Wilson', 1,GETUTCDATE()),
('MV730005', 'Fleet Vehicle 05', 'MNO-7890', 'David Brown', 1,GETUTCDATE());


-- Insert sample GPS locations (last 24 hours)
DECLARE @BaseTime datetime2 = DATEADD(hour, -24, GETUTCDATE());
DECLARE @VehicleId int;
DECLARE @Counter int = 0;

-- Vehicle 1 route (Denver to Colorado Springs)
SET @VehicleId = 1;
WHILE @Counter < 24
BEGIN
    INSERT INTO GpsLocations (VehicleId, Latitude, Longitude, Speed, Course, Satellites, Timestamp, RawData)
    VALUES (
        @VehicleId,
        39.7392 + (RAND() - 0.5) * 0.5, -- Denver area with variation
        -104.9903 + (RAND() - 0.5) * 0.5,
        CASE WHEN @Counter % 3 = 0 THEN 0 ELSE 45 + (RAND() * 30) END, -- Stopped every 3rd hour
        RAND() * 360,
        8 + (RAND() * 4),
        DATEADD(hour, @Counter, @BaseTime),
		''
    );
    SET @Counter = @Counter + 1;
END

-- Vehicle 2 route (Moving pattern)
SET @Counter = 0;
SET @VehicleId = 2;
WHILE @Counter < 24
BEGIN
    INSERT INTO GpsLocations (VehicleId, Latitude, Longitude, Speed, Course, Satellites, Timestamp, RawData)
    VALUES (
        @VehicleId,
        39.6612 + (RAND() - 0.5) * 0.3, -- Aurora area
        -104.8197 + (RAND() - 0.5) * 0.3,
        CASE WHEN @Counter % 4 = 0 THEN 0 ELSE 30 + (RAND() * 40) END,
        RAND() * 360,
        7 + (RAND() * 5),
        DATEADD(hour, @Counter, @BaseTime),
		''
    );
    SET @Counter = @Counter + 1;
END

-- Vehicle 3 (Stationary for testing offline status)
INSERT INTO GpsLocations (VehicleId, Latitude, Longitude, Speed, Course, Satellites, Timestamp, RawData)
VALUES (3, 39.7284, -104.9694, 0, 0, 6, DATEADD(hour, -25, GETUTCDATE()),'');

-- Vehicles 4 and 5 with recent data
INSERT INTO GpsLocations (VehicleId, Latitude, Longitude, Speed, Course, Satellites, Timestamp, RawData) VALUES
(4, 39.7547, -105.0178, 55.2, 180, 9, DATEADD(minute, -5, GETUTCDATE()),''),
(5, 39.7019, -104.9675, 0, 0, 8, DATEADD(minute, -2, GETUTCDATE()),'');

USE GpsTrackingDb;
GO

-- Insert sample raw GPS data for TCP service
INSERT INTO GpsData (DeviceId, Timestamp, Latitude, Longitude, Speed, Course, Satellites, IsValid, RawMessage, ReceivedAt) VALUES
('MV730001', DATEADD(minute, -5, GETUTCDATE()), 39.7392, -104.9903, 45.5, 180, 9, 1, '$GPRMC,120000,A,3944.355,N,10459.418,W,24.6,180.0,021224,003.1,W*6A', DATEADD(minute, -5, GETUTCDATE())),
('MV730002', DATEADD(minute, -3, GETUTCDATE()), 39.6612, -104.8197, 32.1, 90, 8, 1, '$GPRMC,120200,A,3939.672,N,10449.182,W,17.3,90.0,021224,003.1,W*6A', DATEADD(minute, -3, GETUTCDATE())),
('MV730004', DATEADD(minute, -1, GETUTCDATE()), 39.7547, -105.0178, 55.2, 225, 10, 1, '$GPRMC,120400,A,3945.282,N,10501.068,W,29.8,225.0,021224,003.1,W*6A', DATEADD(minute, -1, GETUTCDATE())),
('MV730005', GETUTCDATE(), 39.7019, -104.9675, 0, 0, 7, 1, '$GPRMC,120500,A,3942.114,N,10458.050,W,0.0,0.0,021224,003.1,W*6A', GETUTCDATE());

PRINT 'Sample data inserted successfully!';
