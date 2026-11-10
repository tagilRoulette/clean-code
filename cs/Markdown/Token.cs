namespace Markdown;
public class Token
{
    public readonly IList<Token>? EmbeddedTokens;

    public TokenType Type;

    public Token(TokenType tokenType, IList<Token>? embeddedTokens)
    {
        EmbeddedTokens = embeddedTokens;
        Type = tokenType;
    }

    public void EmbedText(string text)
    {
        if (Type == TokenType.Basic)
            throw new InvalidOperationException("Can't embed Text Token into Text Token.");
        EmbeddedTokens.Add(new BasicTextToken(text));
    }
}
