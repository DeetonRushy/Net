using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using System.Reflection;

namespace Net.Services;

/// <summary>
/// Provides a way to load all types from another module that derive from <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type to check against</typeparam>
public class PluginLoader<T>
{
    private Type[] _types;

    public PluginLoader(string file)
    {
        verifyFile(file);

        var module = Assembly.LoadFrom(file);
        _types = module.GetTypes();
    }

    /// <summary>
    /// Load each type in the specified module that is assignable to <typeparamref name="T"/>
    /// </summary>
    /// <returns>A List of <typeparamref name="T"/>, each instance has been initialized.</returns>
    public List<T> Load()
    {
        var type = typeof(T);
        var matches = _types.Where(
            t => t.IsAssignableTo(type) && t != type);

        List<T> results = new List<T>();
        foreach (var t in matches)
        {
            T? result;

            try
            {
                result = (T?)Activator.CreateInstance(t);
            }
            catch
            {
                throw;
            }

            if (result is null)
            {
                continue;
            }

            results.Add(result);
        }

        return results;
    }

    public static List<T> LoadFrom(string file)
    {
        return new PluginLoader<T>(file).Load();
    }

    private void verifyFile(string filePath)
    {
        var fileInfo = new FileInfo(filePath);

        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException(filePath);
        }

        if (fileInfo.Extension != ".dll")
        {
            throw new FileLoadException($"can only load C# class librarys [.dll] (not {fileInfo.Extension})");
        }
    }
}