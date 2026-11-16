namespace Task17_Rational;

public struct Rational
{
    public int Numerator;   
    public int Denominator;  

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

    public double Value => (double)Numerator / Denominator;
    public int CompareTo(Rational other)
    {
        long left = (long)Numerator * other.Denominator;
        long right = (long)other.Numerator * Denominator;
        return left.CompareTo(right);
    }

    public override string ToString() => $"{Numerator}/{Denominator}";
}

public static class RationalMath
{
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
