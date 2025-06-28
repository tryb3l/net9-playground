using Newtonsoft.Json;

namespace WebApplication1.Helpers;

public class DataTablesResponse<T>
{
    [JsonProperty("draw")]
    public int Draw { get; set; }

    [JsonProperty("recordsTotal")]
    public int RecordsTotal { get; set; }

    [JsonProperty("recordsFiltered")]
    public int RecordsFiltered { get; set; }

    [JsonProperty("data")]
    public IEnumerable<T> Data { get; set; } = [];

    [JsonProperty("error")]
    public string? Error { get; set; }
}