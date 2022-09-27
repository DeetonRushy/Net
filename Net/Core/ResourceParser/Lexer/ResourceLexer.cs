using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Net.Core.ResourceParser.Lexer.Exceptions;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace Net.Core.ResourceParser.Lexer;

public class ResourceLexer
{
    private readonly List<Token> _tokens;
    private int _position;

    private string _contents;

    bool _isAtEnd = false;

    public ResourceLexer()
    {
        _tokens = new List<Token>();
        _position = -1;
        _contents = string.Empty;
    }

    public List<Token> Lex(string Contents)
    {
        _contents = Contents + '\0';
        
        while (true)
        {
            if (_isAtEnd)
                break;

            var token = LexSingleToken();

            if (token.TokenType == TokenType.Eor)
            {
                _tokens.Add(token);
                break;
            }

            _tokens.Add(token);
        }

        return _tokens;
    }

    private Token LexSingleToken()
    {
        var ch = Advance();

        return ch switch
        {
            LexerConstants.Ampersand => MakeToken(TokenType.Ampersand, "&"),
            LexerConstants.Assignment => MakeToken(TokenType.Equals, "="),
            LexerConstants.QuestionMark => MakeToken(TokenType.QuestionMark, "?"),
            LexerConstants.EndOfResource => MakeToken(TokenType.Eor, "\0"),
            var c when !LexerConstants.ReservedCharacters.Contains(c) => LexLiteral(),
            _ => throw new LexerException($"failed to lex position '{_position}' in string '{_contents}'"),
        };
    }

    private Token LexLiteral()
    {
        var last = LastToken();

        if (last == null)
        {
            // This is the first identifier of the resource.
            // help?index=2
            // ^^^^

            return LexEventId();
        }

        if (last.TokenType == TokenType.QuestionMark
            || last.TokenType == TokenType.Ampersand)
        {
            return LexPropertyIdentifier();
        }

        if (last.TokenType == TokenType.Equals)
        {
            return LexPropertyValue();
        }

        if (last.TokenType == TokenType.Value)
        {

        }

        throw new LexerException("Unknown literal sequence");
    }

    private Token LexPropertyValue()
    {
        // Values are allowed to be anything (other than reserved characters when not in a string).
        // They are all stored as strings anyway.

        string lexeme = string.Empty;
        char c = Current();

        if (c == LexerConstants.StringLiteralDelim)
        {
            c = Advance();

            while (c != LexerConstants.StringLiteralDelim)
            {
                lexeme += c;
                c = Advance();

                if (c == LexerConstants.EndOfResource)
                {
                    throw new LexerException("unterminated string literal in resource");
                }
            }

            // It could be an empty string
            // Also:
            _position += 1;
            return MakeToken(TokenType.Identifier, lexeme);
        }
        else
        {
            while (!LexerConstants.ReservedCharacters.Contains(c))
            {
                lexeme += c;
                c = Advance();

                if (c == LexerConstants.EndOfResource)
                {
                    _isAtEnd = true;
                    break;
                }
            }
        }

        if (lexeme.Length == 0)
        {
            // Looks like this: 'resource?property=
            //                                    ^^^
            //                               Needs value

            throw new LexerException($"Bad property value at {_position}");
        }

        // deduct an index to make up for our advances
        _position -= 1;
        return MakeToken(TokenType.Identifier, lexeme);
    }

    private Token LexPropertyIdentifier()
    {
        // Must be text only.

        string lexeme = string.Empty;
        char c = Current();

        while (LexerConstants.IsValidIdentifierChar(c))
        {
            lexeme += c;
            c = Advance();

            if (c == LexerConstants.EndOfResource)
                throw new LexerException("Unexpected end of resource");
        }

        if (!LexerConstants.ReservedCharacters.Contains(c))
        {
            // used something not within the bounds ('a-Z_')
            Trace.TraceWarning($"You may have used a dis-allowed character in your property identifier at position {_position}. (context: '{_contents}')");
        }

        if (lexeme.Length == 0)
        {
            // Looks like this: 'resource?=value
            //                           ^^^
            //                    Needs identifier

            throw new LexerException("Bad identifier. (Must be a-Z)");
        }

        // deduct an index to make up for our advances
        _position -= 1;
        return MakeToken(TokenType.Identifier, lexeme);
    }

    private Token LexEventId()
    {
        string lexeme = string.Empty;
        char c = Current();

        while (LexerConstants.IsValidIdentifierChar(c))
        {
            lexeme += c;
            c = Advance();

            if (c == LexerConstants.EndOfResource)
                throw new LexerException("Unexpected end of resource");
        }

        if (lexeme.Length == 0)
        {
            // Looks like this: '?property=value
            //                 ^^^
            //                 Needs identifier

            throw new LexerException("Bad identifier");
        }

        // deduct an index to make up for our advances
        _position -= 1;
        return MakeToken(TokenType.EventIdentifier, lexeme);
    }

    private char Advance()
    {
        _position++;

        if (_position >= _contents.Length)
            return LexerConstants.EndOfResource;

        return _contents[_position];
    }

    private char Current()
    {
        if (_position < 0)
            return LexerConstants.EndOfResource;
        return _contents[_position];
    }

    private Token MakeToken(TokenType type, string Lexeme)
    {
        return new Token { Position = _position, Lexeme = Lexeme, TokenType = type };
    }

    private Token? LastToken()
    {
        if (_tokens.Count == 0)
            return null;
        return _tokens[^1];
    }
}