namespace Task16_Strings;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
    }

    /// <summary>Кнопка «Выполнить»: проверка ввода и формирование результата.</summary>
    private void btnRun_Click(object sender, EventArgs e)
    {
        string text = txtInput.Text.Trim();
        string[] words = WordFilter.SplitWords(text);

        if (words.Length < WordFilter.MinWords)
        {
            ShowInputError($"Текст должен состоять не менее чем из {WordFilter.MinWords} слов.\n" +
                           $"Сейчас слов: {words.Length}.");
            return;
        }

        string result = WordFilter.Filter(text);

        if (result.Length == 0)
        {
            txtResult.Text = string.Empty;
            lblInfo.Text = "Слов, где одна буква встречается более двух раз, нет.";
        }
        else
        {
            txtResult.Text = result;
            lblInfo.Text = $"Всего слов: {words.Length}, подходящих: {WordFilter.SplitWords(result).Length}.";
        }

        AskRepeat();
    }

    /// <summary>Кнопка «Очистить»: очищает все поля.</summary>
    private void btnClear_Click(object sender, EventArgs e)
    {
        ClearFields();
    }

    /// <summary>Кнопка «Выход»: завершает программу.</summary>
    private void btnExit_Click(object sender, EventArgs e)
    {
        Close();
    }

    /// <summary>Сообщение о некорректном вводе данных.</summary>
    private void ShowInputError(string message)
    {
        MessageBox.Show(message, "Некорректный ввод", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        txtInput.Focus();
    }

    /// <summary>Спрашивает, повторить ли работу программы.</summary>
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
        txtInput.Clear();
        txtResult.Clear();
        lblInfo.Text = string.Empty;
        txtInput.Focus();
    }
}
