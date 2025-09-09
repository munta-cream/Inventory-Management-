using Microsoft.AspNetCore.Mvc;
using Inventory_Management_Requirements.Services;
using Inventory_Management_Requirements.Models;
using System.Threading.Tasks;

public class SearchController : Controller
{
    private readonly SearchService _searchService;

    public SearchController(SearchService searchService)
    {
        _searchService = searchService;
    }

    public async Task<IActionResult> Index(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            return View(new SearchViewModel
            {
                SearchTerm = searchTerm,
                Inventories = new List<Inventory>(),
                Items = new List<Item>()
            });
        }

        var inventories = await _searchService.SearchInventoriesAsync(searchTerm);
        var items = await _searchService.SearchItemsAsync(searchTerm);

        var viewModel = new SearchViewModel
        {
            SearchTerm = searchTerm,
            Inventories = inventories,
            Items = items
        };

        return View(viewModel);
    }
}
