namespace WarehousePOS.Domain.Exceptions;

/// <summary>
/// Base class for all domain-specific exceptions.
/// Throw these from within domain entities and aggregates when invariants are violated.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Thrown when a requested entity does not exist.</summary>
public sealed class EntityNotFoundException(string entityName, object key)
    : DomainException($"{entityName} with key '{key}' was not found.");

/// <summary>Thrown when a business rule invariant is violated.</summary>
public sealed class BusinessRuleViolationException(string rule, string details)
    : DomainException($"Business rule '{rule}' violated: {details}");

/// <summary>Thrown when a sale or purchase results in insufficient stock.</summary>
public sealed class InsufficientStockException(string productName, int requested, int available)
    : DomainException($"Insufficient stock for '{productName}'. Requested: {requested}, Available: {available}");
