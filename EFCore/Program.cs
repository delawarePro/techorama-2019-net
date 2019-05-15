using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MoreLinq;
using Shared.DataGeneration;
using Shared.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace EFCore
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            using (var context = new EFCoreContext(config.GetConnectionString(nameof(EFCoreContext))))
            {
                // Ensure context is initialized.
                ClearData(context);
                context.Products.FirstOrDefault();
                Console.WriteLine("Database initialized.");

                //WriteAndReadProducts(context, nofProducts: 20, nofProperties: 25);

                //QueryProducts(context);

                context.EnsureUpsertSproc();
                UpsertProductsBatched(context, nofProducts: 1000, nofProperties: 25);
            }

            Console.ReadLine();
        }

        private static void WriteAndReadProducts(EFCoreContext context, int nofProducts, int nofProperties, int batchSize = 100)
        {
            Console.WriteLine("Writing products...");
            var sw = Stopwatch.StartNew();
            {
                foreach (var batch in ProductGenerator.GenerateProducts(nofProducts, nofProperties).Batch(batchSize))
                {
                    // Read existing from database.
                    var ids = batch.Select(x => x.Id).ToArray();
                    var existingSetFromDb = context.Products
                        .Include(x => x.Properties)
                        .Where(x => ids.Contains(x.Id)).ToList();

                    // Update existing ones.
                    var existingSetInBatch = new List<Product>();
                    foreach (var existing in existingSetFromDb)
                    {
                        var update = batch.First(x => x.Id.Equals(existing.Id, StringComparison.OrdinalIgnoreCase));
                        existing.UpdateFrom(update);
                        existingSetInBatch.Add(update);
                    }

                    // Add new ones.
                    context.Products.AddRange(batch.Except(existingSetInBatch));

                    context.SaveChanges();
                }
            }
            sw.Stop();
            Console.WriteLine($"Written {nofProducts} products with {nofProperties} properties in: {sw.Elapsed}");
            var rps = (nofProducts + nofProducts * nofProperties) / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"That is {rps:0.##} records/s");

            Console.WriteLine("Reading products...");
            context.Products.FirstOrDefault();
            var counter = 0;
            sw.Restart();
            {
                IList<Product> batch = null;
                do
                {
                    batch = context.Products.AsNoTracking().Include(x => x.Properties).OrderBy(x => x.Id).Skip(counter).Take(100).ToList();
                    counter += batch.Count;
                } while (batch.Count == 100);
            }
            sw.Stop();
            Console.WriteLine($"Read {counter} products with {nofProperties} properties in: {sw.Elapsed}");
        }

        private static void UpsertProductsBatched(EFCoreContext context, int nofProducts, int nofProperties, int batchSize = 1000)
        {
            Console.WriteLine("Writing products...");
            var sw = Stopwatch.StartNew();
            {
                foreach (var batch in ProductGenerator.GenerateProducts(nofProducts, nofProperties).Batch(batchSize))
                {
                    var productsTable = new DataTable();
                    productsTable.Columns.Add("Id", typeof(string));
                    productsTable.Columns.Add("Code", typeof(string));
                    productsTable.Columns.Add("Name", typeof(string));
                    productsTable.Columns.Add("StartDate", typeof(DateTime));
                    productsTable.Columns.Add("EndDate", typeof(DateTime));
                    productsTable.Columns.Add("IsActive", typeof(bool));
                    productsTable.Columns.Add("IsBuyable", typeof(bool));
                    productsTable.Columns.Add("MinOrderQuantity", typeof(int));
                    productsTable.Columns.Add("MaxOrderQuantity", typeof(int));

                    var propertiesTable = new DataTable();
                    propertiesTable.Columns.Add("ProductId", typeof(string));
                    propertiesTable.Columns.Add("Name", typeof(string));
                    propertiesTable.Columns.Add("Locale", typeof(string));
                    propertiesTable.Columns.Add("Value", typeof(string));

                    foreach (var p in batch)
                    {
                        productsTable.Rows.Add(p.Id, p.Code, p.Name, p.StartDate, p.EndDate,
                            p.IsActive, p.IsBuyable, p.MinOrderQuantity, p.MaxOrderQuantity);

                        foreach (var prop in p.Properties)
                        {
                            propertiesTable.Rows.Add(prop.ProductId, prop.Name, prop.Locale, prop.Value);
                        }
                    }

                    context.Database.ExecuteSqlCommand("EXEC [dbo].[UpsertProducts] @products, @properties",
                        new SqlParameter("@products", SqlDbType.Structured)
                        {
                            Value = productsTable,
                            TypeName = "dbo.ProductsUDT"
                        }, 
                        new SqlParameter("@properties", SqlDbType.Structured)
                        {
                            Value = propertiesTable,
                            TypeName = "dbo.ProductPropertiesUDT"
                        });
                }
            }
            sw.Stop();
            Console.WriteLine($"Written {nofProducts} products with {nofProperties} properties in: {sw.Elapsed}");
            var rps = (nofProducts + nofProducts * nofProperties) / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"That is {rps:0.##} records/s");
        }

        private static void QueryProducts(EFCoreContext context)
        {
            var sw = Stopwatch.StartNew();

            var results = context.Products
                .Include(pd => pd.Properties)
                .Where(pd => pd.Properties.Any(prop => prop.Name == "prop-20"
                    && prop.Value.StartsWith("Property value 20"))
                )
                .GroupBy(pd => pd.IsActive == null ? false : pd.IsActive)
                .Select(g => new
                {
                    Active = g.Key,
                    Count = g.Count()
                })
                .ToList();

            Console.WriteLine($"Queried {results.Count} results in: {sw.Elapsed}");
        }

        private static void ClearData(EFCoreContext context)
        {
            context.Database.ExecuteSqlCommand(@"
                TRUNCATE TABLE ProductProperties
                DELETE FROM Products
            ");
        }
    }
}
