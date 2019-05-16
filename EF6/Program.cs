using MoreLinq;
using Shared.DataGeneration;
using Shared.Model;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;

namespace EF6
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(typeof(Program).FullName);
            using (var context = new EF6Context())
            {
                // Ensure context is initialized.
                ClearData(context);
                context.Products.FirstOrDefault();
                Console.WriteLine("Database initialized.");

                WriteAndReadProducts(context, nofProducts: 50, nofProperties: 25);
            }

            Console.ReadLine();
        }

        private static void WriteAndReadProducts(EF6Context context, int nofProducts, int nofProperties, int batchSize = 100)
        {
            Console.WriteLine("Writing products...");
            var sw = Stopwatch.StartNew();
            {
                foreach (var batch in ProductGenerator.GenerateProducts(nofProducts, nofProperties).Batch(batchSize))
                {
                    // Add new ones.
                    context.Products.AddRange(batch);
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

        private static void QueryProducts(EF6Context context)
        {
            var sw = Stopwatch.StartNew();

            var results = context.Products
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

        private static void ClearData(EF6Context context)
        {
            context.Database.ExecuteSqlCommand(@"
                TRUNCATE TABLE ProductProperties
                DELETE FROM Products
            ");
        }
    }
}
