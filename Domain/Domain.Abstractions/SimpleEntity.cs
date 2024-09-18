using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Domain.Abstractions;

public abstract class SimpleEntity<TId> : IEntity<TId>
    where TId : IComparable
{
    [Key]
    public virtual TId Id { get; set; } = default!;

    public virtual string PartitionKey => GetType().Name;

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}