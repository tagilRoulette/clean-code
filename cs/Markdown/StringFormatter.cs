namespace Markdown;

public class StringFormatter
{
    public readonly Dictionary<string, TokenType> TokenTypes;
    private readonly Dictionary<TokenType, string> _typesToTokenStrings;
    public readonly char EscapeSymbol;
    public readonly string Text;
    private readonly Stack<Token> _openTokens = new();
    private Token _currentToken = new(TokenType.Main, [], isInWord: false);
    private int _textStartIndex = 0;

    public StringFormatter(
        string text,
        Dictionary<string, TokenType> tokenTypes,
        char escapeSymbol = '\\')
    {
        EscapeSymbol = escapeSymbol;
        TokenTypes = tokenTypes;
        _typesToTokenStrings = new(tokenTypes.ToDictionary(kvp => kvp.Value, kvp => kvp.Key));
        Text = text;
    }

    public Token Tokenize()
    {
        int symbolIndex = 0;
        for (; symbolIndex < Text.Length; symbolIndex++)
        {
            if (GetTokenType(symbolIndex, Text) is not TokenType tokenType
                || (symbolIndex > 0 && Text[symbolIndex - 1] == EscapeSymbol))
                continue;

            var tokenString = _typesToTokenStrings[tokenType];
            if (ProcessToken(tokenType, symbolIndex))
                symbolIndex += tokenString.Length - 1;
        }
        if (_openTokens.Count == 0 && symbolIndex != _textStartIndex)
        {
            string tokenText = RemoveEscapeSymbols(Text[_textStartIndex..]);
            _currentToken.EmbedText(tokenText);
        }

        return _currentToken;
    }

    private bool ProcessToken(TokenType tokenType, int tokenIndex)
    {
        // TODO Перенести в CheckIfTokenIsInvalid
        bool hasNoBoldWithinItalics = !(tokenType == TokenType.Bold && _currentToken.Type == TokenType.Italics);
        bool tokenizationRules = hasNoBoldWithinItalics;
        var tokenString = _typesToTokenStrings[tokenType];

        // Если токен не прошел одно из правил токенизации, оставляем его в виде обычного текста,
        // для чего нужно вернуть из метода true.
        bool processResult = true;
        if (_currentToken.Type == tokenType
            && tokenizationRules)
        {
            processResult = CloseCurrentToken(tokenIndex);
        }
        else if (_currentToken.Type != tokenType
            && tokenizationRules)
        {
            processResult = OpenNewToken(tokenType, tokenIndex, tokenString);
        }
        _textStartIndex = tokenIndex + tokenString.Length;

        return processResult;
    }

    private bool OpenNewToken(TokenType tokenType, int tokenIndex, string tokenString)
    {
        if (tokenIndex + 1 < Text.Length && char.IsSeparator(Text[tokenIndex + 1]))
            return false;
        AddPreviousTextToTokenIfTokenIndexDiffersFromTextStartIndex(tokenIndex);
        _openTokens.Push(_currentToken);
        _currentToken = new(tokenType, [], isInWord: CheckIfTokenIsInWord(tokenIndex, tokenType));

        return true;
    }

    // TODO укоротить название
    private void AddPreviousTextToTokenIfTokenIndexDiffersFromTextStartIndex(int tokenIndex)
    {
        if (tokenIndex != _textStartIndex)
        {
            string tokenText = RemoveEscapeSymbols(Text[_textStartIndex..tokenIndex]);
            _currentToken.EmbedText(tokenText);
        }
        //_textStartIndex = tokenIndex;
    }

    private bool CloseCurrentToken(int tokenIndex)
    {
        if (char.IsSeparator(Text[tokenIndex - 1]))
            return false;

        AddPreviousTextToTokenIfTokenIndexDiffersFromTextStartIndex(tokenIndex);

        Token previouslyOpenedToken = _openTokens.Pop();
        if (CheckIfTokenIsValid(tokenIndex, _currentToken))
        {
            previouslyOpenedToken.EmbeddedTokens.Add(_currentToken);
        }
        else
        {
            RemoveTokenMarkdown(_currentToken, previouslyOpenedToken);
        }
        _currentToken = previouslyOpenedToken;

        return true;
    }

    private bool CheckIfTokenIsInWord(int tokenIndex, TokenType tokenType)
    {
        var tokenSymbol = _typesToTokenStrings[tokenType];
        return tokenIndex - 1 > 0
            && !char.IsSeparator(Text[tokenIndex - 1])
            && tokenIndex + tokenSymbol.Length < Text.Length
            && !char.IsSeparator(Text[tokenIndex + tokenSymbol.Length]);
    }

    private void RemoveTokenMarkdown(Token token, Token tokenParent)
    {
        var tokenSymbol = _typesToTokenStrings[token.Type];
        token.Type = TokenType.Basic;
        tokenParent.EmbedText(tokenSymbol);
        tokenParent.EmbeddedTokens.Add(token);
        tokenParent.EmbedText(tokenSymbol);
    }

    private bool CheckIfTokenIsValid(int tokenIndex, Token token)
    {
        var tokenText = Text[_textStartIndex..tokenIndex];
        return !tokenText.All(char.IsDigit) && (!token.IsInWord || !tokenText.Any(char.IsSeparator));
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