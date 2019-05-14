using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Model
{
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
    }
}
