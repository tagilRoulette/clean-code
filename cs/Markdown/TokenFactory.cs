using System.Collections.Generic;
using System.Data.SqlTypes;
using Newtonsoft.Json.Linq;

namespace Markdown;

public class TokenFactory
{
    public readonly Dictionary<string, TokenType> TokenTypes;
    private readonly Dictionary<TokenType, string> _typesToTokenStrings;
    public readonly char EscapeSymbol;
    private readonly Stack<Token> _openTokens = new();
    private readonly HashSet<TokenType> _openTokenTypes = new();
    private Token _currentToken;
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
        unpairableTokens.Add(TokenType.Main);
        _unpairableTokens = unpairableTokens;
    }

    public Token Tokenize(string text)
    {
        _openTokenTypes.Clear();
        _text = text;
        _textStartIndex = 0;
        _currentToken = new(TokenType.Main, [], isInWord: false);
        return Tokenize();
    }

    private Token Tokenize()
    {
        int symbolIndex = 0;
        for (; symbolIndex < _text.Length; symbolIndex++)
        {
            TokenType tokenType = GetTokenType(symbolIndex);
            if (tokenType == TokenType.None
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
        _openTokenTypes.Add(_currentToken.Type);
        _currentToken = new(tokenType, [], isInWord: CheckIfTokenIsInWord(tokenIndex, tokenType));
    }

    private bool ValidateOpeningToken(TokenType tokenType, int tokenIndex)
    {
        var tokenSymbolLength = _typesToTokenStrings[tokenType].Length;
        bool isPairableTokenContinuedBySeparator = !_unpairableTokens.Contains(tokenType)
            && tokenIndex + tokenSymbolLength < _text.Length
            && char.IsSeparator(_text[tokenIndex + tokenSymbolLength]);
        if (isPairableTokenContinuedBySeparator)
            return false;

        bool isUnpairableTokenNotAtLineStart = _unpairableTokens.Contains(tokenType)
            && tokenIndex != 0;
        if (isUnpairableTokenNotAtLineStart)
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
        _openTokenTypes.Remove(previouslyOpenedToken.Type);

        var tokenText = _text[_textStartIndex..tokenIndex];
        bool hasUntokenizedNonNumericSymbols = tokenText.Length > 0
            && !tokenText.All(char.IsDigit);
        if ((hasUntokenizedNonNumericSymbols || _currentToken.HasTokenizedNonNumericSymbols)
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
        var didRemove = false;
        var typeToRemove = _openTokens.Peek().Type;
        if (_openTokens.Count > 1)
            _openTokenTypes.Remove(_openTokens.Peek().Type);
        if (tokenIndex + 1 < _text.Length
            && GetTokenType(tokenIndex + 1) == _currentToken.Type)
        {
            RemoveTokenMarkdown(_currentToken, _openTokens.Peek(), isTokenComplete: false);
            return false;
        }
        if (didRemove)
            _openTokenTypes.Add(typeToRemove);
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

    private TokenType GetTokenType(int symbolIndex)
    {
        List<TokenType> candidates = GetTokenCandidates(symbolIndex);
        if (candidates.Count == 0)
            return TokenType.None;
        var candidatesOrdered = candidates.OrderBy(type => _typesToTokenStrings[type].Length).ToList();
        // if in Main, Header or any other unpairable token
        if (_unpairableTokens.Contains(_currentToken.Type))
            return candidatesOrdered[^1];
        for (int candidateReverseIndex = 1; candidateReverseIndex <= candidatesOrdered.Count; candidateReverseIndex++)
        {
            if (!_openTokenTypes.Contains(candidatesOrdered[^candidateReverseIndex]))
                return candidatesOrdered[^candidateReverseIndex];
        }
        return candidatesOrdered[0];
    }

    private List<TokenType> GetTokenCandidates(int symbolIndex)
    {
        List<TokenType> result = new();
        for (int length = 0; symbolIndex + length < _text.Length; length++)
        {
            var symbol = _text.Substring(symbolIndex, 1 + length);

            if (TokenTypes.TryGetValue(symbol, out TokenType type))
                result.Add(type);

            if (!TokenTypes.Keys.Any(key => key.StartsWith(symbol)))
                break;
        }
        return result;
    }
}