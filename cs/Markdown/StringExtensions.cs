namespace Markdown;
public static class StringExtensions
{
    public static string RemoveEscapeSymbols(this string text, char escapeSymbol)
    {
        const char placeholder = '\u0001'; // SOH в нормальном тексте не встречается
        return text
            .Replace(new string(escapeSymbol, 2), placeholder.ToString())
            .Replace(escapeSymbol.ToString(), string.Empty)
            .Replace(placeholder.ToString(), escapeSymbol.ToString());
    }
}
