using FluentAssertions;
using static System.Net.Mime.MediaTypeNames;

namespace Markdown.Tests;

[TestFixture]
public class MdTests
{
    private MdToHtmlFormatter _formatter;

    [SetUp]
    public void SetUp()
    {
        _formatter = new();
    }

    [TestCase("# Заголовок __с _разными_ символами__", "<h1>Заголовок <strong>с <em>разными</em> символами</strong></h1>")]
    public void ShouldReplaceMdWithHTMLCorrectly(string md, string expected)
    {
        var result = string.Format(_formatter, "{0}", md);

        result.Should().Be(expected);
    }

    [TestCase("_12_3", "_12_3")]
    [TestCase("__12__3", "__12__3")]
    [TestCase("_Banana __12__3_", "<em>Banana __12__3</em>")]
    public void ShouldIgnoreMarkdownInNonLetterStrings(string md, string expected)
    {
        var result = string.Format(_formatter, "{0}", md);

        result.Should().Be(expected);
    }

    [TestCase("__пересечения _двойных__ и одинарных_", "__пересечения _двойных__ и одинарных_")]
    [TestCase("_пересечения __двойных_ и одинарных__", "<em>пересечения __двойных</em> и одинарных__")]
    [TestCase("__Красиво __пересечения _двойных__ и одинарных_ сказал__", "__Красиво __пересечения _двойных__ и одинарных_ сказал__")]
    public void ShouldNotTokenizeIntersectionsOfTags(string text, string expected)
    {
        var result = string.Format(_formatter, "{0}", text);

        result.Should().Be(expected);
    }

    [TestCase("Hello, _V_!", "Hello, <em>V</em>!")]
    [TestCase("Hello, __Mister _V_!__", "Hello, <strong>Mister <em>V</em>!</strong>")]
    [TestCase("Hello, _Mister __V__!_", "Hello, <em>Mister __V__!</em>")]
    [TestCase("_a_", "<em>a</em>")]
    [TestCase("__a__", "<strong>a</strong>")]
    [TestCase("___a___", "<strong><em>a</em></strong>")]
    public void ShouldTokenizeItalicsAndBoldCorrectly(string text, string expected)
    {
        var result = string.Format(_formatter, "{0}", text);

        result.Should().Be(expected);
    }

    [TestCase(@"Hello, \_Mister __V__!\_", @"Hello, _Mister <strong>V</strong>!_")]
    [TestCase(@"Hello, \_\_Mister __V__!\_\_", @"Hello, __Mister <strong>V</strong>!__")]
    [TestCase(@"Hello, \_\_Mister __\\V__!\_\_", @"Hello, __Mister <strong>\V</strong>!__")]
    public void ShouldAccountEscapeSymbols(string md, string expected)
    {
        var result = string.Format(_formatter, "{0}", md);

        result.Should().Be(expected);
    }

    [TestCase("и в _нач_але, и в сер_еди_не, и в кон_це._", "и в <em>нач</em>але, и в сер<em>еди</em>не, и в кон<em>це.</em>")]
    [TestCase("и в __нач__але, и в сер__еди__не, и в кон__це.__", "и в <strong>нач</strong>але, и в сер<strong>еди</strong>не, и в кон<strong>це.</strong>")]
    public void ShouldTokenizePartOfTheWord(string md, string expected)
    {
        var result = string.Format(_formatter, "{0}", md);

        result.Should().Be(expected);
    }

    [TestCase("ра_зных сл_овах")]
    public void ShouldNotTokenizePartsOfDifferentWords(string md)
    {
        var result = string.Format(_formatter, "{0}", md);

        result.Should().Be(md);
    }

    [TestCase("эти_ подчерки_ ", "эти_ подчерки_ ")]
    [TestCase("эти__ подчерки__ ", "эти__ подчерки__ ")]
    [TestCase("_выдели_ эти_ подчерки_ ", "<em>выдели</em> эти_ подчерки_ ")]
    [TestCase("_подчерки _не считаются_", "<em>подчерки _не считаются</em>")]
    public void ShouldNotTokenizeIfTokenWrapsWithWhitespace(string md, string expected)
    {
        var result = string.Format(_formatter, "{0}", md);

        result.Should().Be(expected);
    }

    [TestCase("__")]
    [TestCase("____")]
    [TestCase("______")]
    [TestCase("___")]
    public void ShouldNotTokenizeEmptyStrings(string md)
    {
        var result = string.Format(_formatter, "{0}", md);

        result.Should().Be(md);
    }

    [TestCase("# Заголовок", "<h1>Заголовок</h1>")]
    [TestCase("# Заголовок __с _разными_ символами__", "<h1>Заголовок <strong>с <em>разными</em> символами</strong></h1>")]
    [TestCase("# Заголовок # не заголовок", "<h1>Заголовок # не заголовок</h1>")]
    [TestCase("text # не заголовок", "text # не заголовок")]
    public void ShouldTokenizeUnpairableTagsCorrectly(string md, string expected)
    {
        var result = string.Format(_formatter, "{0}", md);

        result.Should().Be(expected);
    }

    [TestCase("__abcd _efgh")]
    public void ShouldNotTokenizeUnfinishedTags(string md)
    {
        var result = string.Format(_formatter, "{0}", md);

        result.Should().Be(md);
    }

    [TestCase("# sword?\r\n__no__# need\r\n_ok_", "<h1>sword?</h1>\r\n<strong>no</strong># need\r\n<em>ok</em>")]
    public void ShouldTokenizeLinesCorrectly(string md, string expected)
    {
        var result = string.Format(_formatter, "{0}", md);

        result.Should().Be(expected);
    }

    [Test]
    [Ignore("To be ignored until markdown '___' is recognized as 'closing italics' and 'closing bold'")]
    public void PerformanceTest()
    {
        //var md = "# Around __the world__\r\n__around _the world___";
        //var mdLines = string.Concat(Enumerable.Repeat())
    }
}
