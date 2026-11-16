namespace Task16_Strings;

/// <summary>
/// Логика задания (вариант 6): из текста отбираются слова,
/// в которых одна и та же буква встречается более двух раз (например, «молоко»).
/// Класс не зависит от формы, поэтому его можно проверять отдельно.
/// </summary>
public static class WordFilter
{
    /// <summary>Минимальное количество слов во входном тексте.</summary>
    public const int MinWords = 5;

    // Символы, которые разделяют слова. Дефис не входит, поэтому «кто-то» остаётся одним словом.
    private static readonly char[] Separators =
    {
        ' ', '\t', '\r', '\n', ',', '.', ';', ':', '!', '?', '(', ')', '"', '«', '»', '—'
    };

    /// <summary>Разбивает текст на слова (метод string.Split).</summary>
    public static string[] SplitWords(string text)
    {
        return text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Проверяет, есть ли в слове буква, которая встречается более двух раз.
    /// Регистр не учитывается: «Молоко» тоже подходит.
    /// </summary>
    public static bool HasLetterMoreThanTwice(string word)
    {
        string lower = word.ToLower();

        foreach (char letter in lower)
        {
            if (!char.IsLetter(letter))
                continue;

            // Сколько раз встречается буква: длина слова минус длина слова без этой буквы.
            int count = lower.Length - lower.Replace(letter.ToString(), string.Empty).Length;
            if (count > 2)
                return true;
        }

        return false;
    }

    /// <summary>Формирует строку только из подходящих слов, через пробел.</summary>
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
