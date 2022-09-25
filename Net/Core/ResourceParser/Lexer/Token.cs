namespace Net.Core.ResourceParser.Lexer;

public class Token
{
    public string Lexeme { get; set; } = string.Empty;
    public int Position { get; set; }
    public TokenType TokenType { get; set; }

    public override string ToString()
    {
        return $"({TokenType}) '{Lexeme}' at {Position}";
    }
}