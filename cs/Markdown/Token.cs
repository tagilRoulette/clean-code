namespace Markdown;
public class Token : IToken
{
    public IList<Token> EmbeddedTokens { get; }

    public string RawText { get; }

    public Token(string rawText, IList<Token> embeddedTokens)
    {
        EmbeddedTokens = embeddedTokens;
        RawText = rawText;
    }

}
