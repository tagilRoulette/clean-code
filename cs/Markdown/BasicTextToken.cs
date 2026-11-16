namespace Markdown;
internal class BasicTextToken : Token
{
    public readonly string Text;

    public BasicTextToken(string text)
        : base(tokenType: TokenType.Basic, embeddedTokens: null)
    {
        Text = text;
    }

    public override string ToString(Func<TokenType, (string openingSymbol, string closingSymbol)> formatter)
    {
        return Text;
    }
}
