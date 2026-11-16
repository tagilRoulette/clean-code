using Newtonsoft.Json.Linq;

namespace Markdown;

public class TokenFactory
{
    public readonly Dictionary<string, TokenType> TokenTypes;
    private readonly Dictionary<TokenType, string> _typesToTokenStrings;
    public readonly char EscapeSymbol;
    private readonly Stack<Token> _openTokens = new();
    private Token _currentToken = new(TokenType.Main, [], isInWord: false);
    private int _textStartIndex = 0;
    private string _text;
    private readonly HashSet<TokenType> _unpairableTokens;

    public TokenFactory(
        Dictionary<string, TokenType> tokenTypes,
        HashSet<TokenType> unpairableTokens,
        char escapeSymbol = '\\')
    {
        EscapeSymbol = escapeSymbol;
        TokenTypes = tokenTypes;
        _typesToTokenStrings = new(tokenTypes.ToDictionary(kvp => kvp.Value, kvp => kvp.Key));
        _unpairableTokens = unpairableTokens;
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
            ProcessToken(tokenType, symbolIndex);
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
            if (_unpairableTokens.Contains(_currentToken.Type))
                CloseUnpairableToken(tokenParent);
            else RemoveTokenMarkdown(
                    _currentToken,
                    tokenParent: tokenParent,
                    isTokenComplete: false);
            _currentToken = tokenParent;
        }
    }

    private void CloseUnpairableToken(Token tokenParent)
    {
        tokenParent.EmbeddedTokens.Add(_currentToken);
    }

    private bool ProcessToken(TokenType tokenType, int tokenIndex)
    {
        if (_currentToken.Type == tokenType && ValidateClosingToken(tokenIndex))
        {
            CloseCurrentToken(tokenIndex);
        }
        else if (_currentToken.Type != tokenType && ValidateOpeningToken(tokenType, tokenIndex))
        {
            OpenNewToken(tokenType, tokenIndex);
        }
        else return false;
        _textStartIndex = tokenIndex + _typesToTokenStrings[tokenType].Length;

        return true;
    }

    private void OpenNewToken(TokenType tokenType, int tokenIndex)
    {
        AddPreviousTextToTokenIfAnyText(tokenIndex);
        _openTokens.Push(_currentToken);
        _currentToken = new(tokenType, [], isInWord: CheckIfTokenIsInWord(tokenIndex, tokenType));
    }

    private bool ValidateOpeningToken(TokenType tokenType, int tokenIndex)
    {
        // todo
        if (_openTokens.Count > 0
            && _openTokens.Peek().Type == tokenType)
        {
            RemoveTokenMarkdown(_currentToken, _openTokens.Peek(), isTokenComplete: false);
            return false;
        }
        var tokenSymbolLength = _typesToTokenStrings[tokenType].Length;
        bool isPairableTokenContinuedBySeparator = !_unpairableTokens.Contains(tokenType)
            && tokenIndex + tokenSymbolLength < _text.Length
            && char.IsSeparator(_text[tokenIndex + tokenSymbolLength]);
        if (isPairableTokenContinuedBySeparator)
            return false;

        bool isUnpairableTokenInsideAnotherToken = _unpairableTokens.Contains(tokenType)
            && _currentToken.Type != TokenType.Main;
        if (isUnpairableTokenInsideAnotherToken)
            return false;

        // no bold tokens inside italics tokens
        if (_currentToken.Type == TokenType.Italics && tokenType == TokenType.Bold)
            return false;

        return true;
    }

    private void AddPreviousTextToTokenIfAnyText(int tokenIndex)
    {
        if (tokenIndex <= _textStartIndex) return;
        string tokenText = _text[_textStartIndex..tokenIndex];
        _currentToken.EmbedText(tokenText.RemoveEscapeSymbols(EscapeSymbol));
    }

    private void CloseCurrentToken(int tokenIndex)
    {
        AddPreviousTextToTokenIfAnyText(tokenIndex);

        Token previouslyOpenedToken = _openTokens.Pop();

        var tokenText = _text[_textStartIndex..tokenIndex];
        if (tokenIndex > _textStartIndex
            && !tokenText.All(char.IsDigit)
            && (!_currentToken.IsInWord || !tokenText.Any(char.IsSeparator)))
        {
            previouslyOpenedToken.EmbeddedTokens.Add(_currentToken);
        }
        else
        {
            var textDidNotEndAndTokenIsComplete = tokenIndex < _text.Length;
            RemoveTokenMarkdown(_currentToken, previouslyOpenedToken, textDidNotEndAndTokenIsComplete);
        }
        _currentToken = previouslyOpenedToken;
    }

    private bool ValidateClosingToken(int tokenIndex)
    {
        return tokenIndex - 1 <= 0 || !char.IsSeparator(_text[tokenIndex - 1]);
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