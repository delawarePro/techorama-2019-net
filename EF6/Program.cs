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
            using (var context = new EF6Context())
            {
                // Force database recreation.
                context.Database.Delete();
                context.Database.CreateIfNotExists();

                context.Products.FirstOrDefault();
                Console.WriteLine("Database initialized.");

                const int nofProducts = 5000;
                const int nofProperties = 25;
                Console.WriteLine("Writing products...");
                var sw = Stopwatch.StartNew();
                context.Products.AddRange(ProductGenerator.GenerateProducts(nofProducts, nofProperties));
                context.SaveChanges();
                sw.Stop();

                Console.WriteLine($"Written {nofProducts} products with {nofProperties} properties in: {sw.Elapsed}");
                var rps = (nofProducts + nofProducts * nofProperties) / sw.Elapsed.TotalSeconds;
                Console.WriteLine($"That is {rps:0.##} records/s");

                Console.WriteLine("Reading products...");
                context.Products.FirstOrDefault();
                sw.Restart();
                var counter = 0;
                IList<Product> batch = null;
                do
                {
                    batch = context.Products.AsNoTracking().Include(x => x.Properties).OrderBy(x => x.Id).Skip(counter).Take(100).ToList();
                    counter += batch.Count;
                } while (batch.Count == 100);
                sw.Stop();
                Console.WriteLine($"Read {counter} products with {nofProperties} properties in: {sw.Elapsed}");

                sw.Restart();
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

            Console.ReadLine();
        }
    }

    public class EF6Context : DbContext
    {
        public DbSet<Product> Products { get; set; }
    }
}
