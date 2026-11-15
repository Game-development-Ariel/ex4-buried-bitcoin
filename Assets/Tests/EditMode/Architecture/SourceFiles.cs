using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Reads the game's .cs files for the architecture tests, with the comments taken out,
/// so a word in a comment never counts as code.
/// </summary>
public static class SourceFiles
{
    public static readonly string ScriptsFolder = Path.GetFullPath("Assets/Scripts");

    private static readonly Regex BlockComment = new Regex(@"/\*.*?\*/", RegexOptions.Singleline);
    private static readonly Regex LineComment = new Regex(@"//[^\n]*");

    /// <summary>Every script, as a path relative to Assets/Scripts with forward slashes, like "UI/HudStats.cs".</summary>
    public static IEnumerable<string> All()
    {
        return Directory.GetFiles(ScriptsFolder, "*.cs", SearchOption.AllDirectories)
            .Select(path => path.Substring(ScriptsFolder.Length + 1).Replace('\\', '/'))
            .OrderBy(path => path);
    }

    /// <summary>The scripts that run in the game (everything but the editor tools).</summary>
    public static IEnumerable<string> Runtime()
    {
        return All().Where(path => !path.StartsWith("Editor/"));
    }

    public static string Code(string relativePath)
    {
        string text = File.ReadAllText(Path.Combine(ScriptsFolder, relativePath)).Replace("\r\n", "\n");
        return LineComment.Replace(BlockComment.Replace(text, ""), "");
    }

    /// <summary>The body of a method, between its braces. Empty if the file has no such method.</summary>
    public static string MethodBody(string code, string method)
    {
        Match start = Regex.Match(code, @"void\s+" + method + @"\s*\([^)]*\)\s*\{");
        if (!start.Success)
        {
            return "";
        }
        int depth = 1;
        int i = start.Index + start.Length;
        int bodyStart = i;
        while (i < code.Length && depth > 0)
        {
            if (code[i] == '{')
            {
                depth++;
            }
            else if (code[i] == '}')
            {
                depth--;
            }
            i++;
        }
        return code.Substring(bodyStart, i - bodyStart - 1);
    }

    public static string TypeName(string relativePath)
    {
        return Path.GetFileNameWithoutExtension(relativePath);
    }
}
