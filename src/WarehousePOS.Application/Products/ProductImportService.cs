using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WarehousePOS.Application.Common;
using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Enums;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Products;

public sealed partial class ProductImportService(
    IProductRepository productRepo,
    ICategoryRepository categoryRepo,
    IInventoryMovementRepository movementRepo,
    IUnitOfWork unitOfWork,
    ILogger<ProductImportService> logger) : IProductImportService
{
    public async Task<ProductImportResultDto> ImportCsvAsync(
        Stream csvStream,
        ProductImportOptions options,
        int userId = 1,
        CancellationToken ct = default)
    {
        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var records = CsvParser.Parse(reader);

        if (records.Count == 0)
        {
            return new ProductImportResultDto(
                TotalRows: 0,
                SuccessCount: 0,
                SkippedCount: 0,
                ErrorCount: 1,
                Errors: [new ProductImportErrorDto(0, string.Empty, string.Empty, "The uploaded CSV file is empty.")],
                SummaryMessage: "The uploaded CSV file is empty.");
        }

        // ── Map headers ───────────────────────────────────────────────────────
        var headers = records[0];
        int nameIdx = FindColumnIndex(headers, "product name", "productname", "name", "item name", "itemname", "title");
        int skuIdx = FindColumnIndex(headers, "sku", "item code", "itemcode", "product code", "productcode", "code");
        int categoryIdx = FindColumnIndex(headers, "category", "category name", "categoryname", "department");
        int retailPriceIdx = FindColumnIndex(headers, "retail price", "retailprice", "price", "selling price", "sellingprice", "unit price", "unitprice");
        int wholesalePriceIdx = FindColumnIndex(headers, "wholesale price", "wholesaleprice", "wholesale", "cost price", "costprice", "cost");
        int stockIdx = FindColumnIndex(headers, "stock quantity", "stockquantity", "stock", "quantity", "qty", "initial stock", "initialstock");
        int reorderLevelIdx = FindColumnIndex(headers, "reorder level", "reorderlevel", "min stock", "minstock", "alert quantity");
        int barcodeIdx = FindColumnIndex(headers, "barcode", "bar code", "upc", "ean");
        int descIdx = FindColumnIndex(headers, "description", "details", "notes");
        int warrantyYearsIdx = FindColumnIndex(headers, "warranty years", "warrantyyears", "warranty (years)");
        int warrantyMonthsIdx = FindColumnIndex(headers, "warranty months", "warrantymonths", "warranty (months)");
        int warrantyDaysIdx = FindColumnIndex(headers, "warranty days", "warrantydays", "warranty (days)");

        if (nameIdx == -1)
        {
            return new ProductImportResultDto(
                TotalRows: records.Count - 1,
                SuccessCount: 0,
                SkippedCount: 0,
                ErrorCount: 1,
                Errors: [new ProductImportErrorDto(1, string.Empty, string.Empty, "Missing required column 'Product Name' or 'Name' in CSV header.")],
                SummaryMessage: "CSV header is missing the required 'Product Name' column.");
        }

        // ── Pre-load database state for fast lookups ─────────────────────────
        var existingCategories = await categoryRepo.GetAllAsync(ct);
        var categoryCache = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        foreach (var cat in existingCategories)
        {
            categoryCache[cat.Name] = cat;
        }

        var existingProducts = await productRepo.GetAllAsync(ct);
        var usedSkus = new HashSet<string>(existingProducts.Select(p => p.SKU), StringComparer.OrdinalIgnoreCase);
        var existingProductsBySku = existingProducts.ToDictionary(p => p.SKU, StringComparer.OrdinalIgnoreCase);
        var usedBarcodes = new HashSet<string>(existingProducts.Where(p => !string.IsNullOrWhiteSpace(p.Barcode)).Select(p => p.Barcode!), StringComparer.OrdinalIgnoreCase);

        var errors = new List<ProductImportErrorDto>();
        var productsToInsert = new List<Product>();
        var productsToUpdate = new List<Product>();
        int skippedCount = 0;
        int autoSkuCounter = 1;

        // ── Parse rows ────────────────────────────────────────────────────────
        for (int r = 1; r < records.Count; r++)
        {
            var row = records[r];
            int rowNumber = r + 1;

            string name = GetField(row, nameIdx);
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add(new ProductImportErrorDto(rowNumber, string.Empty, string.Empty, "Product name cannot be empty."));
                continue;
            }

            // Category resolution / auto-creation
            string categoryName = GetField(row, categoryIdx);
            if (string.IsNullOrWhiteSpace(categoryName))
                categoryName = string.IsNullOrWhiteSpace(options.DefaultCategory) ? "General" : options.DefaultCategory.Trim();
            else
                categoryName = NormalizeCategoryName(categoryName);

            if (!categoryCache.TryGetValue(categoryName, out var category))
            {
                category = Category.Create(categoryName);
                await categoryRepo.AddAsync(category, ct);
                categoryCache[categoryName] = category;
            }

            // SKU resolution / auto-generation
            string rawSku = GetField(row, skuIdx);
            string sku;

            if (string.IsNullOrWhiteSpace(rawSku) || rawSku.Equals("N/A", StringComparison.OrdinalIgnoreCase))
            {
                // Auto-generate clean SKU e.g. PRD-CONCORD-0001
                sku = GenerateUniqueSku(name, ref autoSkuCounter, usedSkus);
            }
            else
            {
                sku = rawSku.Trim().ToUpperInvariant();
            }

            // Check duplicate / existing SKU
            bool isExistingInDb = existingProductsBySku.TryGetValue(sku, out var existingProduct);
            bool isUsedInCurrentBatch = usedSkus.Contains(sku);

            if (isExistingInDb || isUsedInCurrentBatch)
            {
                if (options.UpdateExisting && isExistingInDb && existingProduct is not null)
                {
                    // Update existing product
                    decimal updateRetailPrice = ParseDecimal(GetField(row, retailPriceIdx), existingProduct.RetailPrice);
                    decimal updateWholesalePrice = ParseDecimal(GetField(row, wholesalePriceIdx), existingProduct.WholesalePrice);
                    existingProduct.UpdatePricing(updateRetailPrice, updateWholesalePrice);
                    productsToUpdate.Add(existingProduct);
                    continue;
                }
                else if (isExistingInDb && existingProduct is not null && existingProduct.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    // Same SKU AND same name -> skip true duplicate
                    skippedCount++;
                    continue;
                }
                else
                {
                    // Differentiate variant (e.g. A-01-10-01-014-410-1) so product is not lost
                    sku = DifferentiateSku(sku, usedSkus);
                }
            }

            usedSkus.Add(sku);

            // Pricing
            decimal retailPrice = ParseDecimal(GetField(row, retailPriceIdx), 0m);
            if (retailPrice < 0)
            {
                errors.Add(new ProductImportErrorDto(rowNumber, sku, name, $"Retail price cannot be negative (got {retailPrice})."));
                continue;
            }

            decimal wholesalePrice = ParseDecimal(GetField(row, wholesalePriceIdx), retailPrice);
            if (wholesalePrice < 0)
            {
                wholesalePrice = retailPrice;
            }

            // Stock & Reorder Level
            int stockQuantity = ParseInt(GetField(row, stockIdx), 0);
            if (stockQuantity < 0) stockQuantity = 0;

            int reorderLevel = ParseInt(GetField(row, reorderLevelIdx), 5);
            if (reorderLevel < 0) reorderLevel = 5;

            // Barcode (ignore if duplicate to avoid crashing)
            string? barcode = GetField(row, barcodeIdx);
            if (!string.IsNullOrWhiteSpace(barcode))
            {
                barcode = barcode.Trim();
                if (usedBarcodes.Contains(barcode))
                {
                    barcode = null; // Do not fail product if manufacturer barcode is reused across models
                }
                else
                {
                    usedBarcodes.Add(barcode);
                }
            }
            else
            {
                barcode = null;
            }

            // Description
            string? description = GetField(row, descIdx);
            if (string.IsNullOrWhiteSpace(description)) description = null;

            // Warranty
            int warrantyYears = ParseInt(GetField(row, warrantyYearsIdx), 0);
            int warrantyMonths = ParseInt(GetField(row, warrantyMonthsIdx), 0);
            int warrantyDays = ParseInt(GetField(row, warrantyDaysIdx), 0);

            var product = Product.Create(
                name: name.Trim(),
                sku: sku,
                retailPrice: retailPrice,
                wholesalePrice: wholesalePrice,
                categoryId: category.Id,
                barcode: barcode,
                description: description,
                reorderLevel: reorderLevel,
                stockQuantity: stockQuantity,
                warrantyYears: Math.Max(0, warrantyYears),
                warrantyMonths: Math.Max(0, warrantyMonths),
                warrantyDays: Math.Max(0, warrantyDays));

            productsToInsert.Add(product);
        }

        // ── Save to Database in Atomic Transaction ───────────────────────────
        int successCount = 0;
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var product in productsToInsert)
            {
                await productRepo.AddAsync(product, ct);
                if (product.StockQuantity > 0)
                {
                    await movementRepo.AddAsync(InventoryMovement.Create(
                        productId: product.Id,
                        type: MovementType.StockIn,
                        quantity: product.StockQuantity,
                        quantityBefore: 0,
                        createdByUserId: userId,
                        referenceType: "ProductImport",
                        notes: "Initial stock from CSV import"), ct);
                }
                successCount++;
            }

            foreach (var updated in productsToUpdate)
            {
                await productRepo.UpdateAsync(updated, ct);
                successCount++;
            }
        }, ct);

        logger.LogInformation("Product CSV import finished: {Success} success, {Skipped} skipped, {Errors} errors",
            successCount, skippedCount, errors.Count);

        string summary = $"Successfully imported {successCount} product(s)" +
                         (skippedCount > 0 ? $", skipped {skippedCount} duplicate(s)" : string.Empty) +
                         (errors.Count > 0 ? $", {errors.Count} error(s) encountered." : ".");

        return new ProductImportResultDto(
            TotalRows: records.Count - 1,
            SuccessCount: successCount,
            SkippedCount: skippedCount,
            ErrorCount: errors.Count,
            Errors: errors,
            SummaryMessage: summary);
    }

    public byte[] GenerateSampleTemplateCsv()
    {
        const string template =
            "Product Name,SKU,Category,Retail Price,Wholesale Price,Stock Quantity,Reorder Level,Barcode,Description\n" +
            "Classic Foam Double Layer 72*36*4,CFDL72364,Mattress,25000.00,22000.00,10,3,10001,Double layer high density mattress\n" +
            "Executive Office Chair High Back,EOC-HB-01,Office Chair,18500.00,16000.00,5,2,10002,Ergonomic mesh chair with armrests\n" +
            "Rice Cooker 1.5KG,IRC229,Rice Cooker,8500.00,7200.00,15,5,10003,Automatic electric rice cooker with steamer\n";

        return Encoding.UTF8.GetBytes(template);
    }

    private static int FindColumnIndex(List<string> headers, params string[] candidates)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            string clean = CleanHeader(headers[i]);
            foreach (var candidate in candidates)
            {
                if (clean.Equals(CleanHeader(candidate), StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }
        return -1;
    }

    private static string CleanHeader(string header) =>
        header.Trim().ToLowerInvariant().Replace("_", " ").Replace("-", " ");

    private static string GetField(List<string> row, int index)
    {
        if (index >= 0 && index < row.Count)
            return row[index].Trim();
        return string.Empty;
    }

    private static decimal ParseDecimal(string text, decimal defaultValue)
    {
        if (string.IsNullOrWhiteSpace(text)) return defaultValue;
        // Strip currency symbols (Rs., $, commas)
        string clean = CleanCurrencyRegex().Replace(text, string.Empty);
        if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return value;
        return defaultValue;
    }

    private static int ParseInt(string text, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(text)) return defaultValue;
        string clean = CleanNonDigitsRegex().Replace(text, string.Empty);
        if (int.TryParse(clean, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return value;
        return defaultValue;
    }

    private static string NormalizeCategoryName(string raw)
    {
        // Convert "MATTRESS" -> "Mattress", "PLASTIC CHAIR" -> "Plastic Chair"
        var words = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 1)
                words[i] = char.ToUpperInvariant(words[i][0]) + words[i][1..].ToLowerInvariant();
            else
                words[i] = words[i].ToUpperInvariant();
        }
        return string.Join(" ", words);
    }

    private static string GenerateUniqueSku(string productName, ref int counter, HashSet<string> used)
    {
        // Extract 3-4 alphanumeric letters from product name
        var alphaOnly = CleanAlphaRegex().Replace(productName, string.Empty);
        string prefix = alphaOnly.Length >= 4 ? alphaOnly[..4].ToUpperInvariant() : (alphaOnly.ToUpperInvariant() + "PRD")[..4];

        string sku;
        do
        {
            sku = $"PRD-{prefix}-{counter:D4}";
            counter++;
        } while (used.Contains(sku));

        return sku;
    }

    private static string DifferentiateSku(string baseSku, HashSet<string> used)
    {
        int suffix = 1;
        string candidate;
        do
        {
            candidate = $"{baseSku}-{suffix}";
            suffix++;
        } while (used.Contains(candidate));

        return candidate;
    }

    [GeneratedRegex(@"[^\d.-]")]
    private static partial Regex CleanCurrencyRegex();

    [GeneratedRegex(@"[^\d-]")]
    private static partial Regex CleanNonDigitsRegex();

    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex CleanAlphaRegex();
}
