using System.Text;

namespace Markdown;
public class Token
{
    public readonly IList<Token>? EmbeddedTokens;
    public TokenType Type;
    public bool IsInWord;

    public virtual bool HasTokenizedNonNumericSymbols => EmbeddedTokens.Any(t => t.HasTokenizedNonNumericSymbols);

    public Token(TokenType tokenType, IList<Token>? embeddedTokens, bool isInWord = false)
    {
        EmbeddedTokens = embeddedTokens;
        Type = tokenType;
        IsInWord = isInWord;
    }

    public void EmbedText(string text)
    {
        if (GetType() == typeof(BasicTextToken))
            throw new InvalidOperationException("Can't embed Text Token into Text Token.");
        EmbeddedTokens.Add(new BasicTextToken(text));
    }

    public virtual string ToString(Func<TokenType, (string openingSymbol, string closingSymbol)> formatter)
    {
        (string openingSymbol, string closingSymbol) = formatter(Type);
        StringBuilder result = new();

        result.Append(openingSymbol);
        foreach (var childToken in EmbeddedTokens)
        {
            result.Append(childToken.ToString(formatter));
        }
        result.Append(closingSymbol);

        return result.ToString();
    }
}
