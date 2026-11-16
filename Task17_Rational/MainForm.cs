namespace Task17_Rational;

public partial class MainForm : Form
{
    private Rational[] numbers = Array.Empty<Rational>();

    public MainForm()
    {
        InitializeComponent();
        UpdateCount();
    }

    private void btnAdd_Click(object sender, EventArgs e)
    {
        if (!int.TryParse(txtNumerator.Text.Trim(), out int numerator))
        {
            ShowInputError("Числитель должен быть целым числом.", txtNumerator);
            return;
        }

        if (!int.TryParse(txtDenominator.Text.Trim(), out int denominator))
        {
            ShowInputError("Знаменатель должен быть целым числом.", txtDenominator);
            return;
        }

        if (denominator == 0 || denominator == int.MinValue)
        {
            ShowInputError("Знаменатель не может быть равен нулю.", txtDenominator);
            return;
        }

        var number = new Rational(numerator, denominator);

        Array.Resize(ref numbers, numbers.Length + 1);
        numbers[^1] = number;

        lstNumbers.Items.Add($"{numbers.Length}) {number}  ≈ {number.Value:0.####}");
        UpdateCount();

        txtNumerator.Clear();
        txtDenominator.Clear();
        txtNumerator.Focus();
    }

    private void btnFindMax_Click(object sender, EventArgs e)
    {
        if (numbers.Length == 0)
        {
            ShowInputError("Сначала добавьте хотя бы одно рациональное число.", txtNumerator);
            return;
        }

        Rational max = RationalMath.FindMax(numbers);
        lblResult.Text = $"Максимальное число: {max}  ≈ {max.Value:0.####}";

        AskRepeat();
    }

    private void btnClear_Click(object sender, EventArgs e)
    {
        ClearFields();
    }

    private void btnExit_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void ShowInputError(string message, Control field)
    {
        MessageBox.Show(message, "Некорректный ввод", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        field.Focus();
    }

    private void AskRepeat()
    {
        DialogResult answer = MessageBox.Show(
            "Повторить выполнение программы?",
            "Повтор",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer == DialogResult.Yes)
            ClearFields();
        else
            Close();
    }

    private void ClearFields()
    {
        numbers = Array.Empty<Rational>();
        txtNumerator.Clear();
        txtDenominator.Clear();
        lstNumbers.Items.Clear();
        lblResult.Text = string.Empty;
        UpdateCount();
        txtNumerator.Focus();
    }

    private void UpdateCount()
    {
        lblCount.Text = $"N = {numbers.Length}";
    }
}
