using Shared.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shared.DataGeneration
{
    public static class ProductGenerator
    {
        public static IEnumerable<Product> GenerateProducts(int nofProducts, int nofProperties)
        {
            return Enumerable.Range(0, nofProducts).Select(x => GenerateProduct(x, nofProperties));
        }

        public static Product GenerateProduct(int sequence, int nofProperties)
        {
            var id = sequence.ToString().PadLeft(9, '0');

            return new Product
            {
                Id = id,
                Code = $"p-{id}",
                Name = $"Product {sequence}",
                IsActive = Randomizer.Randomize(true, false, (bool?)null),
                IsBuyable = Randomizer.Randomize(true, false, (bool?)null),
                StartDate = Randomizer.Randomize(
                    Randomizer.RandomDate(DateTime.Today.AddDays(-50), DateTime.Today.AddDays(50)),
                    (DateTime?)null),
                EndDate = Randomizer.Randomize(
                    Randomizer.RandomDate(DateTime.Today.AddDays(50), DateTime.Today.AddDays(250)),
                    (DateTime?)null),
                MinOrderQuantity = Randomizer.Randomize(Randomizer.RandomInt(1, 10), (int?)null),
                MaxOrderQuantity = Randomizer.Randomize(Randomizer.RandomInt(10, 100), (int?)null),
                Properties = GenerateProductProperties(id, nofProperties),
                Partition = sequence.ToString()
            };
        }

        public static ICollection<ProductProperty> GenerateProductProperties(string productId, int nofProperties)
        {
            return Enumerable
                .Range(1, nofProperties)
                .Select(x => new ProductProperty
                {
                    ProductId = productId,
                    Name = $"prop-{x}",
                    Locale = Randomizer.Randomize("nl-BE", "fr-BE", "nl", "fr", null),
                    Value = $"Property value {x} for product {productId}"
                })
                .ToList();
        }
    }
}
