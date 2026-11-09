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
        Dictionary<string, TokenType> tokenTypes,
        char escapeSymbol = '\\')
    {
        //PairReplacements = pairReplacements;
        //SingleReplacements = singleReplacements;
        EscapeSymbol = escapeSymbol;
        TokenTypes = tokenTypes;
        _typesToTokenStrings = new(tokenTypes.ToDictionary(kvp => kvp.Value, kvp => kvp.Key));
    }

    public Token Tokenize(Token currentToken)
    {
        var text = currentToken.RawText;
        bool isOutOfTokenContext = true;
        //Stack<Token> openTokens = new();
        //Stack<int> openTokenIndexes = new();

        TokenType pairToken = default;
        int pairIndex = default;

        int basicTextStartIndex = 0, basicTextLength = 0;

        for (int symbolIndex = 0; symbolIndex < text.Length; symbolIndex++)
        {
            if (GetTokenType(symbolIndex, text) is not TokenType tokenType)
            {
                if (isOutOfTokenContext) basicTextLength++;
                else if (char.IsSeparator(text[symbolIndex])) isOutOfTokenContext = true;

                continue;
            }
            
            if (basicTextLength > 0)
            {
                Token basicTextToken =
                    new(currentToken.RawText.Substring(basicTextStartIndex, basicTextLength), [], TokenType.Basic);
                currentToken.EmbeddedTokens.Add(basicTextToken);
                basicTextLength = 0;
            }

            bool hasNoBoldWithinItalics = !(tokenType == TokenType.Bold && currentToken.TokenType == TokenType.Italics);
            int tokenStringLength = _typesToTokenStrings[tokenType].Length;
            if (!isOutOfTokenContext 
                && pairToken == tokenType
                && hasNoBoldWithinItalics)
            {
                Token newToken = new(text[(pairIndex + tokenStringLength)..symbolIndex], [], tokenType);
                currentToken.EmbeddedTokens.Add(Tokenize(newToken));
                isOutOfTokenContext = true;
                basicTextStartIndex = symbolIndex + tokenStringLength;
            }
            else if (isOutOfTokenContext)
            {
                pairToken = tokenType;
                pairIndex = symbolIndex;
                isOutOfTokenContext = false;
            }
            symbolIndex += tokenStringLength - 1;
        }
        if (isOutOfTokenContext && basicTextLength > 0)// && pairToken != default)
            currentToken.EmbeddedTokens.Add(
                new(currentToken.RawText.Substring(basicTextStartIndex, basicTextLength), [], TokenType.Basic));
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