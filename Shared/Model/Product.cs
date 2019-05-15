using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Shared.Model
{
    [Table("Products", Schema = "dbo")]
    public class Product
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [MaxLength(100)]
        public string Id { get; set; }

        [MaxLength(100)]
        public string Code { get; set; }

        public string Name { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsBuyable { get; set; }

        public int? MinOrderQuantity { get; set; }
        public int? MaxOrderQuantity { get; set; }

        public virtual ICollection<ProductProperty> Properties { get; set; }

        public string Partition { get; set; }

        public Product UpdateFrom(Product update)
        {
            Code = update.Code;
            Name = update.Name;
            StartDate = update.StartDate;
            EndDate = update.EndDate;
            IsActive = update.IsActive;
            IsBuyable = update.IsBuyable;
            MinOrderQuantity = update.MinOrderQuantity;
            MaxOrderQuantity = update.MaxOrderQuantity;

            // Merge property list.
            var matchedProperties = new List<ProductProperty>();
            foreach (var updateProperty in (update.Properties ?? Enumerable.Empty<ProductProperty>()))
            {
                var existing = Properties?.FirstOrDefault(x =>
                    x.ProductId.Equals(updateProperty.ProductId, StringComparison.OrdinalIgnoreCase)
                    && x.Name.Equals(updateProperty.Name, StringComparison.Ordinal));
                if (existing != null)
                {
                    existing.UpdateFrom(updateProperty);
                    matchedProperties.Add(existing);
                }
                else
                {
                    Properties.Add(updateProperty);
                    matchedProperties.Add(updateProperty);
                }
            }

            // Remove unmatched properties.
            foreach(var unmatched in Properties.Except(matchedProperties).ToArray())
            {
                Properties.Remove(unmatched);
            }

            return this;
        }
    }
}
