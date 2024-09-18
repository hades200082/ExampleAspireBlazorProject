using System.ComponentModel.DataAnnotations;

namespace Domain.Abstractions;

public abstract class AuditableSimpleEntity<TId> : SimpleEntity<TId>, IAuditableEntity
    where TId : IComparable
{
    [Required]
    public DateTime CreatedAt { get; set; }
    
    [Required]
    public DateTime UpdatedAt { get; set; }
}