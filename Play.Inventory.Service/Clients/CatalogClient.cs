using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Play.Inventory.Service.Clients;

public class CatalogClient
{
    private readonly HttpClient _client;

    public CatalogClient(HttpClient httpClient)
    {
        _client = httpClient;
    }

    public async Task<IReadOnlyCollection<CatalogItemDto>> GetCatalogItemsAsync()
    {
        var items = await _client.
            GetFromJsonAsync<IReadOnlyCollection<CatalogItemDto>>("/items");
        return items;
    }
}