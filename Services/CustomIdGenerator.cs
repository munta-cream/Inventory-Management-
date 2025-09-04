using System;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace Inventory_Management_Requirements.Services
{
    public class CustomIdGenerator : ICustomIdGenerator
    {
        private readonly ApplicationDbContext _db;
        private readonly Random _random = new();

        public CustomIdGenerator(ApplicationDbContext db)
        {
            _db = db;
        }

        public string Generate(int inventoryId, JsonDocument format, int sequence)
        {
            var elements = format.RootElement.EnumerateArray();
            var result = new StringBuilder();

            foreach (var element in elements)
            {
                var type = element.GetProperty("type").GetString();
                result.Append(GenerateElement(type, element, sequence));
            }

            return result.ToString();
        }

        private string GenerateElement(string type, JsonElement element, int sequence)
        {
            return type switch
            {
                "fixed" => element.GetProperty("value").GetString(),
                "random" => GenerateRandom(element),
                "guid" => Guid.NewGuid().ToString(),
                "datetime" => GenerateDateTime(element),
                "seq" => GenerateSequence(sequence, element),
                _ => string.Empty
            };
        }

        private string GenerateRandom(JsonElement element)
        {
            if (element.TryGetProperty("bits", out var bitsElement))
            {
                var bits = bitsElement.GetInt32();
                var bytes = new byte[(bits + 7) / 8];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(bytes);
                
                var value = bits switch
                {
                    20 => BitConverter.ToUInt32(bytes, 0) & 0xFFFFF,
                    32 => BitConverter.ToUInt32(bytes, 0),
                    _ => BitConverter.ToUInt64(bytes, 0) & ((1UL << bits) - 1)
                };
                
                return value.ToString();
            }
            else if (element.TryGetProperty("digits", out var digitsElement))
            {
                var digits = digitsElement.GetInt32();
                var maxValue = (int)Math.Pow(10, digits) - 1;
                var randomValue = _random.Next(0, maxValue + 1);
                return randomValue.ToString($"D{digits}");
            }
            
            return string.Empty;
        }

        private string GenerateDateTime(JsonElement element)
        {
            var format = element.TryGetProperty("format", out var formatElement) 
                ? formatElement.GetString() 
                : "yyyyMMddHHmmss";
            
            return DateTime.UtcNow.ToString(format);
        }

        private string GenerateSequence(int sequence, JsonElement element)
        {
            var pad = element.TryGetProperty("pad", out var padElement) 
                ? padElement.GetInt32() 
                : 0;
            
            return pad > 0 ? sequence.ToString($"D{pad}") : sequence.ToString();
        }
    }
}
