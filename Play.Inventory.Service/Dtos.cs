using System;

namespace Play.Inventory.Service;

    public record GrantItemsDto(Guid UserId, Guid CatalogItemId,  int Quantity); 
    public record InventoryItemsDto(Guid CatalogItemId, string Name, int Quantity,  string Description, DateTimeOffset AcquiredDate);
    
    public record CatalogItemDto(Guid Id, string Name,  string Description);