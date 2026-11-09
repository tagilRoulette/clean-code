
namespace Markdown;
internal class BasicTextToken : IToken
{
    public string RawText { get; }


    public BasicTextToken(string rawText)
    {
        RawText = rawText;
    }
}
