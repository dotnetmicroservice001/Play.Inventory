using System;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service;


public static class Extensions
{
    public static InventoryItemsDto AsDto(this InventoryItem item, string name, string description, string imageUrl, string category, string rarity)
    {
        return new InventoryItemsDto(item.CatalogItemID, name, item.Quantity, description, imageUrl, category, rarity, item.AcquiredDate);
    }
}