-- =============================================
-- Migration: AddBusIdAndOperatorResponseToFeedback
-- Description: Adds BusId column and operator response fields to Feedbacks table
-- Run this script after stopping the application
-- =============================================

-- Add BusId column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Feedbacks]') AND name = 'BusId')
BEGIN
    ALTER TABLE [dbo].[Feedbacks]
    ADD [BusId] INT NULL;
END
GO

-- Add OperatorResponse column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Feedbacks]') AND name = 'OperatorResponse')
BEGIN
    ALTER TABLE [dbo].[Feedbacks]
    ADD [OperatorResponse] NVARCHAR(1000) NULL;
END
GO

-- Add RespondedAt column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Feedbacks]') AND name = 'RespondedAt')
BEGIN
    ALTER TABLE [dbo].[Feedbacks]
    ADD [RespondedAt] DATETIME2 NULL;
END
GO

-- Update existing feedbacks to set BusId from their booking's route
UPDATE f
SET f.BusId = r.BusId
FROM [dbo].[Feedbacks] f
INNER JOIN [dbo].[Bookings] b ON f.BookingId = b.BookingId
INNER JOIN [dbo].[Routes] r ON b.RouteId = r.RouteId
WHERE f.BusId IS NULL;
GO

-- Make BusId NOT NULL after populating data
ALTER TABLE [dbo].[Feedbacks]
ALTER COLUMN [BusId] INT NOT NULL;
GO

-- Add foreign key constraint
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Feedbacks_Buses_BusId')
BEGIN
    ALTER TABLE [dbo].[Feedbacks]
    ADD CONSTRAINT [FK_Feedbacks_Buses_BusId] 
    FOREIGN KEY ([BusId]) REFERENCES [dbo].[Buses]([BusId])
    ON DELETE NO ACTION;
END
GO

-- Create index for BusId for faster queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Feedbacks_BusId')
BEGIN
    CREATE INDEX [IX_Feedbacks_BusId] ON [dbo].[Feedbacks] ([BusId]);
END
GO

PRINT 'Migration completed successfully!';
GO
