namespace Task17_Rational;

/// <summary>
/// Рациональное число: запись (структура) с двумя полями — числитель и знаменатель.
/// Знаменатель всегда хранится положительным, знак переносится в числитель.
/// </summary>
public struct Rational
{
    public int Numerator;    // числитель
    public int Denominator;  // знаменатель (> 0)

    public Rational(int numerator, int denominator)
    {
        if (denominator == 0)
            throw new ArgumentException("Знаменатель не может быть равен нулю.", nameof(denominator));

        if (denominator < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        Numerator = numerator;
        Denominator = denominator;
    }

    /// <summary>Значение дроби в виде десятичного числа (только для вывода).</summary>
    public double Value => (double)Numerator / Denominator;

    /// <summary>
    /// Сравнение без потери точности: a/b &gt; c/d  ⇔  a·d &gt; c·b (знаменатели положительные).
    /// Умножение в long, чтобы не было переполнения.
    /// </summary>
    public int CompareTo(Rational other)
    {
        long left = (long)Numerator * other.Denominator;
        long right = (long)other.Numerator * Denominator;
        return left.CompareTo(right);
    }

    public override string ToString() => $"{Numerator}/{Denominator}";
}

/// <summary>Функции для работы с массивом рациональных чисел.</summary>
public static class RationalMath
{
    /// <summary>Находит максимальное число в массиве из N рациональных чисел.</summary>
    public static Rational FindMax(Rational[] numbers)
    {
        if (numbers.Length == 0)
            throw new ArgumentException("Массив пуст.", nameof(numbers));

        Rational max = numbers[0];
        for (int i = 1; i < numbers.Length; i++)
        {
            if (numbers[i].CompareTo(max) > 0)
                max = numbers[i];
        }

        return max;
    }
}
