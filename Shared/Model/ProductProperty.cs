﻿using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Model
{
    [Table("ProductProperties", Schema = "dbo")]
    public class ProductProperty
    {
        [Key, Column(Order = 1)]
        [MaxLength(100)]
        [JsonIgnore]
        public string ProductId { get; set; }

        [Key, Column(Order = 2)]
        [MaxLength(100)]
        public string Name { get; set; }

        public string Locale { get; set; }
        public string Value { get; set; }

        public ProductProperty UpdateFrom(ProductProperty update)
        {
            Locale = update.Locale;
            Value = update.Value;

            return this;
        }
    }
}
