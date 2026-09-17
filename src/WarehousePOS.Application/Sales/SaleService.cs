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

        Customer? customer = null;
        if (req.CustomerId.HasValue)
        {
            customer = await customerRepo.GetByIdAsync(req.CustomerId.Value, ct)
                ?? throw new EntityNotFoundException(nameof(Customer), req.CustomerId.Value);
        }
        else if (req.SaveAsNewCustomer && !string.IsNullOrWhiteSpace(req.CustomerName))
        {
            customer = Customer.Create(req.CustomerName, req.SaleType, req.CustomerPhone, address: req.DeliveryAddress);
            await customerRepo.AddAsync(customer, ct);
        }

        Sale sale = null!;
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            sale = Sale.Create(
                req.SaleType,
                req.CreatedByUserId,
                customer?.Id ?? req.CustomerId,
                req.Notes,
                req.PaymentMethod,
                req.DeliveryFee,
                req.CustomerName ?? customer?.Name,
                req.CustomerPhone ?? customer?.Phone,
                req.DeliveryAddress ?? customer?.Address);

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
                    notes: req.IsAdvancePayment ? $"POS Sale ({req.SaleType}) [Advance Order]" : $"POS Sale ({req.SaleType})");

                await movementRepo.AddAsync(movement, ct);
            }

            if (req.DiscountAmount > 0)
                sale.ApplyDiscount(req.DiscountAmount);

            sale.RecordPayment(
                req.AmountPaid,
                isRegisteredCustomer: customer is not null,
                isAdvancePayment: req.IsAdvancePayment,
                userId: req.CreatedByUserId,
                notes: req.Notes);

            if (customer is not null && req.AmountPaid < sale.TotalAmount)
            {
                decimal unpaidBalance = sale.TotalAmount - req.AmountPaid;
                customer.IncreaseOutstandingBalance(unpaidBalance);
                await customerRepo.UpdateAsync(customer, ct);
            }

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

            if (sale.CustomerId.HasValue && sale.AmountPaid < sale.TotalAmount)
            {
                var customer = await customerRepo.GetByIdAsync(sale.CustomerId.Value, ct);
                if (customer is not null)
                {
                    decimal unpaidBalance = sale.TotalAmount - sale.AmountPaid;
                    customer.DecreaseOutstandingBalance(unpaidBalance);
                    await customerRepo.UpdateAsync(customer, ct);
                }
            }

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

    public async Task<IReadOnlyList<SaleDto>> SearchSalesAsync(SaleSearchCriteria criteria, CancellationToken ct = default)
    {
        var sales = await saleRepo.SearchAsync(
            criteria.FromDate,
            criteria.ToDate,
            criteria.SearchTerm,
            criteria.Status,
            criteria.PaymentMethod,
            criteria.SaleType,
            ct);
        return sales.Select(Map).ToList();
    }

    public async Task<SaleDto> RecordPaymentAsync(RecordSalePaymentRequest req, CancellationToken ct = default)
    {
        var sale = await saleRepo.GetByIdAsync(req.SaleId, ct)
            ?? throw new EntityNotFoundException(nameof(Sale), req.SaleId);

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            sale.RecordAdditionalPayment(req.Amount, req.PaymentMethod, req.CashierUserId, req.Notes);

            if (sale.CustomerId.HasValue)
            {
                var customer = await customerRepo.GetByIdAsync(sale.CustomerId.Value, ct);
                if (customer is not null)
                {
                    customer.DecreaseOutstandingBalance(req.Amount);
                    await customerRepo.UpdateAsync(customer, ct);
                }
            }

            await saleRepo.UpdateAsync(sale, ct);
        }, ct);

        logger.LogInformation("Additional payment of {Amount:C2} recorded for Sale #{SaleId}", req.Amount, sale.Id);
        return Map(sale);
    }

    public async Task<SaleDto> ProcessReturnAsync(ProcessSaleReturnRequest req, CancellationToken ct = default)
    {
        var sale = await saleRepo.GetByIdAsync(req.SaleId, ct)
            ?? throw new EntityNotFoundException(nameof(Sale), req.SaleId);

        if (!req.Items.Any())
            throw new BusinessRuleViolationException("EmptyReturn", "No items specified for return.");

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            decimal totalRefundAmount = 0;

            foreach (var returnItem in req.Items)
            {
                if (returnItem.Quantity <= 0) continue;

                var saleItem = sale.Items.FirstOrDefault(i => i.ProductId == returnItem.ProductId)
                    ?? throw new BusinessRuleViolationException("ItemNotFound", $"Product ID {returnItem.ProductId} is not on invoice #{sale.Id}.");

                var unitRefund = saleItem.UnitPrice - (saleItem.Quantity > 0 ? (saleItem.Discount / saleItem.Quantity) : 0);
                totalRefundAmount += unitRefund * returnItem.Quantity;

                sale.ProcessReturn(returnItem.ProductId, returnItem.Quantity);

                var product = await productRepo.GetByIdAsync(returnItem.ProductId, ct)
                    ?? throw new EntityNotFoundException(nameof(Product), returnItem.ProductId);

                var before = product.StockQuantity;
                product.AddStock(returnItem.Quantity);
                await productRepo.UpdateAsync(product, ct);

                var movement = InventoryMovement.Create(
                    product.Id,
                    MovementType.ReturnIn,
                    returnItem.Quantity,
                    before,
                    req.CashierUserId,
                    referenceId: sale.Id.ToString(),
                    referenceType: "SaleReturn",
                    notes: returnItem.Reason ?? $"Return from Invoice #{sale.Id}");

                await movementRepo.AddAsync(movement, ct);
            }

            // If sale had unpaid balance, reduce customer debt first
            if (sale.CustomerId.HasValue && sale.UnpaidAmount > 0)
            {
                var customer = await customerRepo.GetByIdAsync(sale.CustomerId.Value, ct);
                if (customer is not null)
                {
                    decimal debtReduction = Math.Min(totalRefundAmount, sale.UnpaidAmount);
                    customer.DecreaseOutstandingBalance(debtReduction);
                    await customerRepo.UpdateAsync(customer, ct);
                }
            }

            await saleRepo.UpdateAsync(sale, ct);
        }, ct);

        logger.LogInformation("Processed return for Sale #{SaleId}", sale.Id);
        return Map(sale);
    }

    public async Task<SaleDto> AdjustSaleAsync(AdjustSaleRequest req, CancellationToken ct = default)
    {
        var sale = await saleRepo.GetByIdAsync(req.SaleId, ct)
            ?? throw new EntityNotFoundException(nameof(Sale), req.SaleId);

        if (string.IsNullOrWhiteSpace(req.Reason))
            throw new BusinessRuleViolationException("ReasonRequired", "An adjustment reason is mandatory.");

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            sale.AdjustBillDetails(req.DeliveryFee, req.Notes, req.CustomerName, req.CustomerPhone, req.DeliveryAddress);
            await saleRepo.UpdateAsync(sale, ct);
        }, ct);

        logger.LogInformation("Sale #{SaleId} adjusted by User {UserId}. Reason: {Reason}", req.SaleId, req.AdminUserId, req.Reason);
        return Map(sale);
    }

    private static SaleDto Map(Sale s) => new(
        s.Id,
        s.CustomerId,
        !string.IsNullOrWhiteSpace(s.CustomerName) ? s.CustomerName : (s.Customer?.Name ?? "Walk-in Customer"),
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
            i.ClaimedQuantity,
            i.ReturnedQuantity,
            i.ReturnableQuantity)).ToList(),
        s.PaymentMethod,
        s.DeliveryFee,
        s.CustomerPhone ?? s.Customer?.Phone,
        s.DeliveryAddress ?? s.Customer?.Address,
        s.UnpaidAmount,
        s.Payments.Select(p => new SalePaymentDto(
            p.Id,
            p.SaleId,
            p.Amount,
            p.PaymentMethod,
            p.PaymentMethod.ToString(),
            p.PaymentDate,
            p.CashierUserId,
            p.Notes)).ToList());
}
