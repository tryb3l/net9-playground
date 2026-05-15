namespace WebApp.Utils;

public static class DotEnv
{
    public static void Load(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmedLine = line.Trim();
            
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith('#'))
                continue;

            // Find the first '=' character
            var splitIndex = trimmedLine.IndexOf('=');
            if (splitIndex < 0)
                continue;

            var key = trimmedLine[..splitIndex].Trim();
            var value = trimmedLine[(splitIndex + 1)..].Trim();

            Environment.SetEnvironmentVariable(key, value);
        }
    }
}