using Microsoft.Extensions.Logging;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Exceptions;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Products;

public sealed class CategoryService(
    ICategoryRepository repo,
    ILogger<CategoryService> logger) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var cats = await repo.GetAllAsync(ct);
        return cats.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<CategoryDto>> GetActiveAsync(CancellationToken ct = default)
    {
        var cats = await repo.GetActiveAsync(ct);
        return cats.Select(Map).ToList();
    }

    public async Task<CategoryDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var cat = await repo.GetByIdAsync(id, ct);
        return cat is null ? null : Map(cat);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        if (await repo.ExistsByNameAsync(request.Name, ct: ct))
            throw new BusinessRuleViolationException("UniqueCategory", $"Category '{request.Name}' already exists.");

        var category = Category.Create(request.Name, request.Description);
        await repo.AddAsync(category, ct);

        logger.LogInformation("Category created: {Name}", category.Name);
        return Map(category);
    }

    public async Task<CategoryDto> UpdateAsync(UpdateCategoryRequest request, CancellationToken ct = default)
    {
        var category = await repo.GetByIdAsync(request.Id, ct)
            ?? throw new EntityNotFoundException(nameof(Category), request.Id);

        if (await repo.ExistsByNameAsync(request.Name, request.Id, ct))
            throw new BusinessRuleViolationException("UniqueCategory", $"Category '{request.Name}' already exists.");

        category.Update(request.Name, request.Description);
        await repo.UpdateAsync(category, ct);
        return Map(category);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var category = await repo.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Category), id);
        category.Deactivate();
        await repo.UpdateAsync(category, ct);
    }

    public async Task ActivateAsync(int id, CancellationToken ct = default)
    {
        var category = await repo.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Category), id);
        category.Activate();
        await repo.UpdateAsync(category, ct);
    }

    private static CategoryDto Map(Category c) =>
        new(c.Id, c.Name, c.Description, c.IsActive, c.Products.Count);
}
