using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.DataGeneration;
using Shared.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EFCore
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var context = new EFCoreContext())
            {
                // Force database recreation.
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                context.Products.FirstOrDefault();
                Console.WriteLine("Database initialized.");

                const int nofProducts = 5000;
                const int nofProperties = 100;
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

    public class EFCoreContext : DbContext
    {
        public DbSet<Product> Products { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(@"Data Source=(LocalDb)\ProjectsV13;Initial Catalog=TechoramaEFCore;Integrated Security=SSPI;");
            }

            //optionsBuilder
            //    .ConfigureWarnings(w => w.Throw(RelationalEventId.QueryClientEvaluationWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductProperty>()
                .HasKey(x => new { x.ProductId, x.Name });
        }
    }
}
