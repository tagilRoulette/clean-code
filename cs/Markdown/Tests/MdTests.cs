using FluentAssertions;

namespace Markdown.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    public static IEnumerable<(string, Dictionary<string, TokenType>, List<Token>)> Source_ShouldTokenizeItalicsCorrectly()
    {
        yield return ("Hello, _V_", new() { { "_", TokenType.Italics } }, [new(7, 1, TokenType.Italics)]);
    }

    public static IEnumerable<(string, Dictionary<string, TokenType>, List<Token>)> Source_ShouldTokenizeItalicsAndBoldCorrectly()
    {
        yield return (
            "Hello, __Mister _V_!__",
            new()
            {
                { "_", TokenType.Italics },
                { "__", TokenType.Bold },
            },
            [
                new(16, 1, TokenType.Italics),
                new(8, 10, TokenType.Bold)
            ]);
    }

    [TestCaseSource(nameof(Source_ShouldTokenizeItalicsCorrectly))]
    [TestCaseSource(nameof(Source_ShouldTokenizeItalicsAndBoldCorrectly))]
    public void ShouldTokenizeItalicsCorrectly(
        (string text,
        Dictionary<string, TokenType> tokenTypes,
        List<Token> expected) input)
    {
        var formatter = new StringFormatter(
            input.text,
            input.tokenTypes);

        formatter.Tokenize().Should().BeEquivalentTo(input.expected);
    }

    //[Test]
    //public void ShouldTokenizeItalicsAndBoldCorrectly()
    //{
    //    var formatter = new StringFormatter(
    //        input.text,
    //        input.tokenTypes);
    //}

}
