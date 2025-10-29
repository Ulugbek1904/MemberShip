using Common.Common.Enums;

namespace Common.Common.Models;

public record DataQueryRequest
{
    public List<string>? FilteringExpression { get; set; }

    public int Skip { get; set; }

    public int Take { get; set; } = 10;

    public string? SortPropName { get; set; }

    public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}
