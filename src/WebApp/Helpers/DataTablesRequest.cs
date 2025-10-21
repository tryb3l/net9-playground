using Microsoft.AspNetCore.Mvc;

namespace WebApp.Helpers;

[ModelBinder(typeof(DataTablesRequestModelBinder))]
public class DataTablesRequest
{
    public int Draw { get; init; }
    public int Start { get; init; }
    public int Length { get; init; }
    public Search? Search { get; init; }
    public List<Column> Columns { get; init; } = [];
    public List<Order> Order { get; init; } = [];
    public string? StatusFilter { get; init; }

}

public class Search
{
    public string? Value { get; init; }
    public bool Regex { get; init; }
}

public class Column
{
    public string? Data { get; init; }
    public string? Name { get; init; }
    public bool Searchable { get; init; }
    public bool Orderable { get; init; }
    public Search? Search { get; init; }
}

public class Order
{
    public int Column { get; init; }
    public string? Dir { get; init; }
}