using FluentAssertions;

namespace Markdown.Tests;

[TestFixture]
public class ItalicsAndBoldMarkdownTest
{
    private static Dictionary<string, TokenType> TokensItalicsBold = new()
    {
        {"_", TokenType.Italics},
        {"__", TokenType.Bold}
    };
    private StringFormatter _formatter;

    [SetUp]
    public void SetUp()
    {
        _formatter = new StringFormatter(TokensItalicsBold);
    }

    public static IEnumerable<(string, Dictionary<string, TokenType>, Token)> Source_ShouldTokenizeItalicsAndBoldCorrectly()
    {
        var rawText = "Hello, _V_!";
        yield return (
            rawText,
            TokensItalicsBold,
            new(TokenType.Main,
            [
                new BasicTextToken("Hello, "),
                new(TokenType.Italics, [new BasicTextToken("V")]),
                new BasicTextToken("!")
            ]
            ));

        rawText = "Hello, __Mister _V_!__";
        yield return (
            rawText,
            TokensItalicsBold,
            new(TokenType.Main,
                [
                    new BasicTextToken("Hello, "),
                    new(TokenType.Bold,
                    [
                        new BasicTextToken("Mister "),
                        new Token(TokenType.Italics, [new BasicTextToken("V")]),
                        new BasicTextToken("!")
                    ])
                ]
                ));

        rawText = "Hello, _Mister __V__!_";
        yield return (rawText, TokensItalicsBold,
            new Token(TokenType.Main,
                [
                    new BasicTextToken("Hello, "),
                    new(TokenType.Italics,
                    [
                        new BasicTextToken("Mister "),
                        new BasicTextToken("__V__"),
                        new BasicTextToken("!")
                    ])
                ]
            ));

        yield return ("_a_", TokensItalicsBold, new(TokenType.Main, 
            [
                new(TokenType.Italics, [new BasicTextToken("a")])
            ]));
    }

    [TestCaseSource(nameof(Source_ShouldTokenizeItalicsAndBoldCorrectly))]
    public void ShouldTokenizeItalicsAndBoldCorrectly(
        (string text,
        Dictionary<string, TokenType> tokenTypes,
        Token expected) input)
    {
        var formatter = new StringFormatter(input.tokenTypes);

        var token = formatter.Tokenize(input.text);

        token.Should().BeEquivalentTo(input.expected);
    }

    [Test]
    public void ShouldAccountEscapeSymbols()
    {
        string text = @"Hello, \_Mister __V__!\_";
        var expected = new Token(TokenType.Main,
                [
                    new BasicTextToken("Hello, _Mister"),
                    new(TokenType.Bold, [new BasicTextToken("V")]),
                    new BasicTextToken("!_")
                ]
            );

        var result = _formatter.Tokenize(text);

        result.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void ShouldIgnoreItalicsInNonLetterStrings()
    {
        var text = "_12_3";
        var expected = new Token(TokenType.Main, 
            [
                new BasicTextToken("_"), 
            new Token(TokenType.Basic, [new BasicTextToken("12")]),
            new BasicTextToken("_"),
            new BasicTextToken("3")
            ]);

        var result = _formatter.Tokenize(text);

        result.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void ShouldMarkPartOfTheWord()
    {
        string text = "и в _нач_але, и в сер_еди_не, и в кон_це._";
        Token expected = new(TokenType.Main, [
            new BasicTextToken("и в "),
            new (TokenType.Italics, [new BasicTextToken("нач")]),
            new BasicTextToken("але, и в сер"),
            new (TokenType.Italics, [new BasicTextToken("еди")]),
            new BasicTextToken("не, и в кон"),
            new (TokenType.Italics, [new BasicTextToken("це.")]),
            ]);

        var result = _formatter.Tokenize(text);

        result.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void ShouldNotMarkPartOfDifferentWords()
    {
        string text = "ра_зных сл_овах";
        Token expected = new(TokenType.Main, [new BasicTextToken(text)]);

        var result = _formatter.Tokenize(text);

        result.Should().BeEquivalentTo(expected);
    }

    public static IEnumerable<(string text, Token expected)> Source_ShouldNotMarkIfTokenWrapsWithWhitespace()
    {
        string text;

        text = "эти_ подчерки_ ";
        yield return (text, new(TokenType.Main, [new BasicTextToken(text)]));

        text = "_выдели_ эти_ подчерки_ ";
        yield return (text, new(TokenType.Main, [new BasicTextToken(text)]));

        text = "_подчерки _не считаются_";
        yield return (text, new(TokenType.Main, [new(TokenType.Italics, [new BasicTextToken("подчерки _не считаются")])]));
    }

    [TestCaseSource(nameof(Source_ShouldNotMarkIfTokenWrapsWithWhitespace))]
    public void ShouldNotMarkIfTokenWrapsWithWhitespace((string text, Token expected) input)
    {
        var result = _formatter.Tokenize(input.text);

        result.Should().BeEquivalentTo(input.expected);
    }
}
