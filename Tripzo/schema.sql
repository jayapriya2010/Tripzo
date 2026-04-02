CREATE TABLE [AmenityMaster] (
    [AmenityId] int NOT NULL IDENTITY,
    [AmenityName] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_AmenityMaster] PRIMARY KEY ([AmenityId])
);
GO


CREATE TABLE [ErrorLogs] (
    [LogId] int NOT NULL IDENTITY,
    [Message] nvarchar(max) NOT NULL,
    [StackTrace] nvarchar(max) NOT NULL,
    [Source] nvarchar(max) NOT NULL,
    [Timestamp] datetime2 NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_ErrorLogs] PRIMARY KEY ([LogId])
);
GO


CREATE TABLE [Users] (
    [UserId] int NOT NULL IDENTITY,
    [FullName] nvarchar(150) NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [PhoneNumber] nvarchar(15) NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Role] nvarchar(50) NOT NULL,
    [Gender] nvarchar(20) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId])
);
GO


CREATE TABLE [Buses] (
    [BusId] int NOT NULL IDENTITY,
    [BusName] nvarchar(150) NOT NULL,
    [BusNumber] nvarchar(50) NOT NULL,
    [BusType] nvarchar(50) NOT NULL,
    [Capacity] int NOT NULL,
    [IsActive] bit NOT NULL,
    [OperatorId] int NOT NULL,
    CONSTRAINT [PK_Buses] PRIMARY KEY ([BusId]),
    CONSTRAINT [FK_Buses_Users_OperatorId] FOREIGN KEY ([OperatorId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [BusAmenities] (
    [BusId] int NOT NULL,
    [AmenityId] int NOT NULL,
    CONSTRAINT [PK_BusAmenities] PRIMARY KEY ([BusId], [AmenityId]),
    CONSTRAINT [FK_BusAmenities_AmenityMaster_AmenityId] FOREIGN KEY ([AmenityId]) REFERENCES [AmenityMaster] ([AmenityId]) ON DELETE CASCADE,
    CONSTRAINT [FK_BusAmenities_Buses_BusId] FOREIGN KEY ([BusId]) REFERENCES [Buses] ([BusId]) ON DELETE CASCADE
);
GO


CREATE TABLE [Routes] (
    [RouteId] int NOT NULL IDENTITY,
    [BusId] int NOT NULL,
    [SourceCity] nvarchar(100) NOT NULL,
    [DestCity] nvarchar(100) NOT NULL,
    [BaseFare] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_Routes] PRIMARY KEY ([RouteId]),
    CONSTRAINT [FK_Routes_Buses_BusId] FOREIGN KEY ([BusId]) REFERENCES [Buses] ([BusId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [SeatConfigs] (
    [SeatId] int NOT NULL IDENTITY,
    [BusId] int NOT NULL,
    [SeatNumber] nvarchar(20) NOT NULL,
    [SeatType] nvarchar(50) NOT NULL,
    [AddonFare] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_SeatConfigs] PRIMARY KEY ([SeatId]),
    CONSTRAINT [FK_SeatConfigs_Buses_BusId] FOREIGN KEY ([BusId]) REFERENCES [Buses] ([BusId]) ON DELETE CASCADE
);
GO


CREATE TABLE [BusSchedules] (
    [ScheduleId] int NOT NULL IDENTITY,
    [RouteId] int NOT NULL,
    [BusId] int NOT NULL,
    [ScheduledDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BusSchedules] PRIMARY KEY ([ScheduleId]),
    CONSTRAINT [FK_BusSchedules_Buses_BusId] FOREIGN KEY ([BusId]) REFERENCES [Buses] ([BusId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BusSchedules_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Routes] ([RouteId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [RouteStops] (
    [StopId] int NOT NULL IDENTITY,
    [RouteId] int NOT NULL,
    [CityName] nvarchar(100) NOT NULL,
    [LocationName] nvarchar(200) NOT NULL,
    [StopType] nvarchar(50) NOT NULL,
    [StopOrder] int NOT NULL,
    [ArrivalTime] time NOT NULL,
    [DayOffset] int NOT NULL,
    CONSTRAINT [PK_RouteStops] PRIMARY KEY ([StopId]),
    CONSTRAINT [FK_RouteStops_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Routes] ([RouteId]) ON DELETE CASCADE
);
GO


CREATE TABLE [Bookings] (
    [BookingId] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [RouteId] int NOT NULL,
    [BoardingStopId] int NOT NULL,
    [DroppingStopId] int NOT NULL,
    [JourneyDate] datetime2 NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [BookingDate] datetime2 NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CancellationReason] nvarchar(max) NULL,
    CONSTRAINT [PK_Bookings] PRIMARY KEY ([BookingId]),
    CONSTRAINT [FK_Bookings_RouteStops_BoardingStopId] FOREIGN KEY ([BoardingStopId]) REFERENCES [RouteStops] ([StopId]),
    CONSTRAINT [FK_Bookings_RouteStops_DroppingStopId] FOREIGN KEY ([DroppingStopId]) REFERENCES [RouteStops] ([StopId]),
    CONSTRAINT [FK_Bookings_Routes_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Routes] ([RouteId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Bookings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [BookedSeats] (
    [BookedSeatId] int NOT NULL IDENTITY,
    [BookingId] int NOT NULL,
    [SeatId] int NOT NULL,
    [PassengerName] nvarchar(150) NOT NULL,
    [PassengerAge] int NOT NULL,
    [Gender] nvarchar(20) NOT NULL,
    [PassengerPhone] nvarchar(15) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [CancellationReason] nvarchar(500) NULL,
    CONSTRAINT [PK_BookedSeats] PRIMARY KEY ([BookedSeatId]),
    CONSTRAINT [FK_BookedSeats_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE CASCADE,
    CONSTRAINT [FK_BookedSeats_SeatConfigs_SeatId] FOREIGN KEY ([SeatId]) REFERENCES [SeatConfigs] ([SeatId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Feedbacks] (
    [FeedbackId] int NOT NULL IDENTITY,
    [BookingId] int NOT NULL,
    [UserId] int NOT NULL,
    [BusId] int NOT NULL,
    [Rating] int NOT NULL,
    [Comment] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [OperatorResponse] nvarchar(1000) NULL,
    [RespondedAt] datetime2 NULL,
    CONSTRAINT [PK_Feedbacks] PRIMARY KEY ([FeedbackId]),
    CONSTRAINT [FK_Feedbacks_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Feedbacks_Buses_BusId] FOREIGN KEY ([BusId]) REFERENCES [Buses] ([BusId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Feedbacks_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Payments] (
    [PaymentId] int NOT NULL IDENTITY,
    [BookingId] int NOT NULL,
    [TransactionId] nvarchar(100) NOT NULL,
    [AmountPaid] decimal(18,2) NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [PaymentStatus] nvarchar(50) NOT NULL,
    [RazorpayOrderId] nvarchar(100) NULL,
    [RazorpayPaymentId] nvarchar(100) NULL,
    [RazorpayRefundId] nvarchar(100) NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentId]),
    CONSTRAINT [FK_Payments_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([BookingId]) ON DELETE CASCADE
);
GO


CREATE INDEX [IX_BookedSeats_BookingId] ON [BookedSeats] ([BookingId]);
GO


CREATE INDEX [IX_BookedSeats_SeatId] ON [BookedSeats] ([SeatId]);
GO


CREATE INDEX [IX_Bookings_BoardingStopId] ON [Bookings] ([BoardingStopId]);
GO


CREATE INDEX [IX_Bookings_DroppingStopId] ON [Bookings] ([DroppingStopId]);
GO


CREATE INDEX [IX_Bookings_RouteId] ON [Bookings] ([RouteId]);
GO


CREATE INDEX [IX_Bookings_UserId] ON [Bookings] ([UserId]);
GO


CREATE INDEX [IX_BusAmenities_AmenityId] ON [BusAmenities] ([AmenityId]);
GO


CREATE UNIQUE INDEX [IX_Buses_BusNumber] ON [Buses] ([BusNumber]);
GO


CREATE INDEX [IX_Buses_OperatorId] ON [Buses] ([OperatorId]);
GO


CREATE INDEX [IX_BusSchedules_BusId] ON [BusSchedules] ([BusId]);
GO


CREATE UNIQUE INDEX [IX_BusSchedules_RouteId_BusId_ScheduledDate] ON [BusSchedules] ([RouteId], [BusId], [ScheduledDate]);
GO


CREATE UNIQUE INDEX [IX_Feedbacks_BookingId] ON [Feedbacks] ([BookingId]);
GO


CREATE INDEX [IX_Feedbacks_BusId] ON [Feedbacks] ([BusId]);
GO


CREATE INDEX [IX_Feedbacks_UserId] ON [Feedbacks] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_Payments_BookingId] ON [Payments] ([BookingId]);
GO


CREATE INDEX [IX_Routes_BusId] ON [Routes] ([BusId]);
GO


CREATE INDEX [IX_RouteStops_RouteId] ON [RouteStops] ([RouteId]);
GO


CREATE INDEX [IX_SeatConfigs_BusId] ON [SeatConfigs] ([BusId]);
GO


CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO


