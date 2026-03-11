-- =============================================
-- Stored Procedure: GetUserById (Updated)
-- Description: Gets user details by UserId including PhoneNumber
-- =============================================
-- Run this script to update the stored procedure in your database

CREATE OR ALTER PROCEDURE [dbo].[GetUserById]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        UserId,
        FullName,
        Email,
        PhoneNumber,
        Role,
        Gender,
        IsActive
    FROM Users
    WHERE UserId = @UserId AND Role != 'Admin';
END
GO
