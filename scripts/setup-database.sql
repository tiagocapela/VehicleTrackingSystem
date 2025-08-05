--# scripts/setup-database.sql
-- Vehicle Tracking Database Setup Script

-- Create VehicleTrackingDb Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'VehicleTrackingDb')
BEGIN
    CREATE DATABASE VehicleTrackingDb;
END
GO

USE VehicleTrackingDb;
GO

-- Create Vehicles table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Vehicles' AND xtype='U')
BEGIN
    CREATE TABLE Vehicles (
        Id int IDENTITY(1,1) PRIMARY KEY,
        DeviceId nvarchar(50) NOT NULL,
        VehicleName nvarchar(100) NOT NULL,
        LicensePlate nvarchar(20) NULL,
        DriverName nvarchar(100) NULL,
        CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
        IsActive bit NOT NULL DEFAULT 1,
        CONSTRAINT UK_Vehicles_DeviceId UNIQUE (DeviceId)
    );
    
    CREATE INDEX IX_Vehicles_DeviceId ON Vehicles(DeviceId);
    CREATE INDEX IX_Vehicles_IsActive ON Vehicles(IsActive);
END
GO

-- Create GpsLocations table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='GpsLocations' AND xtype='U')
BEGIN
    CREATE TABLE GpsLocations (
        Id int IDENTITY(1,1) PRIMARY KEY,
        VehicleId int NOT NULL,
        Latitude float NOT NULL,
        Longitude float NOT NULL,
        Speed float NOT NULL DEFAULT 0,
        Course float NOT NULL DEFAULT 0,
        Satellites int NOT NULL DEFAULT 0,
        Timestamp datetime2 NOT NULL,
        RawData nvarchar(1000) NULL,
        CONSTRAINT FK_GpsLocations_Vehicles FOREIGN KEY (VehicleId) REFERENCES Vehicles(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_GpsLocations_VehicleId_Timestamp ON GpsLocations(VehicleId, Timestamp DESC);
    CREATE INDEX IX_GpsLocations_Timestamp ON GpsLocations(Timestamp DESC);
END
GO

-- Create GpsTrackingDb Database for TCP Service
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'GpsTrackingDb')
BEGIN
    CREATE DATABASE GpsTrackingDb;
END
GO

USE GpsTrackingDb;
GO

-- Create GpsData table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='GpsData' AND xtype='U')
BEGIN
    CREATE TABLE GpsData (
        Id int IDENTITY(1,1) PRIMARY KEY,
        DeviceId nvarchar(50) NOT NULL,
        Timestamp datetime2 NOT NULL,
        Latitude float NOT NULL,
        Longitude float NOT NULL,
        Speed float NOT NULL DEFAULT 0,
        Course float NOT NULL DEFAULT 0,
        Satellites int NOT NULL DEFAULT 0,
        IsValid bit NOT NULL DEFAULT 1,
        RawMessage nvarchar(1000) NULL,
        ReceivedAt datetime2 NOT NULL DEFAULT GETUTCDATE()
    );
    
    CREATE INDEX IX_GpsData_DeviceId_Timestamp ON GpsData(DeviceId, Timestamp DESC);
    CREATE INDEX IX_GpsData_ReceivedAt ON GpsData(ReceivedAt DESC);
    CREATE INDEX IX_GpsData_DeviceId_ReceivedAt ON GpsData(DeviceId, ReceivedAt DESC);
END
GO

PRINT 'Database setup completed successfully!';

