using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumer;

public class SubtractItemsConsumer : IConsumer<SubtractItems>
{
    private readonly IRepository<InventoryItem> _inventoryItemsRepository;
    private readonly IRepository<CatalogItem> _catalogItemsRepository;
    private readonly ILogger<SubtractItemsConsumer> _logger;

    public SubtractItemsConsumer(IRepository<InventoryItem> inventoryItemsRepository,
        IRepository<CatalogItem> catalogItemsRepository, 
        ILogger<SubtractItemsConsumer> logger)
    {
        _inventoryItemsRepository = inventoryItemsRepository;
        _catalogItemsRepository = catalogItemsRepository;
        _logger = logger;
    }
    
    
    public async Task Consume(ConsumeContext<SubtractItems> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received Subtract Items message with id: {CorrelationId} for " +
                               "catalog item {CatalogItemId} for user {UserId} with quantity {Quantity}",
            message.CorrelationId, message.CatalogItemId,
            message.UserId, message.Quantity);
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
            if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
            {
                await context.Publish(new InventoryItemsGranted(message.CorrelationId)); 
                return;
            }
            inventoryItem.Quantity -= message.Quantity;
            inventoryItem.MessageIds.Add(context.MessageId.Value);
            await _inventoryItemsRepository.UpdateAsync(inventoryItem);
            
            // publish inventory item is updated 
            await context.Publish(new InventoryItemUpdated(
                inventoryItem.UserId,
                inventoryItem.CatalogItemID,
                inventoryItem.Quantity));
        }
        
        // send an event that inventory item has been granted
        await context.Publish(new InventoryItemsSubtracted(message.CorrelationId)); 
    }
}