namespace Net.Services.DirectoryLayoutBuilder;

public class DirectoryNode
{
    private DirectoryLayout? _rootNode;
    private DirectoryNode? _previousNode;
    private string _context;

    public DirectoryNode(DirectoryLayout root, string context)
    {
        _rootNode = root;
        _context = context;
    }
    public DirectoryNode(DirectoryNode lastNode, string context)
    {
        _previousNode = lastNode;
        _context = context;
    }

    public DirectoryLayout ToRoot()
    {
        return _rootNode 
            ?? throw new InvalidOperationException("cannot go back into root from here.");
    }
    public DirectoryNode GoBack()
    {
        return _previousNode
            ?? throw new InvalidOperationException("cannot go back into previous directory from here.");
    }
    public DirectoryNode MakeNew(string name)
    {
        var path = Path.Join(_context, name);
        Directory.CreateDirectory(path);
        return new DirectoryNode(this, path);
    }
    public DirectoryNode MakeDotFile(string name)
    {
        File.Create($"{_context}/.{name}").Close();
        return this;
    }
}