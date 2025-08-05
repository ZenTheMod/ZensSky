using System.IO;
using System.Text.RegularExpressions;

namespace SourceGeneration;

public static class SourceGeneration
{
    public const string ModName = "ZensSky";

    public static string CleanName(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);

            // Remove all numbers from the files name.
        name = Regex.Replace(name, "[0-9]", string.Empty);

        return name;
    }

    public static string Capitalize(string name) =>
        name[0].ToString().ToUpper() + name[1..];

    public static string Decapitalize(string name) =>
        name[0].ToString().ToLower() + name[1..];

    public static string AssetPath(string path)
    {
        string ret = Regex.Match(path, @$"(?=({ModName}[\\/])).*?(?=\.([a-z]+)$)").Value;
        ret = Regex.Replace(ret, @"[\\/]", "/");

        return ret;
    }

    public static string Namespace(string path, string name) =>
        Regex.Replace(path, @"[\\/]", ".").Replace($".{name}", string.Empty);
}
