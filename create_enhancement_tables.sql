-- Mark the EnhancementTables migration as applied (since many parts already exist)
-- We'll manually create only the NEW tables that are missing

-- 1. Create ProductReviews table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductReviews')
BEGIN
    CREATE TABLE [ProductReviews] (
        [ReviewID] int NOT NULL IDENTITY(1, 1),
        [ProductID] int NOT NULL,
        [CustomerID] int NOT NULL,
        [Rating] int NOT NULL,
        [Headline] nvarchar(100) NOT NULL,
        [Comment] nvarchar(1000) NOT NULL,
        [ImageUrl] nvarchar(max) NULL,
        [IsApproved] bit NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_ProductReviews] PRIMARY KEY ([ReviewID]),
        CONSTRAINT [FK_ProductReviews_Customers_CustomerID] FOREIGN KEY ([CustomerID]) REFERENCES [Customers] ([CustomerID]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductReviews_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([ProductID]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_ProductReviews_CustomerID] ON [ProductReviews] ([CustomerID]);
    CREATE INDEX [IX_ProductReviews_ProductID] ON [ProductReviews] ([ProductID]);
    PRINT 'Created ProductReviews table';
END
ELSE
    PRINT 'ProductReviews table already exists';

-- 2. Create Wishlists table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Wishlists')
BEGIN
    CREATE TABLE [Wishlists] (
        [WishlistID] int NOT NULL IDENTITY(1, 1),
        [CustomerID] int NOT NULL,
        [ProductID] int NOT NULL,
        [VariantID] int NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Wishlists] PRIMARY KEY ([WishlistID]),
        CONSTRAINT [FK_Wishlists_Customers_CustomerID] FOREIGN KEY ([CustomerID]) REFERENCES [Customers] ([CustomerID]) ON DELETE CASCADE,
        CONSTRAINT [FK_Wishlists_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([ProductID]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_Wishlists_CustomerID] ON [Wishlists] ([CustomerID]);
    CREATE INDEX [IX_Wishlists_ProductID] ON [Wishlists] ([ProductID]);
    PRINT 'Created Wishlists table';
END
ELSE
    PRINT 'Wishlists table already exists';

-- 3. Create ServiceablePincodes table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ServiceablePincodes')
BEGIN
    CREATE TABLE [ServiceablePincodes] (
        [Id] int NOT NULL IDENTITY(1, 1),
        [Pincode] nvarchar(10) NOT NULL,
        [City] nvarchar(50) NOT NULL,
        [State] nvarchar(50) NOT NULL,
        [EstimatedDays] int NOT NULL DEFAULT 3,
        [IsCODAvailable] bit NOT NULL DEFAULT 1,
        [IsActive] bit NOT NULL DEFAULT 1,
        CONSTRAINT [PK_ServiceablePincodes] PRIMARY KEY ([Id])
    );
    PRINT 'Created ServiceablePincodes table';
END
ELSE
    PRINT 'ServiceablePincodes table already exists';

-- 4. Create CustomerAddresses table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CustomerAddresses')
BEGIN
    CREATE TABLE [CustomerAddresses] (
        [AddressID] int NOT NULL IDENTITY(1, 1),
        [CustomerID] int NOT NULL,
        [FullName] nvarchar(100) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [AddressLine1] nvarchar(200) NOT NULL,
        [AddressLine2] nvarchar(max) NULL,
        [City] nvarchar(50) NOT NULL,
        [State] nvarchar(50) NOT NULL,
        [Pincode] nvarchar(10) NOT NULL,
        [AddressType] nvarchar(20) NOT NULL DEFAULT 'Home',
        [IsDefault] bit NOT NULL DEFAULT 0,
        CONSTRAINT [PK_CustomerAddresses] PRIMARY KEY ([AddressID]),
        CONSTRAINT [FK_CustomerAddresses_Customers_CustomerID] FOREIGN KEY ([CustomerID]) REFERENCES [Customers] ([CustomerID]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_CustomerAddresses_CustomerID] ON [CustomerAddresses] ([CustomerID]);
    PRINT 'Created CustomerAddresses table';
END
ELSE
    PRINT 'CustomerAddresses table already exists';

-- 5. Create AuditLogs table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE [AuditLogs] (
        [LogID] int NOT NULL IDENTITY(1, 1),
        [AdminEmail] nvarchar(100) NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityID] nvarchar(max) NULL,
        [Details] nvarchar(max) NULL,
        [IpAddress] nvarchar(max) NULL,
        [Timestamp] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([LogID])
    );
    PRINT 'Created AuditLogs table';
END
ELSE
    PRINT 'AuditLogs table already exists';

-- 6. Seed some sample serviceable pincodes for testing
IF NOT EXISTS (SELECT TOP 1 1 FROM [ServiceablePincodes])
BEGIN
    INSERT INTO [ServiceablePincodes] (Pincode, City, [State], EstimatedDays, IsCODAvailable, IsActive) VALUES
    ('110001', 'New Delhi', 'Delhi', 2, 1, 1),
    ('110002', 'New Delhi', 'Delhi', 2, 1, 1),
    ('400001', 'Mumbai', 'Maharashtra', 3, 1, 1),
    ('400002', 'Mumbai', 'Maharashtra', 3, 1, 1),
    ('560001', 'Bangalore', 'Karnataka', 3, 1, 1),
    ('500001', 'Hyderabad', 'Telangana', 4, 1, 1),
    ('600001', 'Chennai', 'Tamil Nadu', 4, 1, 1),
    ('700001', 'Kolkata', 'West Bengal', 5, 1, 1),
    ('302001', 'Jaipur', 'Rajasthan', 4, 1, 1),
    ('411001', 'Pune', 'Maharashtra', 3, 1, 1),
    ('226001', 'Lucknow', 'Uttar Pradesh', 4, 1, 1),
    ('380001', 'Ahmedabad', 'Gujarat', 3, 1, 1);
    PRINT 'Seeded 12 serviceable pincodes';
END

-- 7. Mark the EnhancementTables migration as applied
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260725200943_EnhancementTables')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20260725200943_EnhancementTables', '10.0.3');
    PRINT 'Marked EnhancementTables migration as applied';
END

PRINT 'All enhancement tables created successfully!';
