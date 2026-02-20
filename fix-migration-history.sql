-- Fix Migration History
-- This script inserts the missing migration record into __EFMigrationsHistory
-- Run this script against your database to fix the migration history sync issue

USE [OjisanBackendDb];
GO

-- Check if the migration history table exists, create it if it doesn't
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
    PRINT 'Created __EFMigrationsHistory table';
END
ELSE
BEGIN
    PRINT '__EFMigrationsHistory table already exists';
END
GO

-- Insert the migration record if it doesn't exist
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20260220145302_ojisan-db')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES ('20260220145302_ojisan-db', '10.0.0');
    PRINT 'Inserted migration record: 20260220145302_ojisan-db';
END
ELSE
BEGIN
    PRINT 'Migration record already exists: 20260220145302_ojisan-db';
END
GO

-- Verify the migration history
SELECT * FROM [__EFMigrationsHistory];
GO
