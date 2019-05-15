IF TYPE_ID(N'ProductsUDT') IS NULL
	CREATE TYPE ProductsUDT AS TABLE
	(
		[Id] [nvarchar](100) NOT NULL,
		[Code] [nvarchar](100) NULL,
		[Name] [nvarchar](max) NULL,
		[StartDate] [datetime2](7) NULL,
		[EndDate] [datetime2](7) NULL,
		[IsActive] [bit] NULL,
		[IsBuyable] [bit] NULL,
		[MinOrderQuantity] [int] NULL,
		[MaxOrderQuantity] [int] NULL
	) 

IF TYPE_ID(N'ProductPropertiesUDT') IS NULL
	CREATE TYPE ProductPropertiesUDT AS TABLE
	(
		[ProductId] [nvarchar](100) NOT NULL,
		[Name] [nvarchar](100) NOT NULL,
		[Locale] [nvarchar](max) NULL,
		[Value] [nvarchar](max) NULL
	)

IF OBJECT_ID(N'[dbo].[UpsertProducts]') IS NULL
	EXEC('CREATE PROCEDURE [dbo].[UpsertProducts] AS SELECT 1;')	
GO

ALTER PROCEDURE [dbo].[UpsertProducts]
    @Products AS ProductsUDT READONLY,
	@Properties AS ProductPropertiesUDT READONLY
AS
BEGIN
    MERGE
        dbo.Products WITH (HOLDLOCK) AS target -- WITH (HOLDLOCK): http://weblogs.sqlteam.com/dang/archive/2009/01/31/UPSERT-Race-Condition-With-MERGE.aspx
    
        USING (SELECT * FROM @Products) source
    
        ON source.Id = target.Id
    
        -- Add new product.
        WHEN NOT MATCHED  THEN
            INSERT  (Id, Code, Name, StartDate, EndDate, IsActive, IsBuyable, MinOrderQuantity, MaxOrderQuantity)
            VALUES  (source.Id, source.Code, source.Name, source.StartDate, source.EndDate, source.IsActive, source.IsBuyable, source.MinOrderQuantity, source.MaxOrderQuantity)
    
        -- Update existing product.
        WHEN MATCHED THEN
            UPDATE SET
                target.Code = source.Code,
				target.Name = source.Name,
				target.EndDate = source.EndDate,
				target.IsActive = source.IsActive,
				target.IsBuyable = source.IsBuyable,
				target.MinOrderQuantity = source.MinOrderQuantity,
				target.MaxOrderQuantity = source.MaxOrderQuantity
	;
	
	MERGE
        dbo.ProductProperties WITH (HOLDLOCK) AS target -- WITH (HOLDLOCK): http://weblogs.sqlteam.com/dang/archive/2009/01/31/UPSERT-Race-Condition-With-MERGE.aspx
    
        USING (SELECT * FROM @Properties) source
    
        ON source.ProductId = target.ProductId
			AND source.Name = target.Name
    
        -- Add new property.
        WHEN NOT MATCHED  THEN
            INSERT  (ProductId, Name, Locale, Value)
            VALUES  (source.ProductId, source.Name, source.Locale, source.Value)
    
        -- Update existing property.
        WHEN MATCHED THEN
            UPDATE SET
                target.Locale = source.Locale,
				target.Value= source.Value
	;
END