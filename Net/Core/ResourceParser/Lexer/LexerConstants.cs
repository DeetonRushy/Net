namespace Net.Core.ResourceParser.Lexer;

public static class LexerConstants
{
    public const char QuestionMark = '?';
    public const char Ampersand = '&';

    public const char Assignment = '=';

    public const char EndOfResource = '\0';

    public static readonly List<char> ReservedCharacters
        = new ()
        {
            QuestionMark,
            Ampersand,
            Assignment
        };

    public const string ValidIdentifierCharacters =
        "_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static bool IsValidIdentifierChar(char c)
        => ValidIdentifierCharacters.Contains(c);
}