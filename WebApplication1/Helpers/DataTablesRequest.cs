using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Helpers;

[ModelBinder(typeof(DataTablesRequestModelBinder))]
public class DataTablesRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public Search? Search { get; set; }
    public List<Column> Columns { get; set; } = new();
    public List<Order> Order { get; set; } = new();
    public string? StatusFilter { get; set; }

}

public class Search
{
    public string? Value { get; set; }
    public bool Regex { get; set; }
}

public class Column
{
    public string? Data { get; set; }
    public string? Name { get; set; }
    public bool Searchable { get; set; }
    public bool Orderable { get; set; }
    public Search? Search { get; set; }
}

public class Order
{
    public int Column { get; set; }
    public string? Dir { get; set; }
}