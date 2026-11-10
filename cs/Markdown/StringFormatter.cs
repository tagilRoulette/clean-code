namespace Markdown;

public class StringFormatter
{
    //public readonly Dictionary<string, string> PairReplacements;
    //public readonly Dictionary<string, string> SingleReplacements;
    public readonly Dictionary<string, TokenType> TokenTypes;
    private readonly Dictionary<TokenType, string> _typesToTokenStrings;
    public readonly char EscapeSymbol;
    private readonly Stack<Token> _openTokens = new();

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

    public Token Tokenize(string text)
    {
        Token currentToken = new(TokenType.Main, []);
        int basicTextStartIndex = 0, symbolIndex = 0;

        for (; symbolIndex < text.Length; symbolIndex++)
        {
            if (GetTokenType(symbolIndex, text) is not TokenType tokenType
                || (symbolIndex > 0 && text[symbolIndex - 1] == EscapeSymbol))
                continue;

            if (symbolIndex != basicTextStartIndex)
            {
                string tokenText = RemoveEscapeSymbols(text[basicTextStartIndex..symbolIndex]);
                currentToken.EmbedText(tokenText);
            }

            var tokenString = _typesToTokenStrings[tokenType];
            bool hasNoBoldWithinItalics = !(tokenType == TokenType.Bold && currentToken.Type == TokenType.Italics);
            bool tokenizationRules = hasNoBoldWithinItalics;
            // TODO: Переписать с ProcessToken
            if (currentToken.Type == tokenType // Закрываем
                && tokenizationRules)
            {
                if (char.IsSeparator(text[symbolIndex - 1]))
                    continue;
                var currentTokenText = text[basicTextStartIndex..symbolIndex];
                currentToken = CloseTokenAndGetParent(currentTokenText, currentToken);
            }
            else if (currentToken.Type != tokenType // Открываем
                && tokenizationRules)
            {
                if (symbolIndex + 1 < text.Length && char.IsSeparator(text[symbolIndex + 1]))
                {
                    currentToken.EmbeddedTokens.RemoveAt(currentToken.EmbeddedTokens.Count - 1); // когда может сломать все?
                    continue;
                }

                _openTokens.Push(currentToken);
                currentToken = new(tokenType, []);
            }

            basicTextStartIndex = symbolIndex + tokenString.Length;
            symbolIndex += tokenString.Length - 1;
        }
        if (_openTokens.Count == 0 && symbolIndex != basicTextStartIndex)
        {
            string tokenText = RemoveEscapeSymbols(text[basicTextStartIndex..symbolIndex]);
            currentToken.EmbedText(tokenText);
        }

        return currentToken;
    }

    private Token CloseTokenAndGetParent(string tokenText, Token token)
    {
        var tokenSymbol = _typesToTokenStrings[token.Type];
        Token previouslyOpenedToken = _openTokens.Pop();
        // Если нет ни одной буквы, то делаем токен обычным
        if (tokenText.All(char.IsDigit))
        {
            token.Type = TokenType.Basic;
            previouslyOpenedToken.EmbedText(tokenSymbol);
            previouslyOpenedToken.EmbeddedTokens.Add(token);
            previouslyOpenedToken.EmbedText(tokenSymbol);
        }
        else
            previouslyOpenedToken.EmbeddedTokens.Add(token);

        return previouslyOpenedToken;
    }

    private string RemoveEscapeSymbols(string text)
    {
        const char placeholder = '\u0001'; // SOH в нормальном тексте не встречается
        return text
            .Replace(new string(EscapeSymbol, 2), placeholder.ToString())
            .Replace(EscapeSymbol.ToString(), string.Empty)
            .Replace(placeholder.ToString(), EscapeSymbol.ToString());
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