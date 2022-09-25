using Net.Core.Messages;
using Net.Core.ResourceParser.Lexer;
using Net.Core.ResourceParser.Lexer.Exceptions;
using Newtonsoft.Json.Linq;

namespace Net.Core.ResourceParser;

public class ResourceConversionEngine<T> where T: class, INetMessage, new()
{
    private ResourceLexer resourceLexer;
    private T? message;
    private List<Token> _tokens;
    private int _index = -1;

    public ResourceConversionEngine()
    {
        resourceLexer = new ResourceLexer();
        _tokens = new();
    }

    public T? Parse(string resource)
    {
        List<Token> tokens;

        try
        {
            tokens = resourceLexer.Lex(resource);
        }
        catch (LexerException)
        {
#if DEBUG
            throw;
#else
            return default;
#endif
        }

        _tokens = tokens;
        BuildMessage();

        return message;
    }

    public void Reset()
    {
        resourceLexer = new();
        message = null;
        _tokens = new();
        _index = -1;
    }

    private void BuildMessage()
    {
        message = new();

        while (Advance().TokenType != TokenType.Eor)
        {
            ProcessToken();
        }
    }

    private void ProcessToken()
    {
        var c = Current();

        switch (c.TokenType)
        {
            case TokenType.EventIdentifier:
                ProcessEventIdentifier();
                break;
            case TokenType.Identifier:
                ProcessProperty();
                break;
            default:
                break;
        }
    }

    private void ProcessProperty()
    {
        var property = Current();
        var value = Advance(2);

        if (SpecialProperties.Contains(property.Lexeme))
        {
            ProcessSpecialProperty(property, value);
            return;
        }

        message ??= new T();
        message.Properties.Add(property.Lexeme, value.Lexeme);
    }

    private void ProcessSpecialProperty(Token property, Token value)
    {
        message ??= new T();

        /*handle the case of WantsRequest being set*/
        if (property.Lexeme == "iiwr")
        {
            if (!bool.TryParse(value.Lexeme, out var wantsResponse))
            {
                if (value.Lexeme == "0")
                {
                    message!.WantsResponse = false;
                    return;
                }

                if (value.Lexeme == "1")
                {
                    message!.WantsResponse = true;
                    return;
                }

                throw new InvalidDataException($"Bad value for WantsResponse in resource string (value: '{value.Lexeme}')");
            }

            message!.WantsResponse = wantsResponse;
        }
    }

    private void ProcessEventIdentifier()
    {
        message ??= new T();

        message.EventId = Current().Lexeme;
    }

    private Token Current()
    {
        if (_tokens.Count <= _index)
            return new Token { TokenType = TokenType.Eor };
        return _tokens[_index];
    }

    private Token Advance(int a = 1)
    {
        _index += a;
        if (_index >= _tokens.Count)
            // some weird shit
            return new Token { TokenType = TokenType.Eor };
        return _tokens[_index];
    }

    private List<string> SpecialProperties { get; }
            = new()
            {
                "iiwr", /*WantsResponse*/
            };

    public static T? ParseResource(string resource)
    {
        return new ResourceConversionEngine<T>().Parse(resource);
    }
}