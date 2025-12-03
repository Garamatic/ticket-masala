-- MANUAL TEST USER CREATION SCRIPT
-- Use this if the automatic seeder doesn't work
-- Run this in SQL Server Management Studio or Azure Data Studio

-- WARNING: This script will DELETE all existing users!
-- Only run this in a development environment

USE [IT-Project2526]
GO

-- Step 1: Delete existing data (optional - comment out if you want to keep existing data)
DELETE FROM AspNetUserRoles;
DELETE FROM AspNetUsers;
GO

-- Step 2: Insert Admin User
-- Password hash for "Admin123!" with ASP.NET Core Identity default settings
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, FirstName, LastName, Phone, Discriminator, Team, [Level])
VALUES 
(NEWID(), 'admin@ticketmasala.com', 'ADMIN@TICKETMASALA.COM', 'admin@ticketmasala.com', 'ADMIN@TICKETMASALA.COM', 1, 
'AQAAAAIAAYagAAAAEFdGKzC7PfxvPbKlQxL3dR8KZ9QJ5XvYdPCF6vYvF3kQZ8vY6XFdPCF6vYvF3kQZ8==', -- You'll need to generate this
NEWID(), NEWID(), '+1-555-0100', 0, 0, NULL, 1, 0, 'John', 'Administrator', '+1-555-0100', 'Employee', 'Management', 0);

-- Note: The above password hash is an example. You need to actually create a user through the application
-- or use the seeder to get proper password hashes.

-- Alternative: Use the /Seed/Index endpoint to create users programmatically
GO

PRINT 'Manual user creation script completed.'
PRINT 'IMPORTANT: The password hashes in this script are examples and will NOT work.'
PRINT 'Please use one of these methods instead:'
PRINT '1. Navigate to https://localhost:XXXX/Seed/TestAccounts in your browser'
PRINT '2. Click the "Run Database Seeder" button'
PRINT '3. Or use the Identity Registration page to create users manually'
GO
