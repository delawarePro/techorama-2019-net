using System;
using System.Collections.Generic;
using System.Linq;

namespace Hacking
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    public interface IDontDoThisAtHomeProductRepository
    {
        IEnumerable<Product> GetProducts();
        Product GetProductById(string productId);

        void AddProduct(Product product);
        void UpdateProduct(Product product);
        void DeleteProduct(string productId);
    }

    public interface IDontDoThisAtHomeEitherProductRepository
    {
        IQueryable<Product> GetProducts();
        Product GetProductById(string productId);
    }

    public interface IProductRepository
    {
        Product GetProductById(string productId);
        IPaginatedList<Product> GetProductsByCategory(string catalog, string category, int page, int pageSize);
        IPaginatedList<Product> FindProducts(string catalog, string category, string searchText, int page, int pageSize);

        void AddOrUpdateProduct(Product product);
        void DeleteProduct(string productIds);

        ICollection<Product> GetProductsById(params string[] productId);
        void AddOrUpdateProducts(params Product[] products);
        void DeleteProducts(params string[] productIds);
    }

    public interface IProductRepository2
    {
        Product GetProductById(string productId);
        // Can support GetProductsByCategory and FindProducts
        IPaginatedList<Product> FindProducts<Product>(IQuery<Product> query);

        void AddOrUpdateProduct(Product product);
        void DeleteProduct(string productIds);

        ICollection<Product> GetProductsById(params string[] productId);
        void AddOrUpdateProducts(params Product[] products);
        void DeleteProducts(params string[] productIds);
    }


    public class Product
    {

    }

    public interface IPaginatedList<T> : IList<T>
    {

    }

    public interface IQuery<T>
    {

    }
}
