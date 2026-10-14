namespace WebApp.Utils;

// Single source for the stored image variants: AttachmentService writes them, srcset descriptors advertise them.
public static class ImageVariants
{
    public const string Large = "large";
    public const string Medium = "medium";
    public const string Thumbnail = "thumbnail";

    public const int EditorWidth = 1920;

    // Width-bound only; height follows the aspect ratio and images are never upscaled.
    public static readonly IReadOnlyList<(string Name, int Width)> Featured =
    [
        (Large, 1920),
        (Medium, 800),
        (Thumbnail, 400)
    ];
}
