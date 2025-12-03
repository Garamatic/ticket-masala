-- Clear Database for Re-seeding
-- WARNING: This will delete ALL data!

USE [ITProjectDB]
GO

-- Disable all constraints
EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'
GO

-- Delete data from all tables
DELETE FROM [AspNetUserTokens]
DELETE FROM [AspNetUserRoles]
DELETE FROM [AspNetUserLogins]
DELETE FROM [AspNetUserClaims]
DELETE FROM [AspNetRoleClaims]
DELETE FROM [Tickets]
DELETE FROM [Projects]
DELETE FROM [AspNetUsers]
DELETE FROM [AspNetRoles]
GO

-- Re-enable all constraints
EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'
GO

-- Reset identity columns (if any)
-- DBCC CHECKIDENT ('[TableName]', RESEED, 0)

PRINT 'Database cleared successfully. Run the application to reseed.'
