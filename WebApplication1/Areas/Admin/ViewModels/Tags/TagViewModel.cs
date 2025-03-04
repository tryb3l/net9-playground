using System;

namespace WebApplication1.Areas.Admin.ViewModels.Tags;

public class TagViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PostCount { get; set; }
}
