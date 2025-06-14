using System;
using System.Threading.Tasks;
using MassTransit;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumer;

public class SubtractItemsConsumer : IConsumer
{
    private readonly IRepository<InventoryItem> _inventoryItemsRepository;
    private readonly IRepository<CatalogItem> _catalogItemsRepository;

    public SubtractItemsConsumer(IRepository<InventoryItem> inventoryItemsRepository,
        IRepository<CatalogItem> catalogItemsRepository)
    {
        _inventoryItemsRepository = inventoryItemsRepository;
        _catalogItemsRepository = catalogItemsRepository;
    }
    
    
    public async Task Consume(ConsumeContext<SubtractItems> context)
    {
        var message = context.Message;
        
        // make sure item exists in database 
        var item = await _catalogItemsRepository.GetAsync(message.CatalogItemId);
        if (item == null)
        {
            // if null, throw exception
            throw new UnknownItemException(message.CatalogItemId);
        }
        
        var inventoryItem = await _inventoryItemsRepository.GetAsync(
            item => item.UserId == message.UserId
                    && item.CatalogItemID == message.CatalogItemId);
        
        // if we dont have it, create a new message
        if (inventoryItem != null)
        {
            inventoryItem.Quantity -= message.Quantity;
            await _inventoryItemsRepository.CreateAsync(inventoryItem);
        }
        
        // send an event that inventory item has been granted
        await context.Publish(new InventoryItemsSubtracted(message.CorrelationId)); 
    }
}