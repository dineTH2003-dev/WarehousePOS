namespace WarehousePOS.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// Raised within aggregates and dispatched by the infrastructure layer.
/// </summary>
public interface IDomainEvent;

/// <summary>
/// Base class for aggregates — entities that own a collection of domain events.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
