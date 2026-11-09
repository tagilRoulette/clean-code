using System.Diagnostics.Metrics;
using FluentAssertions;

namespace Markdown.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    public static IEnumerable<(string, Dictionary<string, TokenType>, Token)> Source_ShouldTokenizeItalicsAndBoldCorrectly()
    {
        var rawText = "Hello, _V_!";
        yield return (
            rawText, 
            new()
            {
                { "_", TokenType.Italics } 
            },
            new(rawText, 
            [
                new("Hello, ", [], TokenType.Basic),
                new("V", [], TokenType.Italics),
                new("!", [], TokenType.Basic)
            ], 
            TokenType.Basic
            ));
        
        rawText = "Hello, __Mister _V_!__";
        yield return (
            rawText,
            new()
            {
                { "_", TokenType.Italics },
                { "__", TokenType.Bold },
            },
            new
            (
                rawText,
                [
                    new("Mister _V_!", [new("V", [], TokenType.Italics)], TokenType.Bold)
                ],
                TokenType.Basic
            ));
        
        rawText = "Hello, _Mister __V__!_";
        yield return (
            rawText,
            new()
            {
                { "_", TokenType.Italics },
                { "__", TokenType.Bold },
            },
            new
            (
                rawText,
                [new("Mister __V__!", [new("__V__", [], TokenType.Basic)], TokenType.Italics)],
                TokenType.Basic
            ));
    }


    [TestCaseSource(nameof(Source_ShouldTokenizeItalicsAndBoldCorrectly))]
    //[TestCaseSource(nameof(Source_ShouldTokenizeItalicsAndBoldCorrectly))]
    public void ShouldTokenizeItalicsCorrectly(
        (string text,
        Dictionary<string, TokenType> tokenTypes,
        Token expected) input)
    {
        var formatter = new StringFormatter(
            input.tokenTypes);

        var token = formatter.Tokenize(new(input.text, [], TokenType.Basic));
        token.Should().BeEquivalentTo(input.expected);
    }

    //[Test]t
    //public void ShouldTokenizeItalicsAndBoldCorrectly()
    //{
    //    var formatter = new StringFormatter(
    //        input.text,
    //        input.tokenTypes);
    //}

}
