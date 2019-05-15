using Microsoft.EntityFrameworkCore;
using Shared.Model;
using System.IO;
using System.Text.RegularExpressions;

namespace EFCore
{
    public class EFCoreContext : DbContext
    {
        private string ConnectionString { get; }

        public DbSet<Product> Products { get; set; }

        public EFCoreContext(string connectionString)
        {
            ConnectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(ConnectionString);
            }

            // Don't allow client side evalutation.
            //optionsBuilder
            //    .ConfigureWarnings(w => w.Throw(RelationalEventId.QueryClientEvaluationWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductProperty>()
                .HasKey(x => new { x.ProductId, x.Name });
        }

        public void EnsureUpsertSproc()
        {
            using (var stream = typeof(EFCoreContext).Assembly.GetManifestResourceStream("EFCore.Sql.UpsertSproc.sql"))
            using (var reader = new StreamReader(stream))
            {
                var sql = reader.ReadToEnd();

                string[] array = Regex.Split(sql, "^\\s*GO\\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                foreach (string text in array)
                {
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        Database.ExecuteSqlCommand(text);
                    }
                }
            }
        }
    }
}
