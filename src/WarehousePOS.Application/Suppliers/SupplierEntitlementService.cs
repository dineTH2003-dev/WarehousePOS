using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Exceptions;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Suppliers;

public sealed class SupplierEntitlementService : ISupplierEntitlementService
{
    private readonly ISupplierEntitlementRepository _repository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IProductRepository _productRepository;

    public SupplierEntitlementService(
        ISupplierEntitlementRepository repository,
        ISupplierRepository supplierRepository,
        IProductRepository productRepository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
    }

    public async Task<IReadOnlyList<SupplierProductEntitlementDto>> GetEntitlementsBySupplierAsync(int supplierId, CancellationToken ct = default)
    {
        var items = await _repository.GetBySupplierIdAsync(supplierId, ct);
        return items.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<SupplierProductEntitlementDto>> GetEntitlementsByProductAsync(int productId, CancellationToken ct = default)
    {
        var items = await _repository.GetByProductIdAsync(productId, ct);
        return items.Select(MapToDto).ToList();
    }

    public async Task<SupplierProductEntitlementDto> CreateEntitlementAsync(CreateSupplierEntitlementDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var supplier = await _supplierRepository.GetByIdAsync(dto.SupplierId, ct)
            ?? throw new EntityNotFoundException("Supplier", dto.SupplierId);

        var product = await _productRepository.GetByIdAsync(dto.ProductId, ct)
            ?? throw new EntityNotFoundException("Product", dto.ProductId);

        var entity = SupplierProductEntitlement.Create(
            supplierId: dto.SupplierId,
            productId: dto.ProductId,
            nature: dto.Nature,
            eventDate: dto.EventDate,
            quantity: dto.Quantity,
            value: dto.Value,
            nextEntitlementDate: dto.NextEntitlementDate,
            specialNotes: dto.SpecialNotes
        );

        await _repository.AddAsync(entity, ct);

        // Fetch re-populated entity with navigational properties loaded
        var savedEntity = await _repository.GetByIdAsync(entity.Id, ct) ?? entity;
        return MapToDto(savedEntity);
    }

    public async Task DeleteEntitlementAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException("SupplierProductEntitlement", id);

        await _repository.DeleteAsync(entity, ct);
    }

    private static SupplierProductEntitlementDto MapToDto(SupplierProductEntitlement e)
    {
        var supplierName = e.Supplier?.Name ?? $"Supplier #{e.SupplierId}";
        var productName = e.Product?.Name ?? $"Product #{e.ProductId}";
        var productCode = !string.IsNullOrWhiteSpace(e.Product?.SKU)
            ? e.Product.SKU
            : (!string.IsNullOrWhiteSpace(e.Product?.Barcode) ? e.Product.Barcode : "N/A");

        string displayVal;
        if (e.Value.HasValue && e.Value.Value > 0)
        {
            displayVal = $"Rs. {e.Value.Value:N2}";
            if (e.Quantity.HasValue && e.Quantity.Value > 0)
                displayVal += $" ({e.Quantity.Value} units)";
        }
        else if (e.Quantity.HasValue && e.Quantity.Value > 0)
        {
            displayVal = $"{e.Quantity.Value} units";
        }
        else
        {
            displayVal = "N/A";
        }

        string nextEntitlementDisplay;
        bool isImminent = false;

        if (e.NextEntitlementDate.HasValue)
        {
            var date = e.NextEntitlementDate.Value.Date;
            var daysUntil = (date - DateTime.Today).Days;

            if (daysUntil < 0)
            {
                nextEntitlementDisplay = $"🚨 Overdue ({date:yyyy-MM-dd})";
                isImminent = true;
            }
            else if (daysUntil == 0)
            {
                nextEntitlementDisplay = $"⚡ Due Today ({date:yyyy-MM-dd})";
                isImminent = true;
            }
            else if (daysUntil <= 14)
            {
                nextEntitlementDisplay = $"⏰ Imminent ({date:yyyy-MM-dd})";
                isImminent = true;
            }
            else
            {
                nextEntitlementDisplay = date.ToString("yyyy-MM-dd");
            }
        }
        else
        {
            nextEntitlementDisplay = "N/A";
        }

        return new SupplierProductEntitlementDto(
            Id: e.Id,
            SupplierId: e.SupplierId,
            SupplierName: supplierName,
            ProductId: e.ProductId,
            ProductName: productName,
            ProductCode: productCode,
            Nature: e.Nature,
            Quantity: e.Quantity,
            Value: e.Value,
            DisplayQuantityOrValue: displayVal,
            EventDate: e.EventDate,
            NextEntitlementDate: e.NextEntitlementDate,
            NextEntitlementDisplay: nextEntitlementDisplay,
            IsImminent: isImminent,
            SpecialNotes: e.SpecialNotes,
            CreatedAt: e.CreatedAt
        );
    }
}
