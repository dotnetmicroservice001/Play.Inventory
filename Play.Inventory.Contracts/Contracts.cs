
namespace Play.Inventory.Contracts;


public record GrantItems(Guid UserId, 
                Guid CatalogItemId,
                int Quantity,
                Guid CorrelationId);

public record InventoryItemsGranted(Guid CorrelationId);       

//compensatory action in saga for granting items
public record SubtractItems(Guid UserId, 
    Guid CatalogItemId,
    int Quantity,
    Guid CorrelationId);        

public record InventoryItemsSubtracted(Guid CorrelationId);    

public record InventoryItemUpdated(
    Guid UserId,
    Guid CatalogItemId,
    int newTotalQuantity
    );