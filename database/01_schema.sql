IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [Sku] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [Unit] nvarchar(30) NOT NULL,
        [StandardPrice] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] int NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Products_StandardPrice] CHECK ([StandardPrice] >= 0)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(150) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [Role] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] int NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE TABLE [Vendors] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(20) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [ContactPerson] nvarchar(150) NULL,
        [Email] nvarchar(150) NOT NULL,
        [Phone] nvarchar(30) NULL,
        [Address] nvarchar(500) NULL,
        [Rating] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] int NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Vendors] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Vendors_Rating] CHECK ([Rating] >= 0 AND [Rating] <= 5)
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NULL,
        [Action] nvarchar(100) NOT NULL,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityId] int NULL,
        [OldValues] nvarchar(max) NULL,
        [NewValues] nvarchar(max) NULL,
        [Timestamp] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE TABLE [PurchaseRequests] (
        [Id] int NOT NULL IDENTITY,
        [RequestNumber] nvarchar(30) NOT NULL,
        [RequesterId] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Justification] nvarchar(1000) NULL,
        [Status] nvarchar(30) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [RequiredByDate] datetime2 NULL,
        [SubmittedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] int NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_PurchaseRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseRequests_TotalAmount] CHECK ([TotalAmount] >= 0),
        CONSTRAINT [FK_PurchaseRequests_Users_RequesterId] FOREIGN KEY ([RequesterId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE TABLE [VendorProducts] (
        [Id] int NOT NULL IDENTITY,
        [VendorId] int NOT NULL,
        [ProductId] int NOT NULL,
        [QuotedPrice] decimal(18,2) NOT NULL,
        [LeadTimeDays] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] int NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_VendorProducts] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_VendorProducts_LeadTimeDays] CHECK ([LeadTimeDays] >= 0),
        CONSTRAINT [CK_VendorProducts_QuotedPrice] CHECK ([QuotedPrice] >= 0),
        CONSTRAINT [FK_VendorProducts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_VendorProducts_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE TABLE [AiRecommendationLogs] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseRequestId] int NOT NULL,
        [RequestedBy] int NOT NULL,
        [InputJson] nvarchar(max) NOT NULL,
        [OutputJson] nvarchar(max) NOT NULL,
        [RecommendedVendorId] int NULL,
        [FinalVendorId] int NULL,
        [WasOverridden] bit NOT NULL,
        [OverrideReason] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AiRecommendationLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AiRecommendationLogs_PurchaseRequests_PurchaseRequestId] FOREIGN KEY ([PurchaseRequestId]) REFERENCES [PurchaseRequests] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AiRecommendationLogs_Users_RequestedBy] FOREIGN KEY ([RequestedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AiRecommendationLogs_Vendors_FinalVendorId] FOREIGN KEY ([FinalVendorId]) REFERENCES [Vendors] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AiRecommendationLogs_Vendors_RecommendedVendorId] FOREIGN KEY ([RecommendedVendorId]) REFERENCES [Vendors] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE TABLE [Approvals] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseRequestId] int NOT NULL,
        [ApproverId] int NOT NULL,
        [Decision] nvarchar(30) NOT NULL,
        [Comments] nvarchar(1000) NULL,
        [DecidedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] int NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Approvals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Approvals_PurchaseRequests_PurchaseRequestId] FOREIGN KEY ([PurchaseRequestId]) REFERENCES [PurchaseRequests] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Approvals_Users_ApproverId] FOREIGN KEY ([ApproverId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE TABLE [PurchaseOrders] (
        [Id] int NOT NULL IDENTITY,
        [PoNumber] nvarchar(30) NOT NULL,
        [PurchaseRequestId] int NOT NULL,
        [VendorId] int NOT NULL,
        [OrderDate] datetime2 NOT NULL,
        [PromisedDeliveryDate] datetime2 NOT NULL,
        [DeliveredDate] datetime2 NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] int NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_PurchaseOrders_Dates] CHECK ([PromisedDeliveryDate] >= [OrderDate]),
        CONSTRAINT [CK_PurchaseOrders_TotalAmount] CHECK ([TotalAmount] >= 0),
        CONSTRAINT [FK_PurchaseOrders_PurchaseRequests_PurchaseRequestId] FOREIGN KEY ([PurchaseRequestId]) REFERENCES [PurchaseRequests] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PurchaseOrders_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE TABLE [RequestItems] (
        [Id] int NOT NULL IDENTITY,
        [PurchaseRequestId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] int NOT NULL,
        [EstimatedUnitPrice] decimal(18,2) NOT NULL,
        [LineTotal] decimal(18,2) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] int NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] int NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_RequestItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_RequestItems_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_RequestItems_UnitPrice] CHECK ([EstimatedUnitPrice] >= 0),
        CONSTRAINT [FK_RequestItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RequestItems_PurchaseRequests_PurchaseRequestId] FOREIGN KEY ([PurchaseRequestId]) REFERENCES [PurchaseRequests] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiRecommendationLogs_FinalVendorId] ON [AiRecommendationLogs] ([FinalVendorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiRecommendationLogs_PurchaseRequestId] ON [AiRecommendationLogs] ([PurchaseRequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiRecommendationLogs_RecommendedVendorId] ON [AiRecommendationLogs] ([RecommendedVendorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiRecommendationLogs_RequestedBy] ON [AiRecommendationLogs] ([RequestedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Approvals_ApproverId] ON [Approvals] ([ApproverId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Approvals_PurchaseRequestId] ON [Approvals] ([PurchaseRequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_EntityName_EntityId] ON [AuditLogs] ([EntityName], [EntityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs] ([Timestamp]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_Category] ON [Products] ([Category]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Products_Name] ON [Products] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Products_Sku] ON [Products] ([Sku]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PurchaseOrders_PoNumber] ON [PurchaseOrders] ([PoNumber]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PurchaseOrders_PurchaseRequestId] ON [PurchaseOrders] ([PurchaseRequestId]) WHERE [IsDeleted] = 0 AND [Status] <> ''Cancelled''');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PurchaseOrders_VendorId] ON [PurchaseOrders] ([VendorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PurchaseRequests_RequesterId] ON [PurchaseRequests] ([RequesterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PurchaseRequests_RequestNumber] ON [PurchaseRequests] ([RequestNumber]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PurchaseRequests_Status] ON [PurchaseRequests] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RequestItems_ProductId] ON [RequestItems] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RequestItems_PurchaseRequestId_ProductId] ON [RequestItems] ([PurchaseRequestId], [ProductId]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VendorProducts_ProductId] ON [VendorProducts] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_VendorProducts_VendorId_ProductId] ON [VendorProducts] ([VendorId], [ProductId]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Vendors_Code] ON [Vendors] ([Code]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Vendors_Email] ON [Vendors] ([Email]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Vendors_Name] ON [Vendors] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261001095538_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261001095538_InitialCreate', N'8.0.31');
END;
GO

COMMIT;
GO

