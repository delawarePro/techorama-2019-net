using Shared.Model;
using System.Data.Entity;

namespace EF6
{
    public class EF6Context : DbContext
    {
        public DbSet<Product> Products { get; set; }
    }
}
