using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using WarehousePOS.Application.Common;
using WarehousePOS.Domain.Common;
using WarehousePOS.Domain.Entities;
using WarehousePOS.Domain.Interfaces;

namespace WarehousePOS.Application.Suppliers;

public sealed class SupplierImportService(
    ISupplierRepository supplierRepo,
    IProductRepository productRepo,
    ICategoryRepository categoryRepo,
    IUnitOfWork unitOfWork,
    ILogger<SupplierImportService> logger) : ISupplierImportService
{
    private static readonly char[] ProductDelimiters = [',', ';', '|'];

    public async Task<SupplierImportResultDto> ImportCsvAsync(
        Stream csvStream,
        SupplierImportOptions options,
        int userId = 1,
        CancellationToken ct = default)
    {
        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var records = CsvParser.Parse(reader);

        if (records.Count == 0)
        {
            return new SupplierImportResultDto(
                TotalRows: 0,
                SuccessCount: 0,
                UpdatedCount: 0,
                SkippedCount: 0,
                ErrorCount: 1,
                Errors: [new SupplierImportErrorDto(0, string.Empty, "The uploaded CSV file is empty.")],
                SummaryMessage: "The uploaded CSV file is empty.");
        }

        // ── Map headers ───────────────────────────────────────────────────────
        var headers = records[0];
        int nameIdx = FindColumnIndex(headers, "supplier name", "suppliername", "supplier", "name", "vendor name", "vendor");
        int contactIdx = FindColumnIndex(headers, "contact person", "contactperson", "contact", "contact name", "person");
        int phoneIdx = FindColumnIndex(headers, "phone", "phone number", "phonenumber", "telephone", "mobile", "mobile number");
        int emailIdx = FindColumnIndex(headers, "email", "email address", "emailaddress", "mail");
        int addressIdx = FindColumnIndex(headers, "address", "location", "street", "street address");
        int productsIdx = FindColumnIndex(headers, "provided products", "providedproducts", "products", "supply products", "supplied products", "items", "supplied items");
        int balanceIdx = FindColumnIndex(headers, "balance", "opening balance", "openingbalance", "initial balance");

        if (nameIdx == -1)
        {
            return new SupplierImportResultDto(
                TotalRows: records.Count - 1,
                SuccessCount: 0,
                UpdatedCount: 0,
                SkippedCount: 0,
                ErrorCount: 1,
                Errors: [new SupplierImportErrorDto(1, string.Empty, "Missing required column 'Supplier Name' or 'Name' in CSV header.")],
                SummaryMessage: "CSV header is missing the required 'Supplier Name' column.");
        }

        // ── Pre-load database state for lookups ────────────────────────────────
        var existingSuppliers = await supplierRepo.GetAllAsync(ct);
        var supplierMap = existingSuppliers.ToDictionary(s => s.Name.Trim(), s => s, StringComparer.OrdinalIgnoreCase);

        var existingProducts = await productRepo.GetAllAsync(ct);
        var productByNameOrSku = new Dictionary<string, Product>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in existingProducts)
        {
            if (!productByNameOrSku.ContainsKey(p.Name)) productByNameOrSku[p.Name] = p;
            if (!productByNameOrSku.ContainsKey(p.SKU)) productByNameOrSku[p.SKU] = p;
        }

        var existingCategories = await categoryRepo.GetAllAsync(ct);
        var defaultCategory = existingCategories.FirstOrDefault(c => c.Name.Equals("General", StringComparison.OrdinalIgnoreCase))
                               ?? existingCategories.FirstOrDefault();

        if (defaultCategory is null && options.AutoCreateMissingProducts)
        {
            defaultCategory = Category.Create("General", "Auto-created category for imported products");
            await categoryRepo.AddAsync(defaultCategory, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        var errors = new List<SupplierImportErrorDto>();
        int createdCount = 0;
        int updatedCount = 0;
        int skippedCount = 0;
        int autoSkuCounter = 1;

        // Group rows by Supplier Name so multi-row product listings merge seamlessly
        var supplierGroups = new Dictionary<string, SupplierGroupData>(StringComparer.OrdinalIgnoreCase);

        for (int r = 1; r < records.Count; r++)
        {
            var row = records[r];
            int rowNumber = r + 1;

            string name = GetField(row, nameIdx);
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add(new SupplierImportErrorDto(rowNumber, string.Empty, "Supplier name cannot be empty."));
                continue;
            }

            string? contact = contactIdx >= 0 ? NullIfEmpty(GetField(row, contactIdx)) : null;
            string? phone = phoneIdx >= 0 ? NullIfEmpty(GetField(row, phoneIdx)) : null;
            string? email = emailIdx >= 0 ? NullIfEmpty(GetField(row, emailIdx)) : null;
            string? address = addressIdx >= 0 ? NullIfEmpty(GetField(row, addressIdx)) : null;
            string? productsRaw = productsIdx >= 0 ? NullIfEmpty(GetField(row, productsIdx)) : null;
            string? balanceStr = balanceIdx >= 0 ? NullIfEmpty(GetField(row, balanceIdx)) : null;

            // Validate phone per domain rule (10 digits starting with '0')
            if (phone is not null)
            {
                var cleanPhone = phone.Replace(" ", "").Replace("-", "");
                if (cleanPhone.Length != 10 || !cleanPhone.StartsWith('0') || cleanPhone.Any(c => c is < '0' or > '9'))
                {
                    errors.Add(new SupplierImportErrorDto(rowNumber, name, $"Invalid phone '{phone}'. Phone number must consist of 10 digits starting with '0'."));
                    continue;
                }
                phone = cleanPhone;
            }

            // Validate email syntax
            if (email is not null && !new EmailAddressAttribute().IsValid(email))
            {
                errors.Add(new SupplierImportErrorDto(rowNumber, name, $"Invalid email address '{email}'."));
                continue;
            }

            decimal balance = 0m;
            if (balanceStr is not null)
            {
                if (decimal.TryParse(balanceStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedBalance))
                {
                    balance = parsedBalance;
                }
                else
                {
                    errors.Add(new SupplierImportErrorDto(rowNumber, name, $"Invalid opening balance '{balanceStr}'. Must be a valid number."));
                    continue;
                }
            }

            // Parse provided products
            var extractedProducts = new List<string>();
            if (!string.IsNullOrWhiteSpace(productsRaw))
            {
                var tokens = productsRaw.Split(ProductDelimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                extractedProducts.AddRange(tokens);
            }

            if (!supplierGroups.TryGetValue(name, out var group))
            {
                group = new SupplierGroupData
                {
                    Name = name.Trim(),
                    ContactPerson = contact,
                    Phone = phone,
                    Email = email,
                    Address = address,
                    OpeningBalance = balance
                };
                supplierGroups[name] = group;
            }
            else
            {
                // Update contact info if previous row was blank
                group.ContactPerson ??= contact;
                group.Phone ??= phone;
                group.Email ??= email;
                group.Address ??= address;
                if (group.OpeningBalance == 0m && balance != 0m)
                {
                    group.OpeningBalance = balance;
                }
            }

            foreach (var prod in extractedProducts)
            {
                if (!group.ProvidedProducts.Contains(prod, StringComparer.OrdinalIgnoreCase))
                {
                    group.ProvidedProducts.Add(prod);
                }
            }
        }

        // ── Process suppliers and catalog items ────────────────────────────────
        foreach (var group in supplierGroups.Values)
        {
            // Auto-create missing products in catalog if requested
            if (options.AutoCreateMissingProducts && defaultCategory is not null)
            {
                foreach (var productName in group.ProvidedProducts)
                {
                    if (!productByNameOrSku.ContainsKey(productName))
                    {
                        string sku = GenerateSku(productName, autoSkuCounter++, productByNameOrSku);
                        var newProduct = Product.Create(
                            name: productName,
                            sku: sku,
                            retailPrice: 0m,
                            wholesalePrice: 0m,
                            categoryId: defaultCategory.Id,
                            description: $"Auto-created from supplier CSV import ({group.Name})",
                            stockQuantity: 0,
                            reorderLevel: 5);

                        await productRepo.AddAsync(newProduct, ct);
                        productByNameOrSku[newProduct.Name] = newProduct;
                        productByNameOrSku[newProduct.SKU] = newProduct;
                        logger.LogInformation("Auto-created missing product '{Product}' (SKU: {Sku}) for supplier '{Supplier}'",
                            productName, sku, group.Name);
                    }
                }
            }

            // Check if supplier exists
            if (supplierMap.TryGetValue(group.Name, out var existingSupplier))
            {
                if (!options.UpdateExisting)
                {
                    skippedCount++;
                    continue;
                }

                // Update contact info if new data is provided
                existingSupplier.Update(
                    name: group.Name,
                    contactPerson: group.ContactPerson ?? existingSupplier.ContactPerson,
                    phone: group.Phone ?? existingSupplier.Phone,
                    email: group.Email ?? existingSupplier.Email,
                    address: group.Address ?? existingSupplier.Address,
                    providedProducts: existingSupplier.ProvidedProducts);

                // Merge provided products
                if (group.ProvidedProducts.Count > 0)
                {
                    existingSupplier.AddProvidedProducts(group.ProvidedProducts);
                }

                await supplierRepo.UpdateAsync(existingSupplier, ct);
                updatedCount++;
            }
            else
            {
                string providedStr = string.Join(", ", group.ProvidedProducts);
                var newSupplier = Supplier.Create(
                    name: group.Name,
                    contactPerson: group.ContactPerson,
                    phone: group.Phone,
                    email: group.Email,
                    address: group.Address,
                    providedProducts: string.IsNullOrWhiteSpace(providedStr) ? null : providedStr);

                if (group.OpeningBalance != 0m)
                {
                    newSupplier.AddToBalance(group.OpeningBalance);
                }

                await supplierRepo.AddAsync(newSupplier, ct);
                supplierMap[newSupplier.Name] = newSupplier;
                createdCount++;
            }
        }

        // Commit all changes in a single atomic transaction
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Supplier CSV import completed: {Created} created, {Updated} updated, {Skipped} skipped, {Errors} errors",
            createdCount, updatedCount, skippedCount, errors.Count);

        string summary = $"Processed {records.Count - 1} row(s): {createdCount} supplier(s) created, {updatedCount} updated, {skippedCount} skipped, {errors.Count} error(s).";

        return new SupplierImportResultDto(
            TotalRows: records.Count - 1,
            SuccessCount: createdCount,
            UpdatedCount: updatedCount,
            SkippedCount: skippedCount,
            ErrorCount: errors.Count,
            Errors: errors,
            SummaryMessage: summary);
    }

    public byte[] GenerateSampleTemplateCsv()
    {
        var sb = new StringBuilder();
        // UTF-8 BOM
        sb.Append('\uFEFF');
        sb.AppendLine("Supplier Name,Contact Person,Phone,Email,Address,Provided Products,Opening Balance");
        sb.AppendLine("\"Ceylon Beverage Distributors\",\"Kamal Perera\",\"0712345678\",\"kamal@ceylonbev.lk\",\"123 Galle Road, Colombo 03\",\"Coca Cola 1.5L; Sprite 1.5L; Fanta 1.5L\",\"0.00\"");
        sb.AppendLine("\"Lanka Stationery Supplies\",\"Nimal Silva\",\"0779876543\",\"sales@lankastationery.com\",\"45 Kandy Road, Kelaniya\",\"Atlas CR Book 120pg; Ballpoint Pen Blue; A4 Paper 80gsm\",\"5000.00\"");
        sb.AppendLine("\"Metro Hardware Traders\",\"Sunil Fernando\",\"0721112233\",\"info@metrohardware.lk\",\"78 Main Street, Kandy\",\"Hammer Steel 500g; Screwdriver Set 6pc; Measuring Tape 5m\",\"0.00\"");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static int FindColumnIndex(List<string> headers, params string[] candidates)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            var h = headers[i].Trim().ToLowerInvariant();
            foreach (var c in candidates)
            {
                if (h == c) return i;
            }
        }
        return -1;
    }

    private static string GetField(List<string> row, int index)
    {
        return index >= 0 && index < row.Count ? row[index].Trim() : string.Empty;
    }

    private static string? NullIfEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string GenerateSku(string productName, int counter, Dictionary<string, Product> existing)
    {
        var baseCode = new string(productName
            .Where(char.IsLetterOrDigit)
            .Take(4)
            .ToArray())
            .ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(baseCode)) baseCode = "ITEM";

        string sku;
        do
        {
            sku = $"SKU-{baseCode}-{counter:D4}";
            counter++;
        } while (existing.ContainsKey(sku));

        return sku;
    }

    private sealed class SupplierGroupData
    {
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public decimal OpeningBalance { get; set; }
        public List<string> ProvidedProducts { get; } = [];
    }
}
