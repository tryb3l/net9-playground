using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApp.Helpers;

public class DataTablesRequestModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProvider = bindingContext.ValueProvider;

        var model = new DataTablesRequest
        {
            Draw = int.Parse(valueProvider.GetValue("draw").FirstValue ?? "0"),
            Start = int.Parse(valueProvider.GetValue("start").FirstValue ?? "0"),
            Length = int.Parse(valueProvider.GetValue("length").FirstValue ?? "10"),
            Search = new Search
            {
                Value = valueProvider.GetValue("search[value]").FirstValue,
                Regex = bool.Parse(valueProvider.GetValue("search[regex]").FirstValue ?? "false")
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
                Orderable = bool.Parse(valueProvider.GetValue($"columns[{colIndex}][orderable]").FirstValue ?? "false"),
                Searchable = bool.Parse(valueProvider.GetValue($"columns[{colIndex}][searchable]").FirstValue ?? "false"),
                Search = new Search
                {
                    Value = valueProvider.GetValue($"columns[{colIndex}][search][value]").FirstValue,
                    Regex = bool.Parse(valueProvider.GetValue($"columns[{colIndex}][search][regex]").FirstValue ?? "false")
                }
            });
            colIndex++;
        }

        var orderIndex = 0;
        while (valueProvider.GetValue($"order[{orderIndex}][column]").Length != 0)
        {
            model.Order.Add(new Order
            {
                Column = int.Parse(valueProvider.GetValue($"order[{orderIndex}][column]").FirstValue ?? "0"),
                Dir = valueProvider.GetValue($"order[{orderIndex}][dir]").FirstValue
            });
            orderIndex++;
        }

        bindingContext.Result = ModelBindingResult.Success(model);
        return Task.CompletedTask;
    }
}