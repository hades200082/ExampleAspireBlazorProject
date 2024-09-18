namespace Shared.Abstractions;

public interface IPagedDataSet<T>
{
    IEnumerable<T> Items { get; init; }
    
    int TotalRecords { get; init; }
    
    int CurrentPage { get; init; }
    
    int TotalPages { get; init; }
}

public record PagedDataSet<T>(IEnumerable<T> Items, int TotalRecords, int CurrentPage, int TotalPages) : IPagedDataSet<T>;