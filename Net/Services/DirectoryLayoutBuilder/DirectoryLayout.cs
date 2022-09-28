namespace Net.Services.DirectoryLayoutBuilder;

/// <summary>
/// Provides a way to build a directory structure.
/// </summary>
public class DirectoryLayout
{
    private string _context =
        Directory.GetCurrentDirectory();

    public DirectoryLayout() { }
    public DirectoryLayout(string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            try
            {
                Directory.CreateDirectory(rootPath);
            }
            catch
            {
                throw;
            }
        }
        _context = rootPath;
    }

    public DirectoryNode MakeTopLevel(string name)
    {
        var path = Path.Join(_context, name);
        Directory.CreateDirectory(path);
        return new DirectoryNode(this, path);
    }
}