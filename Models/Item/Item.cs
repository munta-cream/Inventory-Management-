using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Inventory_Management_Requirements.Models
{
    public class Item
    {
        public int Id { get; set; }
        public int InventoryId { get; set; }
        public virtual Inventory Inventory { get; set; }
        public string CustomId { get; set; }
        public string CreatedById { get; set; }
        public virtual ApplicationUser CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // JSONB: { "name": "...", "description": "...", "brand": "...", "model": "...", "quantity": 10, "price": 100.0, "weight": 5.0, "inStock": true, "fragile": false, "perishable": false }
        public string FieldData { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public JsonDocument FieldDataJson
        {
            get => string.IsNullOrEmpty(FieldData) ? null : JsonDocument.Parse(FieldData);
            set => FieldData = value?.RootElement.GetRawText();
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string Name
        {
            get => GetFieldValue<string>("name");
            set => SetFieldValue("name", value);
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string Description
        {
            get => GetFieldValue<string>("description");
            set => SetFieldValue("description", value);
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string Brand
        {
            get => GetFieldValue<string>("brand");
            set => SetFieldValue("brand", value);
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string Model
        {
            get => GetFieldValue<string>("model");
            set => SetFieldValue("model", value);
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int? Quantity
        {
            get => GetFieldValue<int?>("quantity");
            set => SetFieldValue("quantity", value);
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public decimal? Price
        {
            get => GetFieldValue<decimal?>("price");
            set => SetFieldValue("price", value);
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public decimal? Weight
        {
            get => GetFieldValue<decimal?>("weight");
            set => SetFieldValue("weight", value);
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool InStock
        {
            get => GetFieldValue<bool>("inStock");
            set => SetFieldValue("inStock", value);
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool Fragile
        {
            get => GetFieldValue<bool>("fragile");
            set => SetFieldValue("fragile", value);
        }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool Perishable
        {
            get => GetFieldValue<bool>("perishable");
            set => SetFieldValue("perishable", value);
        }

        private T GetFieldValue<T>(string key)
        {
            if (string.IsNullOrEmpty(FieldData)) return default;
            try
            {
                var json = JsonDocument.Parse(FieldData);
                if (json.RootElement.TryGetProperty(key, out var element))
                {
                    return element.Deserialize<T>();
                }
            }
            catch { }
            return default;
        }

        private void SetFieldValue<T>(string key, T value)
        {
            var json = string.IsNullOrEmpty(FieldData) ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(FieldData) ?? new Dictionary<string, object>();
            if (value == null)
            {
                json.Remove(key);
            }
            else
            {
                json[key] = value;
            }
            FieldData = JsonSerializer.Serialize(json);
        }
    }
}
