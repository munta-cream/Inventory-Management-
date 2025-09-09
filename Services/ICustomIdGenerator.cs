using System.Text.Json;

namespace Inventory_Management_Requirements.Services
{
    public interface ICustomIdGenerator
    {
        string Generate(int inventoryId, JsonDocument format, int sequence);
    }
}
