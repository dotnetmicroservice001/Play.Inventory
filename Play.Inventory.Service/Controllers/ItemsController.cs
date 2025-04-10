using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private readonly IRepository<InventoryItem> _itemsrepository;
    private readonly CatalogClient  _catalogClient;

    public ItemsController(IRepository<InventoryItem> repository,  CatalogClient catalogClient)
    {
        _itemsrepository = repository;
        _catalogClient = catalogClient;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItem>>> GetAsync(Guid userId)
    {
        // if empty return bad request 
        if (userId == Guid.Empty)
        {
            return BadRequest();
        }
        
        // we're defining a filter for get all async which is userID 
        // we are converting item to DTO to give to the user [ get call ] 
        var catalogItems = await _catalogClient.GetCatalogItemsAsync(); 
        var inventoryItemEntities = await _itemsrepository.GetAllAsync(item => item.UserId == userId );

        var inventoryItemDtos = inventoryItemEntities.Select(inventoryItem =>
        {
            var catalogItem = catalogItems.Single(
                catalogItem => catalogItem.Id == inventoryItem.CatalogItemID);
            return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
        }); 
        // wrapped in a action result with the item 
        return Ok(inventoryItemDtos);   
    }

    [HttpPost]
    public async Task<ActionResult> PostAsync(GrantItemsDto grantItemsDto)
    {
         
        var inventoryItem = await _itemsrepository.GetAsync(
            item => item.UserId == grantItemsDto.UserId 
                    && item.CatalogItemID == grantItemsDto.CatalogItemId);
        // if we dont have it, create a new record 
        if (inventoryItem == null)
        {
            inventoryItem = new InventoryItem
            {
                UserId = grantItemsDto.UserId,
                CatalogItemID = grantItemsDto.CatalogItemId,
                Quantity = grantItemsDto.Quantity,
                AcquiredDate = DateTimeOffset.UtcNow
            }; 
            await _itemsrepository.CreateAsync(inventoryItem);
        }
        else
        {
            // if we have it, increase the amt
            inventoryItem.Quantity += grantItemsDto.Quantity;
            await _itemsrepository.UpdateAsync(inventoryItem);
        }
        return Ok();
    }
}