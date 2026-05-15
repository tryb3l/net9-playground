using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApp.Helpers;

public class DataTablesRequestModelBinder : IModelBinder
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProvider = bindingContext.ValueProvider;

        var model = new DataTablesRequest
        {
            Draw = ParseInt(valueProvider.GetValue("draw").FirstValue, 0, min: 0),
            Start = ParseInt(valueProvider.GetValue("start").FirstValue, 0, min: 0),
            Length = ParseInt(valueProvider.GetValue("length").FirstValue, DefaultPageSize, min: 1, max: MaxPageSize),
            Search = new Search
            {
                Value = valueProvider.GetValue("search[value]").FirstValue,
                Regex = ParseBool(valueProvider.GetValue("search[regex]").FirstValue)
            },
            StatusFilter = valueProvider.GetValue("statusFilter").FirstValue
        };

        var colIndex = 0;
        while (valueProvider.GetValue($"columns[{colIndex}][data]").Length != 0)
        {
            model.Columns.Add(new Column
            {
                Data = valueProvider.GetValue($"columns[{colIndex}][data]").FirstValue,
                Name = valueProvider.GetValue($"columns[{colIndex}][name]").FirstValue,
                Orderable = ParseBool(valueProvider.GetValue($"columns[{colIndex}][orderable]").FirstValue),
                Searchable = ParseBool(valueProvider.GetValue($"columns[{colIndex}][searchable]").FirstValue),
                Search = new Search
                {
                    Value = valueProvider.GetValue($"columns[{colIndex}][search][value]").FirstValue,
                    Regex = ParseBool(valueProvider.GetValue($"columns[{colIndex}][search][regex]").FirstValue)
                }
            });
            colIndex++;
        }

        var orderIndex = 0;
        while (valueProvider.GetValue($"order[{orderIndex}][column]").Length != 0)
        {
            var orderColumn = ParseInt(valueProvider.GetValue($"order[{orderIndex}][column]").FirstValue, 0, min: 0);
            model.Order.Add(new Order
            {
                Column = orderColumn,
                Dir = ParseSortDirection(valueProvider.GetValue($"order[{orderIndex}][dir]").FirstValue)
            });
            orderIndex++;
        }

        bindingContext.Result = ModelBindingResult.Success(model);
        return Task.CompletedTask;
    }

    private static int ParseInt(string? value, int fallback, int? min = null, int? max = null)
    {
        if (!int.TryParse(value, out var parsed))
        {
            return fallback;
        }

        if (min.HasValue && parsed < min.Value)
        {
            return min.Value;
        }

        if (max.HasValue && parsed > max.Value)
        {
            return max.Value;
        }

        return parsed;
    }

    private static bool ParseBool(string? value)
        => bool.TryParse(value, out var parsed) && parsed;

    private static string ParseSortDirection(string? value)
        => string.Equals(value, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
}