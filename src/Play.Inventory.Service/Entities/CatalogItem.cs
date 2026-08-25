using System;
using Play.Common;

namespace Play.Inventory.Service.Entities;

public class CatalogItem : IEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public string Category { get; set; }
    public string Rarity { get; set; }
}