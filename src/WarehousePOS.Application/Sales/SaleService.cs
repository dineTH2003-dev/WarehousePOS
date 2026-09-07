using Microsoft.Extensions.Logging;
using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Exceptions;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Sales;

public sealed class SaleService(
    ISaleRepository saleRepo,
    IProductRepository productRepo,
    ICustomerRepository customerRepo,
    IInventoryMovementRepository movementRepo,
    IUnitOfWork unitOfWork,
    ILogger<SaleService> logger) : ISaleService
{
    public async Task<IReadOnlyList<SaleDto>> GetAllAsync(CancellationToken ct = default)
    {
        var sales = await saleRepo.GetAllAsync(ct);
        return sales.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<SaleDto>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var sales = await saleRepo.GetByDateRangeAsync(from, to, ct);
        return sales.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<SaleDto>> GetByCustomerAsync(int customerId, CancellationToken ct = default)
    {
        var sales = await saleRepo.GetByCustomerAsync(customerId, ct);
        return sales.Select(Map).ToList();
    }

    public async Task<SaleDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var sale = await saleRepo.GetByIdAsync(id, ct);
        return sale is null ? null : Map(sale);
    }

    public async Task<SaleDto> ProcessSaleAsync(CreateSaleRequest req, CancellationToken ct = default)
    {
        if (!req.Items.Any())
            throw new BusinessRuleViolationException("EmptySale", "Cannot process a sale with no items.");

        if (req.CustomerId.HasValue)
        {
            _ = await customerRepo.GetByIdAsync(req.CustomerId.Value, ct)
                ?? throw new EntityNotFoundException(nameof(Customer), req.CustomerId.Value);
        }

        Sale sale = null!;
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            sale = Sale.Create(req.SaleType, req.CreatedByUserId, req.CustomerId, req.Notes);

            foreach (var itemReq in req.Items)
            {
                var product = await productRepo.GetByIdAsync(itemReq.ProductId, ct)
                    ?? throw new EntityNotFoundException(nameof(Product), itemReq.ProductId);

                // Add item to sale
                sale.AddItem(product, itemReq.Quantity, itemReq.UnitPrice, itemReq.Discount);

                // Deduct inventory
                var qtyBefore = product.StockQuantity;
                product.DeductStock(itemReq.Quantity);
                await productRepo.UpdateAsync(product, ct);

                // Create InventoryMovement log
                var movement = InventoryMovement.Create(
                    product.Id,
                    MovementType.StockOut,
                    itemReq.Quantity,
                    qtyBefore,
                    req.CreatedByUserId,
                    referenceId: sale.Id.ToString(),
                    referenceType: "Sale",
                    notes: $"POS Sale ({req.SaleType})");

                await movementRepo.AddAsync(movement, ct);
            }

            if (req.DiscountAmount > 0)
                sale.ApplyDiscount(req.DiscountAmount);

            sale.RecordPayment(req.AmountPaid);

            await saleRepo.AddAsync(sale, ct);
        }, ct);

        logger.LogInformation("Sale processed successfully: #{SaleId}, Total: {TotalAmount:C2}", sale.Id, sale.TotalAmount);
        return Map(sale);
    }

    public async Task CancelSaleAsync(int saleId, CancellationToken ct = default)
    {
        var sale = await saleRepo.GetByIdAsync(saleId, ct)
            ?? throw new EntityNotFoundException(nameof(Sale), saleId);

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            sale.Cancel();

            // Revert stock for all items
            foreach (var item in sale.Items)
            {
                var product = await productRepo.GetByIdAsync(item.ProductId, ct);
                if (product is not null)
                {
                    var qtyBefore = product.StockQuantity;
                    product.AddStock(item.Quantity);
                    await productRepo.UpdateAsync(product, ct);

                    var movement = InventoryMovement.Create(
                        product.Id,
                        MovementType.ReturnIn,
                        item.Quantity,
                        qtyBefore,
                        sale.CreatedByUserId,
                        referenceId: sale.Id.ToString(),
                        referenceType: "SaleCancellation",
                        notes: $"Cancelled Sale #{sale.Id}");

                    await movementRepo.AddAsync(movement, ct);
                }
            }

            await saleRepo.UpdateAsync(sale, ct);
        }, ct);

        logger.LogInformation("Sale #{SaleId} cancelled and stock reverted.", sale.Id);
    }

    public async Task ClaimWarrantyAsync(int saleId, int productId, int claimQuantity, int userId = 1, string? notes = null, CancellationToken ct = default)
    {
        if (claimQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(claimQuantity), "Claim quantity must be positive.");

        var sale = await saleRepo.GetByIdAsync(saleId, ct)
            ?? throw new EntityNotFoundException(nameof(Sale), saleId);

        var saleItem = sale.Items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new BusinessRuleViolationException("ItemNotFound", $"Product ID {productId} is not on invoice #{saleId}.");

        var product = await productRepo.GetByIdAsync(productId, ct)
            ?? throw new EntityNotFoundException(nameof(Product), productId);

        // Calculate warranty expiration
        var localSaleDate = sale.SaleDate.ToLocalTime();
        var isWarrantyActive = false;

        if (product.WarrantyYears > 0 || product.WarrantyMonths > 0 || product.WarrantyDays > 0)
        {
            var expiryDate = localSaleDate
                .AddYears(product.WarrantyYears)
                .AddMonths(product.WarrantyMonths)
                .AddDays(product.WarrantyDays);

            if (DateTime.Now <= expiryDate)
                isWarrantyActive = true;
        }

        if (!isWarrantyActive)
            throw new BusinessRuleViolationException("InactiveWarranty", $"Cannot claim warranty for product '{product.Name}' because warranty is not active or has expired.");

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Record claim on SaleItem and Product
            saleItem.RecordClaim(claimQuantity);
            product.RecordWarrantyClaim(claimQuantity);

            await productRepo.UpdateAsync(product, ct);
            await saleRepo.UpdateAsync(sale, ct);

            var movement = InventoryMovement.Create(
                product.Id,
                MovementType.Adjustment,
                claimQuantity,
                product.StockQuantity,
                userId,
                referenceId: sale.Id.ToString(),
                referenceType: "WarrantyClaim",
                notes: notes ?? $"Customer Warranty Claim for Invoice #{sale.Id}");

            await movementRepo.AddAsync(movement, ct);
        }, ct);

        logger.LogInformation("Warranty claim recorded for Sale #{SaleId}, Product {SKU}, Qty: {Qty}", sale.Id, product.SKU, claimQuantity);
    }

    private static SaleDto Map(Sale s) => new(
        s.Id,
        s.CustomerId,
        s.Customer?.Name ?? "Walk-in Customer",
        s.SaleType,
        s.SaleType.ToString(),
        s.Status,
        s.Status.ToString(),
        s.SubTotal,
        s.DiscountAmount,
        s.TotalAmount,
        s.AmountPaid,
        s.Change,
        s.Notes,
        s.SaleDate,
        s.Items.Select(i => new SaleItemDto(
            i.ProductId,
            i.Product?.Name ?? string.Empty,
            i.Product?.SKU ?? string.Empty,
            i.Quantity,
            i.UnitPrice,
            i.Discount,
            i.LineTotal,
            i.Product?.WarrantyYears ?? 0,
            i.Product?.WarrantyMonths ?? 0,
            i.Product?.WarrantyDays ?? 0,
            i.ClaimedQuantity)).ToList());
}
