namespace Markdown;

public class StringFormatter
{
    //public readonly Dictionary<string, string> PairReplacements;
    //public readonly Dictionary<string, string> SingleReplacements;
    public readonly Dictionary<string, TokenType> TokenTypes;
    private readonly Dictionary<TokenType, string> _typesToTokenStrings;
    public readonly char EscapeSymbol;

    public StringFormatter(
        //Dictionary<string, string> pairReplacements,
        //Dictionary<string, string> singleReplacements,
        string text,
        Dictionary<string, TokenType> tokenTypes,
        char escapeSymbol = '\\')
    {
        //PairReplacements = pairReplacements;
        //SingleReplacements = singleReplacements;
        EscapeSymbol = escapeSymbol;
        TokenTypes = tokenTypes;
        _typesToTokenStrings = new(tokenTypes.ToDictionary(kvp => kvp.Value, kvp => kvp.Key));
    }

    public Token Tokenize(string text)
    {
        Token currentToken = new Token(text, []);
        Stack<TokenType> openTokens = new();
        Stack<int> openTokenIndexes = new();

        for (int symbolIndex = 0; symbolIndex < text.Length; symbolIndex++)
        {
            if (GetTokenType(symbolIndex, text) is not TokenType tokenType) continue;

            //if (openTokens.Count == 0)
            //{
            //    currentToken.EmbeddedTokens.Add(new BasicTextToken(text));
            //}

            if (openTokens.TryPeek(out var topTokenType)
                && topTokenType == tokenType)
            {
                openTokens.Pop();
                currentToken.EmbeddedTokens.Add(new(openTokenIndexes.Pop(), tokenType));
            }
            else
            {
                openTokens.Push(tokenType);
                openTokenIndexes.Push(symbolIndex);
            }
            // чтобы не перескочить один символ, а перейти на него уже в for (... symbolIndex++)
            symbolIndex += tokenStringLength - 1;
        }
        return currentToken;
    }

    private TokenType? GetTokenType(int symbolIndex, string text)
    {
        var additionalTokenLength = 0;
        string? savedKey = null;
        while (symbolIndex + additionalTokenLength < text.Length)
        {
            var symbol = text.Substring(symbolIndex, 1 + additionalTokenLength);
            bool keySubstrIsInAnyReplacementDict =
                TokenTypes.Keys.Any(key => key.StartsWith(symbol));

            if (keySubstrIsInAnyReplacementDict)
            {
                savedKey = symbol;
                additionalTokenLength++;
            }
            else break;
        }
        if (savedKey == null)
            return null;
        return TokenTypes[savedKey];
    }

    internal int FindPairSymbolIndex(int fromIndex, int symbolLength)
    {
        throw new NotImplementedException();
    }

    // Будет определять, что нужно использовать: ReplaceSingleSymbol или ReplacePairSymbols
    internal void ReplaceSymbols(int fromIndex, int toIndex, int symbolLength)
    {
        throw new NotImplementedException();

    }

    internal void ReplaceSingleSymbol(int fromIndex, int toIndex, int symbolLength)
    {
        throw new NotImplementedException();
        //string toReplace = Text.ToString(fromIndex, symbolLength);
        //SingleReplacements.TryGetValue(toReplace, out var replacement);
        //if (replacement == null)
        //    throw new KeyNotFoundException("Symbol does not have a replacement.");

        //Text.Remove(fromIndex, symbolLength);
        //Text.Insert(fromIndex, replacement);

        //string closingTag = replacement.Insert(1, "/");
        //Text.Insert(toIndex, closingTag);
    }

    internal void ReplacePairSymbols(int fromIndex, int toIndex, int symbolLength)
    {
        throw new NotImplementedException();

    }
}