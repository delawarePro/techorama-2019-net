using Microsoft.EntityFrameworkCore;
using Shared.Model;

namespace EFCore
{
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
