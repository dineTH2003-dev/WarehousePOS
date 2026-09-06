using Microsoft.Extensions.Logging;
using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Exceptions;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Purchasing;

public sealed class PurchaseService(
    IPurchaseRepository purchaseRepo,
    IProductRepository productRepo,
    ISupplierRepository supplierRepo,
    IInventoryMovementRepository movementRepo,
    IUnitOfWork unitOfWork,
    ILogger<PurchaseService> logger) : IPurchaseService
{
    public async Task<IReadOnlyList<PurchaseDto>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await purchaseRepo.GetAllAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<PurchaseDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var p = await purchaseRepo.GetByIdAsync(id, ct);
        return p is null ? null : Map(p);
    }

    public async Task<PurchaseDto> CreateAsync(CreatePurchaseRequest req, CancellationToken ct = default)
    {
        _ = await supplierRepo.GetByIdAsync(req.SupplierId, ct)
            ?? throw new EntityNotFoundException(nameof(Supplier), req.SupplierId);

        var purchase = Purchase.Create(req.SupplierId, req.CreatedByUserId, req.Notes);

        foreach (var item in req.Items)
        {
            var product = await productRepo.GetByIdAsync(item.ProductId, ct)
                ?? throw new EntityNotFoundException(nameof(Product), item.ProductId);
            purchase.AddItem(product.Id, item.Quantity, item.UnitCost, item.FreeQuantity, item.RetailPrice, item.WholesalePrice);
        }

        await purchaseRepo.AddAsync(purchase, ct);
        logger.LogInformation("Purchase created: #{Id} for supplier {SupplierId}", purchase.Id, purchase.SupplierId);
        return Map(purchase);
    }

    public async Task ConfirmAsync(int purchaseId, CancellationToken ct = default)
    {
        var purchase = await purchaseRepo.GetByIdAsync(purchaseId, ct)
            ?? throw new EntityNotFoundException(nameof(Purchase), purchaseId);
        purchase.Confirm();
        await purchaseRepo.UpdateAsync(purchase, ct);
    }

    public async Task<PurchaseDto> ReceiveStockAsync(int purchaseId, CancellationToken ct = default)
    {
        var purchase = await purchaseRepo.GetByIdAsync(purchaseId, ct)
            ?? throw new EntityNotFoundException(nameof(Purchase), purchaseId);

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            purchase.Receive();

            foreach (var item in purchase.Items)
            {
                var product = await productRepo.GetByIdAsync(item.ProductId, ct)
                    ?? throw new EntityNotFoundException(nameof(Product), item.ProductId);

                var before = product.StockQuantity;
                var totalQuantityToAdd = item.Quantity + item.FreeQuantity;
                product.AddStock(totalQuantityToAdd);

                if (item.RetailPrice > 0 || item.WholesalePrice > 0)
                {
                    var retail = item.RetailPrice > 0 ? item.RetailPrice : product.RetailPrice;
                    var wholesale = item.WholesalePrice > 0 ? item.WholesalePrice : product.WholesalePrice;
                    product.UpdatePricing(retail, wholesale);
                }

                await productRepo.UpdateAsync(product, ct);

                var movement = InventoryMovement.Create(
                    product.Id, MovementType.PurchaseReceive, totalQuantityToAdd, before,
                    purchase.CreatedByUserId,
                    referenceId: purchase.Id.ToString(),
                    referenceType: "Purchase");
                await movementRepo.AddAsync(movement, ct);
            }

            await purchaseRepo.UpdateAsync(purchase, ct);
        }, ct);

        logger.LogInformation("Purchase #{Id} received — {Count} products stocked", purchase.Id, purchase.Items.Count);
        return Map(purchase);
    }

    public async Task CancelAsync(int purchaseId, CancellationToken ct = default)
    {
        var purchase = await purchaseRepo.GetByIdAsync(purchaseId, ct)
            ?? throw new EntityNotFoundException(nameof(Purchase), purchaseId);
        purchase.Cancel();
        await purchaseRepo.UpdateAsync(purchase, ct);
    }

    private static PurchaseDto Map(Purchase p) => new(
        p.Id, p.SupplierId, p.Supplier?.Name ?? string.Empty,
        p.Status, p.Status.ToString(), p.TotalAmount, p.Notes,
        p.PurchaseDate, p.ReceivedDate,
        p.Items.Select(i => new PurchaseItemDto(
            i.ProductId, i.Product?.Name ?? string.Empty,
            i.Product?.SKU ?? string.Empty,
            i.Quantity, i.FreeQuantity, i.UnitCost, i.TotalCost,
            i.RetailPrice, i.WholesalePrice)).ToList());
}
