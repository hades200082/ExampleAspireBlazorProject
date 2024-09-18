namespace Domain.Abstractions;

public interface IEntity<TId> where TId : IComparable
{
    TId Id { get; set; }
    string PartitionKey { get; }
}