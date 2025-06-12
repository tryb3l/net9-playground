using System;

namespace WebApplication1.Areas.Admin.ViewModels.Tag;

public class TagViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int PostCount { get; init; }
}
