BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724075318_AddHomeContentAndProductFlags'
)
BEGIN
    ALTER TABLE [Products] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724075318_AddHomeContentAndProductFlags'
)
BEGIN
    ALTER TABLE [Products] ADD [IsBestseller] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724075318_AddHomeContentAndProductFlags'
)
BEGIN
    ALTER TABLE [Products] ADD [IsFeatured] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724075318_AddHomeContentAndProductFlags'
)
BEGIN
    ALTER TABLE [Products] ADD [OldPrice] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724075318_AddHomeContentAndProductFlags'
)
BEGIN
    CREATE TABLE [HomeBlocks] (
        [Id] int NOT NULL IDENTITY,
        [Section] nvarchar(max) NOT NULL,
        [Slug] nvarchar(max) NULL,
        [Eyebrow] nvarchar(max) NULL,
        [Title] nvarchar(max) NULL,
        [Subtitle] nvarchar(max) NULL,
        [ButtonText] nvarchar(max) NULL,
        [LinkUrl] nvarchar(max) NULL,
        [ImageUrl] nvarchar(max) NOT NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [Size] nvarchar(max) NULL,
        [Theme] nvarchar(max) NULL,
        CONSTRAINT [PK_HomeBlocks] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724075318_AddHomeContentAndProductFlags'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724075318_AddHomeContentAndProductFlags', N'10.0.3');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Name');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Products] ALTER COLUMN [Name] nvarchar(150) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD [AvailableColors] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD [AvailableSizes] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD [Brand] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD [CategoryID] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD [DeliveryInfo] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD [FreeShipping] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD [GSTOverridePercentage] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD [Rating] float NOT NULL DEFAULT 0.0E0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD [SKU] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD [Slug] nvarchar(150) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD [SpecificationsJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD [SubCategoryID] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD [Tags] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD [TotalReviews] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Payments] ADD [RazorpayOrderId] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Payments] ADD [RazorpayPaymentId] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Payments] ADD [RazorpaySignature] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Orders] ADD [CouponCode] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Orders] ADD [DiscountAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Orders] ADD [GSTTotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Orders] ADD [OrderNumber] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Orders] ADD [PaymentStatus] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Orders] ADD [RazorpayOrderId] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Orders] ADD [RazorpayPaymentId] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Orders] ADD [RazorpaySignature] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Orders] ADD [ShippingCharge] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Orders] ADD [SubTotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [GSTAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [GSTPercentage] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [UnitPrice] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [VariantID] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [VariantName] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Carts] ADD [VariantID] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [LogID] int NOT NULL IDENTITY,
        [AdminEmail] nvarchar(100) NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityID] nvarchar(max) NULL,
        [Details] nvarchar(max) NULL,
        [IpAddress] nvarchar(max) NULL,
        [Timestamp] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([LogID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE TABLE [Categories] (
        [CategoryID] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(100) NOT NULL,
        [GSTPercentage] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE TABLE [Coupons] (
        [CouponID] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Description] nvarchar(250) NOT NULL,
        [DiscountType] nvarchar(20) NOT NULL,
        [DiscountValue] decimal(18,2) NOT NULL,
        [MinOrderAmount] decimal(18,2) NOT NULL,
        [MaxDiscountCap] decimal(18,2) NULL,
        [UsageType] nvarchar(30) NOT NULL,
        [TotalUsageLimit] int NULL,
        [PerUserUsageLimit] int NULL,
        [TimesUsed] int NOT NULL,
        [StartDate] datetime2 NULL,
        [EndDate] datetime2 NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Coupons] PRIMARY KEY ([CouponID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE TABLE [CustomerAddresses] (
        [AddressID] int NOT NULL IDENTITY,
        [CustomerID] int NOT NULL,
        [FullName] nvarchar(100) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [AddressLine1] nvarchar(200) NOT NULL,
        [AddressLine2] nvarchar(max) NULL,
        [City] nvarchar(50) NOT NULL,
        [State] nvarchar(50) NOT NULL,
        [Pincode] nvarchar(10) NOT NULL,
        [AddressType] nvarchar(20) NOT NULL,
        [IsDefault] bit NOT NULL,
        CONSTRAINT [PK_CustomerAddresses] PRIMARY KEY ([AddressID]),
        CONSTRAINT [FK_CustomerAddresses_Customers_CustomerID] FOREIGN KEY ([CustomerID]) REFERENCES [Customers] ([CustomerID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE TABLE [ProductReviews] (
        [ReviewID] int NOT NULL IDENTITY,
        [ProductID] int NOT NULL,
        [CustomerID] int NOT NULL,
        [Rating] int NOT NULL,
        [Headline] nvarchar(100) NOT NULL,
        [Comment] nvarchar(1000) NOT NULL,
        [ImageUrl] nvarchar(max) NULL,
        [IsApproved] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProductReviews] PRIMARY KEY ([ReviewID]),
        CONSTRAINT [FK_ProductReviews_Customers_CustomerID] FOREIGN KEY ([CustomerID]) REFERENCES [Customers] ([CustomerID]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductReviews_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([ProductID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE TABLE [ProductVariants] (
        [VariantID] int NOT NULL IDENTITY,
        [ProductID] int NOT NULL,
        [VariantName] nvarchar(100) NOT NULL,
        [SKU] nvarchar(50) NULL,
        [Price] decimal(18,2) NOT NULL,
        [OldPrice] decimal(18,2) NULL,
        [Stock] int NOT NULL,
        [IsDefault] bit NOT NULL,
        [SpecificationsJson] nvarchar(max) NULL,
        [AttributesJson] nvarchar(max) NULL,
        [ColorName] nvarchar(max) NULL,
        [ColorHex] nvarchar(max) NULL,
        CONSTRAINT [PK_ProductVariants] PRIMARY KEY ([VariantID]),
        CONSTRAINT [FK_ProductVariants_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([ProductID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE TABLE [ServiceablePincodes] (
        [Id] int NOT NULL IDENTITY,
        [Pincode] nvarchar(10) NOT NULL,
        [City] nvarchar(50) NOT NULL,
        [State] nvarchar(50) NOT NULL,
        [EstimatedDays] int NOT NULL,
        [IsCODAvailable] bit NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ServiceablePincodes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE TABLE [ShippingSettings] (
        [SettingID] int NOT NULL IDENTITY,
        [FreeShippingThreshold] decimal(18,2) NOT NULL,
        [FlatShippingRate] decimal(18,2) NOT NULL,
        [ShippingInfoText] nvarchar(200) NOT NULL,
        CONSTRAINT [PK_ShippingSettings] PRIMARY KEY ([SettingID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE TABLE [SiteSettings] (
        [SettingID] int NOT NULL IDENTITY,
        [StoreName] nvarchar(100) NOT NULL,
        [ContactEmail] nvarchar(100) NOT NULL,
        [ContactPhone] nvarchar(50) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [AuthorizedSignatureUrl] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_SiteSettings] PRIMARY KEY ([SettingID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE TABLE [SubCategories] (
        [SubCategoryID] int NOT NULL IDENTITY,
        [CategoryID] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Slug] nvarchar(100) NOT NULL,
        [GSTPercentage] decimal(18,2) NULL,
        CONSTRAINT [PK_SubCategories] PRIMARY KEY ([SubCategoryID]),
        CONSTRAINT [FK_SubCategories_Categories_CategoryID] FOREIGN KEY ([CategoryID]) REFERENCES [Categories] ([CategoryID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE TABLE [ProductMediaFiles] (
        [MediaID] int NOT NULL IDENTITY,
        [ProductID] int NOT NULL,
        [VariantID] int NULL,
        [MediaType] nvarchar(20) NOT NULL,
        [MediaUrl] nvarchar(500) NOT NULL,
        [FileSize] bigint NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_ProductMediaFiles] PRIMARY KEY ([MediaID]),
        CONSTRAINT [FK_ProductMediaFiles_ProductVariants_VariantID] FOREIGN KEY ([VariantID]) REFERENCES [ProductVariants] ([VariantID]),
        CONSTRAINT [FK_ProductMediaFiles_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([ProductID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE TABLE [Wishlists] (
        [WishlistID] int NOT NULL IDENTITY,
        [CustomerID] int NOT NULL,
        [ProductID] int NOT NULL,
        [VariantID] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Wishlists] PRIMARY KEY ([WishlistID]),
        CONSTRAINT [FK_Wishlists_Customers_CustomerID] FOREIGN KEY ([CustomerID]) REFERENCES [Customers] ([CustomerID]) ON DELETE CASCADE,
        CONSTRAINT [FK_Wishlists_ProductVariants_VariantID] FOREIGN KEY ([VariantID]) REFERENCES [ProductVariants] ([VariantID]),
        CONSTRAINT [FK_Wishlists_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([ProductID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE TABLE [CategoryFilterAttributes] (
        [AttributeID] int NOT NULL IDENTITY,
        [CategoryID] int NULL,
        [SubCategoryID] int NULL,
        [AttributeName] nvarchar(100) NOT NULL,
        [AttributeType] nvarchar(50) NOT NULL,
        [OptionsJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_CategoryFilterAttributes] PRIMARY KEY ([AttributeID]),
        CONSTRAINT [FK_CategoryFilterAttributes_Categories_CategoryID] FOREIGN KEY ([CategoryID]) REFERENCES [Categories] ([CategoryID]),
        CONSTRAINT [FK_CategoryFilterAttributes_SubCategories_SubCategoryID] FOREIGN KEY ([SubCategoryID]) REFERENCES [SubCategories] ([SubCategoryID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryID] ON [Products] ([CategoryID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_Products_SubCategoryID] ON [Products] ([SubCategoryID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_OrderItems_VariantID] ON [OrderItems] ([VariantID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_Carts_VariantID] ON [Carts] ([VariantID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_CategoryFilterAttributes_CategoryID] ON [CategoryFilterAttributes] ([CategoryID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_CategoryFilterAttributes_SubCategoryID] ON [CategoryFilterAttributes] ([SubCategoryID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_CustomerAddresses_CustomerID] ON [CustomerAddresses] ([CustomerID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_ProductMediaFiles_ProductID] ON [ProductMediaFiles] ([ProductID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_ProductMediaFiles_VariantID] ON [ProductMediaFiles] ([VariantID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_ProductReviews_CustomerID] ON [ProductReviews] ([CustomerID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_ProductReviews_ProductID] ON [ProductReviews] ([ProductID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_ProductVariants_ProductID] ON [ProductVariants] ([ProductID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_SubCategories_CategoryID] ON [SubCategories] ([CategoryID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_Wishlists_CustomerID] ON [Wishlists] ([CustomerID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_Wishlists_ProductID] ON [Wishlists] ([ProductID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    CREATE INDEX [IX_Wishlists_VariantID] ON [Wishlists] ([VariantID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Carts] ADD CONSTRAINT [FK_Carts_ProductVariants_VariantID] FOREIGN KEY ([VariantID]) REFERENCES [ProductVariants] ([VariantID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [OrderItems] ADD CONSTRAINT [FK_OrderItems_ProductVariants_VariantID] FOREIGN KEY ([VariantID]) REFERENCES [ProductVariants] ([VariantID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_Categories_CategoryID] FOREIGN KEY ([CategoryID]) REFERENCES [Categories] ([CategoryID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_SubCategories_SubCategoryID] FOREIGN KEY ([SubCategoryID]) REFERENCES [SubCategories] ([SubCategoryID]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725200943_EnhancementTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725200943_EnhancementTables', N'10.0.3');
END;

COMMIT;
GO

