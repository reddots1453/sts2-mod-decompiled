using System.Text;

namespace lemonSpire2.util;

public static class BbCodeUtils
{
    private static readonly HashSet<string> SelfClosingTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "br",
        "hr",
        "lb",
        "rb",
        "tab",
        "*"
    };

    public static bool IsClosed(string bbCode)
    {
        ArgumentNullException.ThrowIfNull(bbCode);
        return string.Equals(bbCode, AutoCloseUnclosedTags(bbCode), StringComparison.Ordinal);
    }

    public static string AutoCloseUnclosedTags(string bbCode)
    {
        ArgumentNullException.ThrowIfNull(bbCode);
        if (bbCode.Length == 0) return bbCode;

        var openTags = new List<string>();
        var output = new StringBuilder(bbCode.Length + 32);
        var index = 0;

        while (index < bbCode.Length)
        {
            var openBracketIndex = bbCode.IndexOf('[', index);
            if (openBracketIndex < 0)
            {
                output.Append(bbCode, index, bbCode.Length - index);
                break;
            }

            output.Append(bbCode, index, openBracketIndex - index);

            var closeBracketIndex = bbCode.IndexOf(']', openBracketIndex + 1);
            if (closeBracketIndex < 0)
            {
                output.Append(bbCode, openBracketIndex, bbCode.Length - openBracketIndex);
                break;
            }

            if (!TryParseTag(bbCode, openBracketIndex, closeBracketIndex, out var tagName, out var isClosing,
                    out var isSelfClosing))
            {
                output.Append(bbCode, openBracketIndex, closeBracketIndex - openBracketIndex + 1);
                index = closeBracketIndex + 1;
                continue;
            }

            if (isClosing)
            {
                var matchIndex = FindMatchingTagIndex(openTags, tagName);
                if (matchIndex >= 0)
                {
                    while (openTags.Count - 1 > matchIndex)
                    {
                        AppendClosingTag(output, openTags[^1]);
                        openTags.RemoveAt(openTags.Count - 1);
                    }

                    openTags.RemoveAt(openTags.Count - 1);
                    output.Append(bbCode, openBracketIndex, closeBracketIndex - openBracketIndex + 1);
                }
            }
            else
            {
                output.Append(bbCode, openBracketIndex, closeBracketIndex - openBracketIndex + 1);
                if (!isSelfClosing) openTags.Add(tagName);
            }

            index = closeBracketIndex + 1;
        }

        for (var i = openTags.Count - 1; i >= 0; i--) AppendClosingTag(output, openTags[i]);

        return output.ToString();
    }

    private static bool TryParseTag(string text, int openBracketIndex, int closeBracketIndex, out string tagName,
        out bool isClosing,
        out bool isSelfClosing)
    {
        tagName = string.Empty;
        isClosing = false;
        isSelfClosing = false;

        if (closeBracketIndex - openBracketIndex < 2) return false;

        var content = text.AsSpan(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1).Trim();
        if (content.Length == 0) return false;

        if (content[0] == '/')
        {
            isClosing = true;
            content = content[1..].TrimStart();
            if (content.Length == 0) return false;
        }

        if (content[^1] == '/')
        {
            isSelfClosing = true;
            content = content[..^1].TrimEnd();
            if (content.Length == 0) return false;
        }

        var tagNameEndIndex = 0;
        while (tagNameEndIndex < content.Length &&
               !char.IsWhiteSpace(content[tagNameEndIndex]) &&
               content[tagNameEndIndex] != '=')
            tagNameEndIndex++;

        if (tagNameEndIndex == 0) return false;

        tagName = content[..tagNameEndIndex].ToString();
        if (!isClosing && SelfClosingTags.Contains(tagName)) isSelfClosing = true;

        if (isClosing) isSelfClosing = false;

        return true;
    }

    private static int FindMatchingTagIndex(List<string> openTags, string closingTag)
    {
        for (var i = openTags.Count - 1; i >= 0; i--)
            if (string.Equals(openTags[i], closingTag, StringComparison.OrdinalIgnoreCase))
                return i;

        return -1;
    }

    private static void AppendClosingTag(StringBuilder output, string tagName)
    {
        output.Append("[/");
        output.Append(tagName);
        output.Append(']');
    }
}
