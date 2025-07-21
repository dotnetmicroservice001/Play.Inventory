using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Play.Common;
using Play.Common.Settings;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumer;

public class GrantItemsConsumer :IConsumer<GrantItems>
{
    
    private readonly IRepository<InventoryItem> _inventoryItemsRepository;
    private readonly IRepository<CatalogItem> _catalogItemsRepository;
    private readonly ILogger<GrantItemsConsumer> _logger;
    private readonly Counter<int> _itemsGrantedCounter;

    public GrantItemsConsumer(
        IConfiguration configuration,
        IRepository<InventoryItem> inventoryItemsRepository,
        IRepository<CatalogItem> catalogItemsRepository, 
        ILogger<GrantItemsConsumer> logger)
    {
        _inventoryItemsRepository = inventoryItemsRepository;
        _catalogItemsRepository = catalogItemsRepository;
        _logger = logger;
        
        var settings = configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
        Meter meter = new(settings.ServiceName);
        _itemsGrantedCounter = meter.CreateCounter<int>("ItemsGranted");
    }
    
    
    public async Task Consume(ConsumeContext<GrantItems> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received GrantItems message with id: {CorrelationId} for " +
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
        if (inventoryItem == null)
        {
            inventoryItem = new InventoryItem
            {
                UserId = message.UserId,
                CatalogItemID = message.CatalogItemId,
                Quantity = message.Quantity,
                AcquiredDate = DateTimeOffset.UtcNow
            }; 
            inventoryItem.MessageIds.Add(context.MessageId.Value);
            await _inventoryItemsRepository.CreateAsync(inventoryItem);
        }
        else
        {
            if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
            {
                await context.Publish(new InventoryItemsGranted(message.CorrelationId)); 
                return;
            }
            // if we have it, increase the amt
            inventoryItem.Quantity += message.Quantity;
            inventoryItem.MessageIds.Add(context.MessageId.Value);
            await _inventoryItemsRepository.UpdateAsync(inventoryItem);
            _itemsGrantedCounter.Add(1,
                new KeyValuePair<string, object>(context.Message.CorrelationId.ToString(),
                    context.Message.CorrelationId ));
        }
        // send an event that inventory item has been granted
        var itemsGrantedTask = context.Publish(new InventoryItemsGranted(message.CorrelationId));
        var inventoryUpdatedTask = context.Publish(new InventoryItemUpdated(
            inventoryItem.UserId,
            inventoryItem.CatalogItemID,
            inventoryItem.Quantity
        ));
        
        // start both tasks and wait for both of them to complete 
        await Task.WhenAll(itemsGrantedTask, inventoryUpdatedTask);
    }
}

