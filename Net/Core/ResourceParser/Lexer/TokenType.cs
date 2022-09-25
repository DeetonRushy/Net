namespace Net.Core.ResourceParser.Lexer;

/*
 * Example URLs:
 * 
 * Basic: onready
 * HasArguments: onready?token=kdwadjaww82
 * HasMultipleArguments: onready?token=blablabla&use_proxy=true
 * 
 * Bad URLs:
 * 
 * onready=92 (no eventId specified)
 */

public enum TokenType : byte
{
    /// <summary>
    /// The token in the event identifier.
    /// </summary>
    EventIdentifier,
    /// <summary>
    /// The token is a property identifier.
    /// </summary>
    Identifier,
    /// <summary>
    /// The token is a value, occuring on the right of the '=' character.
    /// </summary>
    Value,
    /// <summary>
    /// The token is the '&' symbol.
    /// </summary>
    Ampersand,

    /// <summary>
    /// The token is a '=' character.
    /// </summary>
    Equals,

    /// <summary>
    /// The token is a '?'. This signifies the first property is being defined.
    /// </summary>
    QuestionMark,

    /// <summary>
    /// The end of the resource string.
    /// </summary>
    Eor
}