using System.Globalization;
using System.Text;

namespace Markdown;

public class MdToHtmlFormatter : IFormatProvider, ICustomFormatter
{
    private readonly TokenFactory _tokenFactory = new(new()
        {
            { "_", TokenType.Italics },
            { "__", TokenType.Bold },
            { "# ", TokenType.Header }
        },
        new([TokenType.Header])
    );

    public object GetFormat(Type formatType)
    {
        if (formatType == typeof(ICustomFormatter))
            return this;
        else
            return null;
    }

    public string Format(string fmt, object arg, IFormatProvider formatProvider)
    {
        if (arg.GetType() != typeof(string))
            try
            {
                return HandleOtherFormats(fmt, arg);
            }
            catch (FormatException e)
            {
                throw new FormatException($"The format of '{fmt}' is invalid.", e);
            }

        string text = (string)arg;
        StringBuilder output = new();
        foreach (var line in text.LazySplit(['\r', '\n']))
        {
            //var formattedString = _tokenFactory.Tokenize(line).ToString(GetReplacementSymbol);
            var token = _tokenFactory.Tokenize(line);
            (string openingTag, string closingTag) = GetReplacementSymbol(token.Type);
            var formattedString = token.ToString(GetReplacementSymbol);
            output.Append(formattedString);
        }
        return output.ToString();
    }

    private string HandleOtherFormats(string format, object arg)
    {
        if (arg is IFormattable)
            return ((IFormattable)arg).ToString(format, CultureInfo.CurrentCulture);
        else if (arg != null)
            return arg.ToString();
        else
            return string.Empty;
    }

    private static (string, string) GetReplacementSymbol(TokenType tokenType)
    {
        switch (tokenType)
        {
            case TokenType.Italics:
                return ("<em>", "</em>");
            case TokenType.Bold:
                return ("<strong>", "</strong>");
            case TokenType.Header:
                return ("<h1>", "</h1>");
            default:
                return (string.Empty, string.Empty);
        }
    }
}
