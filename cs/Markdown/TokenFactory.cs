namespace Markdown;

public class TokenFactory
{
    private string _text;
    public readonly Dictionary<string, TokenType> TokenTypes;
    private readonly Dictionary<TokenType, string> _typesToTokenStrings;
    public readonly char EscapeSymbol;
    private readonly Stack<Token> _openTokens = new();
    private Token _currentToken = new(TokenType.Main, [], isInWord: false);
    private int _textStartIndex = 0;

    public TokenFactory(
        Dictionary<string, TokenType> tokenTypes,
        char escapeSymbol = '\\')
    {
        EscapeSymbol = escapeSymbol;
        TokenTypes = tokenTypes;
        _typesToTokenStrings = new(tokenTypes.ToDictionary(kvp => kvp.Value, kvp => kvp.Key));
    }

    public Token Tokenize(string text)
    {
        _text = text;
        _textStartIndex = 0;
        return Tokenize();
    }

    private Token Tokenize()
    {
        int symbolIndex = 0;
        for (; symbolIndex < _text.Length; symbolIndex++)
        {
            if (GetTokenType(symbolIndex) is not TokenType tokenType
                || (symbolIndex > 0 && _text[symbolIndex - 1] == EscapeSymbol))
                continue;

            var tokenString = _typesToTokenStrings[tokenType];
            if (ProcessToken(tokenType, symbolIndex))
                symbolIndex += tokenString.Length - 1;
        }
        AddPreviousTextToTokenIfAnyText(symbolIndex);
        FinalizeOpenTokens();

        return _currentToken;
    }

    private void FinalizeOpenTokens()
    {
        while (_openTokens.Count > 0)
        {
            Token tokenParent = _openTokens.Pop();
            if (_currentToken.Type == TokenType.Header)
                CloseHeaderToken(tokenParent);
            else RemoveTokenMarkdown(
                    _currentToken,
                    tokenParent: tokenParent,
                    isTokenComplete: false);
            _currentToken = tokenParent;
        }
    }

    private void CloseHeaderToken(Token tokenParent)
    {
        tokenParent.EmbeddedTokens.Add(_currentToken);
    }

    private bool ProcessToken(TokenType tokenType, int tokenIndex)
    {
        // TODO Перенести в CheckIfTokenIsInvalid
        bool hasNoBoldWithinItalics = !(tokenType == TokenType.Bold && _currentToken.Type == TokenType.Italics);

        // Если токен не прошел одно из правил токенизации, оставляем его в виде обычного текста,
        // для чего нужно вернуть из метода true.
        bool processResult = true;
        if (_currentToken.Type == tokenType
            && hasNoBoldWithinItalics)
        {
            processResult = CloseCurrentToken(tokenIndex);
        }
        else if (_currentToken.Type != tokenType
            && hasNoBoldWithinItalics)
        {
            processResult = OpenNewToken(tokenType, tokenIndex);
        }
        _textStartIndex = tokenIndex + _typesToTokenStrings[tokenType].Length;

        return processResult;
    }

    private bool OpenNewToken(TokenType tokenType, int tokenIndex)
    {
        var tokenSymbolLength = _typesToTokenStrings[tokenType].Length;
        if (tokenType != TokenType.Header
            && tokenIndex + tokenSymbolLength < _text.Length
            && char.IsSeparator(_text[tokenIndex + tokenSymbolLength]))
            return false;
        if (tokenType == TokenType.Header
            && _currentToken.Type != TokenType.Main)
            return false;

        AddPreviousTextToTokenIfAnyText(tokenIndex);
        _openTokens.Push(_currentToken);
        _currentToken = new(tokenType, [], isInWord: CheckIfTokenIsInWord(tokenIndex, tokenType));

        return true;
    }

    private void AddPreviousTextToTokenIfAnyText(int tokenIndex)
    {
        if (tokenIndex <= _textStartIndex) return;
        string tokenText = _text[_textStartIndex..tokenIndex];
        _currentToken.EmbedText(tokenText.RemoveEscapeSymbols(EscapeSymbol));
    }

    private bool CloseCurrentToken(int tokenIndex)
    {
        if (tokenIndex - 1 > 0 && char.IsSeparator(_text[tokenIndex - 1]))
            return false;

        AddPreviousTextToTokenIfAnyText(tokenIndex);

        Token previouslyOpenedToken = _openTokens.Pop();
        if (CheckIfTokenIsValid(tokenIndex, _currentToken))
        {
            previouslyOpenedToken.EmbeddedTokens.Add(_currentToken);
        }
        else
        {
            var textDidNotEndAndTokenIsComplete = tokenIndex < _text.Length;
            RemoveTokenMarkdown(_currentToken, previouslyOpenedToken, textDidNotEndAndTokenIsComplete);
            //RemoveTokenMarkdown(_currentToken, previouslyOpenedToken, true);
        }
        _currentToken = previouslyOpenedToken;

        return true;
    }

    private bool CheckIfTokenIsInWord(int tokenIndex, TokenType tokenType)
    {
        var tokenSymbol = _typesToTokenStrings[tokenType];
        return tokenIndex - 1 > 0
            && !char.IsSeparator(_text[tokenIndex - 1])
            && tokenIndex + tokenSymbol.Length < _text.Length
            && !char.IsSeparator(_text[tokenIndex + tokenSymbol.Length]);
    }

    private void RemoveTokenMarkdown(Token token, Token tokenParent, bool isTokenComplete)
    {
        if (token.Type == TokenType.Basic) return;
        var tokenSymbol = _typesToTokenStrings[token.Type];
        token.Type = TokenType.Basic;
        tokenParent.EmbedText(tokenSymbol);
        tokenParent.EmbeddedTokens.Add(token);

        if (!isTokenComplete) return;

        tokenParent.EmbedText(tokenSymbol);
    }

    private bool CheckIfTokenIsValid(int tokenIndex, Token token)
    {
        if (tokenIndex <= _textStartIndex) return false;
        var tokenText = _text[_textStartIndex..tokenIndex];
        return !tokenText.All(char.IsDigit) && (!token.IsInWord || !tokenText.Any(char.IsSeparator));
    }

    private TokenType? GetTokenType(int symbolIndex)
    {
        var additionalTokenLength = 0;
        string? savedKey = null;
        while (symbolIndex + additionalTokenLength < _text.Length)
        {
            var symbol = _text.Substring(symbolIndex, 1 + additionalTokenLength);
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