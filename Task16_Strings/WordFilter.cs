namespace Task16_Strings;


public static class WordFilter
{
    public const int MinWords = 5;

    private static readonly char[] Separators =
    {
        ' ', '\t', '\r', '\n', ',', '.', ';', ':', '!', '?', '(', ')', '"', '«', '»', '—'
    };

    public static string[] SplitWords(string text)
    {
        return text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
    }

    public static bool HasLetterMoreThanTwice(string word)
    {
        string lower = word.ToLower();

        foreach (char letter in lower)
        {
            if (!char.IsLetter(letter))
                continue;

            int count = lower.Length - lower.Replace(letter.ToString(), string.Empty).Length;
            if (count > 2)
                return true;
        }

        return false;
    }

    public static string Filter(string text)
    {
        string result = string.Empty;

        foreach (string word in SplitWords(text))
        {
            if (HasLetterMoreThanTwice(word))
                result = result.Length == 0 ? word : string.Concat(result, " ", word);
        }

        return result;
    }
}
