namespace Markdown;
public class Token : IToken
{
    public IList<Token> EmbeddedTokens { get; }

    public string RawText { get; }

    public TokenType TokenType { get; }

    public Token(string rawText, IList<Token> embeddedTokens, TokenType tokenType)
    {
        EmbeddedTokens = embeddedTokens;
        RawText = rawText;
        TokenType = tokenType;
    }
}
