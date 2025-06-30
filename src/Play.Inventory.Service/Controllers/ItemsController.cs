using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private const string AdminRole = "Admin";
    private readonly IRepository<InventoryItem> _inventoryItemsRepository;
    private readonly IRepository<CatalogItem> _catalogItemsRepository;
   private readonly IPublishEndpoint _publishEndpoint;

    public ItemsController(IRepository<InventoryItem> repository,  
        IRepository<CatalogItem> catalogItemsRepository, 
        IPublishEndpoint publishEndpoint)
    {
        _inventoryItemsRepository = repository;
       _catalogItemsRepository = catalogItemsRepository;
       _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<InventoryItem>>> GetAsync(Guid userId)
    {
        
        // if empty return bad request 
        if (userId == Guid.Empty)
        {
            return BadRequest();
        }

        var currentUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (Guid.Parse(currentUserId) != userId)
        {
            if (!User.IsInRole(AdminRole))
            {
                return Forbid();
            }
        }
        
        // we're defining a filter for get all async which is userID 
        // we are converting item to DTO to give to the user [ get call ] 
         
        var inventoryItemEntities = await _inventoryItemsRepository.GetAllAsync(item => item.UserId == userId );
        // collecting itemsIDs from inventory items 
        var itemIds = inventoryItemEntities.Select(item => item.CatalogItemID);
        // we have catalog items that match the ID in the inventory items 
        var catalogItemEntities = await _catalogItemsRepository
                                .GetAllAsync( item => itemIds.Contains(item.Id));
        
        var inventoryItemDtos = inventoryItemEntities.Select(inventoryItem =>
        {
            var catalogItem = catalogItemEntities.Single(
                catalogItem => catalogItem.Id == inventoryItem.CatalogItemID);
            return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
        }); 
        // wrapped in a action result with the item 
        return Ok(inventoryItemDtos);   
    }

    [HttpPost]
    [Authorize(Roles = AdminRole)]
    public async Task<ActionResult> PostAsync(GrantItemsDto grantItemsDto)
    {
         
        var inventoryItem = await _inventoryItemsRepository.GetAsync(
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
            await _inventoryItemsRepository.CreateAsync(inventoryItem);
        }
        else
        {
            // if we have it, increase the amt
            inventoryItem.Quantity += grantItemsDto.Quantity;
            await _inventoryItemsRepository.UpdateAsync(inventoryItem);
        }

        await _publishEndpoint.Publish(new InventoryItemUpdated(
            inventoryItem.UserId,
            inventoryItem.CatalogItemID,
            inventoryItem.Quantity
        )); 
        return Ok();
    }
}