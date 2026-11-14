using FluentAssertions;
using FluentAssertions.Equivalency;

namespace Markdown.Tests;

[TestFixture]
public class ItalicsAndBoldMarkdownTest
{
    private static Dictionary<string, TokenType> TokensItalicsBold = new()
    {
        {"_", TokenType.Italics},
        {"__", TokenType.Bold}
    };
    private Func<EquivalencyOptions<Token>, EquivalencyOptions<Token>> _config =
        config => config.Excluding(token =>
            token.DeclaringType == typeof(Token)
                && token.Name == nameof(Token.IsInWord));

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
                        new BasicTextToken("Mister __V__!"),
                    ])
                ]
            ));

        yield return ("_a_", TokensItalicsBold, new(TokenType.Main,
            [
                new(TokenType.Italics, [new BasicTextToken("a")])
            ]));

        yield return ("__a__", TokensItalicsBold, new(TokenType.Main,
            [
                new(TokenType.Bold, [new BasicTextToken("a")])
            ]));
    }

    [TestCaseSource(nameof(Source_ShouldTokenizeItalicsAndBoldCorrectly))]
    public void ShouldTokenizeItalicsAndBoldCorrectly(
        (string text,
        Dictionary<string, TokenType> tokenTypes,
        Token expected) input)
    {
        var formatter = new StringFormatter(input.text, input.tokenTypes);

        var token = formatter.Tokenize();

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

        StringFormatter stringFormatter = new(text, TokensItalicsBold);

        var result = stringFormatter.Tokenize();

        result.Should().BeEquivalentTo(expected, _config);
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
        StringFormatter stringFormatter = new(text, TokensItalicsBold);

        var result = stringFormatter.Tokenize();

        result.Should().BeEquivalentTo(expected, _config);
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
        StringFormatter stringFormatter = new(text, TokensItalicsBold);

        var result = stringFormatter.Tokenize();

        result.Should().BeEquivalentTo(expected, _config);
    }

    [Test]
    public void ShouldNotMarkPartOfDifferentWords()
    {
        string text = "ра_зных сл_овах";
        Token expected = new(TokenType.Main, [new BasicTextToken("ра"), new BasicTextToken("_"), new Token(TokenType.Basic, [new BasicTextToken("зных сл")]), new BasicTextToken("_"), new BasicTextToken("овах")]);
        StringFormatter stringFormatter = new(text, TokensItalicsBold);

        var result = stringFormatter.Tokenize();

        result.Should().BeEquivalentTo(expected, _config);
    }

    public static IEnumerable<(string text, Token expected)> Source_ShouldNotMarkIfTokenWrapsWithWhitespace()
    {
        string text;

        text = "эти_ подчерки_ ";
        yield return (text, new(TokenType.Main, [new BasicTextToken(text)]));

        text = "_выдели_ эти_ подчерки_ ";
        yield return (text, new(TokenType.Main,
            [
                new Token(TokenType.Italics, [new BasicTextToken("выдели")]),
                new BasicTextToken(text)
            ]));

        text = "_подчерки _не считаются_";
        yield return (text, new(TokenType.Main, [new(TokenType.Italics, [new BasicTextToken("подчерки _не считаются")])]));
    }

    [TestCaseSource(nameof(Source_ShouldNotMarkIfTokenWrapsWithWhitespace))]
    public void ShouldNotMarkIfTokenWrapsWithWhitespace((string text, Token expected) input)
    {
        StringFormatter stringFormatter = new(input.text, TokensItalicsBold);

        var result = stringFormatter.Tokenize();

        result.Should().BeEquivalentTo(input.expected, _config);
    }

    public static IEnumerable<(string text, Token expected)> Source_ShouldNotTokenizeEmptyStrings()
    {
        yield return ("__", new(TokenType.Main, [new BasicTextToken("_"), new(TokenType.Basic, []), new BasicTextToken("_")]));
        yield return ("____", new(TokenType.Main, [new BasicTextToken("__"), new(TokenType.Basic, []), new BasicTextToken("__")]));
    }

    [TestCaseSource(nameof(Source_ShouldNotTokenizeEmptyStrings))]
    public void ShouldNotTokenizeEmptyStrings((string text, Token expected) input)
    {
        StringFormatter stringFormatter = new(input.text, TokensItalicsBold);

        var result = stringFormatter.Tokenize();

        result.Should().BeEquivalentTo(input.expected);
    }

    public static IEnumerable<(string text, Token expected)> Source_ShouldTokenizeHeaderTagsCorrectly()
    {
        yield return ("# Заголовок", 
            new(TokenType.Main, [new(TokenType.Header, [new BasicTextToken("Заголовок")])]));
        yield return ("# Заголовок __с _разными_ символами__", 
            new(TokenType.Main, [new(TokenType.Header, [
                new BasicTextToken("Заголовок ")]),
                new Token(TokenType.Bold, 
                    [new BasicTextToken("с "), new(TokenType.Italics, [new BasicTextToken("разными")]), new BasicTextToken(" символами")]
                )
            ]));
    }

    [TestCaseSource(nameof(Source_ShouldTokenizeHeaderTagsCorrectly))]
    public void ShouldTokenizeHeaderTagsCorrectly((string text, Token expected) input)
    {

    }
}
