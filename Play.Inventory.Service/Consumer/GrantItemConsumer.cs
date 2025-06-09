using System;
using System.Threading.Tasks;
using MassTransit;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumer;

public class GrantItemConsumer :IConsumer<GrantItems>
{
    
    private readonly IRepository<InventoryItem> _inventoryItemsRepository;
    private readonly IRepository<CatalogItem> _catalogItemsRepository;

    public GrantItemConsumer(IRepository<InventoryItem> inventoryItemsRepository,
        IRepository<CatalogItem> catalogItemsRepository)
    {
        _inventoryItemsRepository = inventoryItemsRepository;
        _catalogItemsRepository = catalogItemsRepository;
    }
    
    
    public async Task Consume(ConsumeContext<GrantItems> context)
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
        if (inventoryItem == null)
        {
            inventoryItem = new InventoryItem
            {
                UserId = message.UserId,
                CatalogItemID = message.CatalogItemId,
                Quantity = message.Quantity,
                AcquiredDate = DateTimeOffset.UtcNow
            }; 
            await _inventoryItemsRepository.CreateAsync(inventoryItem);
        }
        else
        {
            // if we have it, increase the amt
            inventoryItem.Quantity += message.Quantity;
            await _inventoryItemsRepository.UpdateAsync(inventoryItem);
        }
        // send an event that inventory item has been granted
        await context.Publish(new InventoryItemsGranted(message.CorrelationId)); 
    }
}

