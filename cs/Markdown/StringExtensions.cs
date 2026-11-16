using System.Text;

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

    public static IEnumerable<string> LazySplit(this string source, HashSet<char> separators)
    {
        if (string.IsNullOrEmpty(source)) yield break;

        var sb = new StringBuilder();
        foreach (char c in source)
        {
            if (separators.Contains(c))
            {
                if (sb.Length > 0)
                    yield return sb.ToString();
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        yield return sb.ToString();
    }
}
