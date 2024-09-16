namespace PbDori.Helpers;

public static class PathHelpers
{
    public static string GetFullPathWindowsCompatible(string path)
    {
        path = path.Replace("\\", "/");
        if(path.Contains(":"))
            return path; // already a full path
        if (path.StartsWith("."))
            return path;
        path = Path.GetFullPath(path);
        return path;
    }
}
