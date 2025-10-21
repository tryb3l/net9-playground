using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;

namespace WebApp.Helpers;

public static class HtmlHelperExtensions
{
    public static string GetRawString(this IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
        return writer.ToString();
    }
}