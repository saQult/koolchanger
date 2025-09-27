using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSLOLTool.Services;

// made by random guy from lolru discord
public static class Sanitizer
{
    static string CleanBase(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";

        var cleaned = Regex.Replace(s, @"[^A-Za-z0-9_-]+", "_");

        cleaned = Regex.Replace(cleaned, @"_+", "_");

        cleaned = cleaned.Trim('_', '-');

        return cleaned;
    }

    static string EnsureUniquePath(string targetPath)
    {
        if (!File.Exists(targetPath) && !Directory.Exists(targetPath))
            return targetPath;

        string dir = Path.GetDirectoryName(targetPath)!;
        string name = Path.GetFileName(targetPath);
        string baseName = name;
        string? ext = null;

        if (File.Exists(targetPath))
        {
            ext = Path.GetExtension(name);
            baseName = Path.GetFileNameWithoutExtension(name);
        }

        int i = 1;
        while (true)
        {
            string candidate = ext is null
                ? Path.Combine(dir, $"{baseName}-{i}")
                : Path.Combine(dir, $"{baseName}-{i}{ext}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
                return candidate;
            i++;
        }
    }

    public static void SanitizeTree(string root)
    {
        if (!Directory.Exists(root))
            return;

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            try
            {
                string dir = Path.GetDirectoryName(file)!;
                string ext = Path.GetExtension(file);
                string cleanBase = CleanBase(Path.GetFileNameWithoutExtension(file));
                string newPath = Path.Combine(dir, cleanBase + ext);

                if (!string.Equals(file, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    newPath = EnsureUniquePath(newPath);
                    File.Move(file, newPath);
                }
            }
            catch (Exception)
            {
            }
        }

        var allDirs = Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories);
        var dirsSorted = new List<string>(allDirs);
        dirsSorted.Sort((a, b) => b.Length.CompareTo(a.Length));

        foreach (var dirPath in dirsSorted)
        {
            try
            {
                string parent = Path.GetDirectoryName(dirPath)!;
                string clean = CleanBase(Path.GetFileName(dirPath));
                if (string.IsNullOrEmpty(clean)) clean = "folder";

                string newPath = Path.Combine(parent, clean);

                if (!string.Equals(dirPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    newPath = EnsureUniquePath(newPath);
                    Directory.Move(dirPath, newPath);
                }
            }
            catch (Exception)
            {
            }
        }
        try
        {
            string? parent = Path.GetDirectoryName(root);
            if (!string.IsNullOrEmpty(parent))
            {
                string clean = CleanBase(Path.GetFileName(root));
                if (string.IsNullOrEmpty(clean)) clean = "root";
                string newRoot = Path.Combine(parent, clean);
                if (!string.Equals(root, newRoot, StringComparison.OrdinalIgnoreCase))
                {
                    string unique = EnsureUniquePath(newRoot);
                    Directory.Move(root, unique);
                }
            }
        }
        catch (Exception)
        {
        }
    }
}
