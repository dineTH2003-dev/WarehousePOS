using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Exceptions;
using WarehousePOS.Domain.Interfaces;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Application.Sales;

public sealed class SaleService(
    ISaleRepository saleRepo,
    IProductRepository productRepo,
    ICustomerRepository customerRepo,
    IInventoryMovementRepository movementRepo,
    AppDbContext db,
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

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var sale = Sale.Create(req.SaleType, req.CreatedByUserId, req.CustomerId, req.Notes);

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
            await tx.CommitAsync(ct);

            logger.LogInformation("Sale processed successfully: #{SaleId}, Total: {TotalAmount:C2}", sale.Id, sale.TotalAmount);
            return Map(sale);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "Failed to process sale");
            throw;
        }
    }

    public async Task CancelSaleAsync(int saleId, CancellationToken ct = default)
    {
        var sale = await saleRepo.GetByIdAsync(saleId, ct)
            ?? throw new EntityNotFoundException(nameof(Sale), saleId);

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
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
            await tx.CommitAsync(ct);

            logger.LogInformation("Sale #{SaleId} cancelled and stock reverted.", sale.Id);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "Failed to cancel sale #{SaleId}", saleId);
            throw;
        }
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
            i.LineTotal)).ToList());
}
