namespace Task16_Strings;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        lblTask = new Label();
        lblInput = new Label();
        txtInput = new TextBox();
        lblResult = new Label();
        txtResult = new TextBox();
        lblInfo = new Label();
        btnRun = new Button();
        btnClear = new Button();
        btnExit = new Button();
        SuspendLayout();
        //
        // lblTask
        //
        lblTask.Location = new Point(12, 9);
        lblTask.Name = "lblTask";
        lblTask.Size = new Size(560, 40);
        lblTask.TabIndex = 0;
        lblTask.Text = "Дан текст, состоящий не менее чем из пяти слов. Сформировать строку из слов, " +
            "где одинаковые буквы встречаются более двух раз (например, «молоко»).";
        //
        // lblInput
        //
        lblInput.AutoSize = true;
        lblInput.Location = new Point(12, 55);
        lblInput.Name = "lblInput";
        lblInput.Size = new Size(200, 15);
        lblInput.TabIndex = 1;
        lblInput.Text = "Введите текст (не менее 5 слов):";
        //
        // txtInput
        //
        txtInput.Location = new Point(12, 75);
        txtInput.Multiline = true;
        txtInput.Name = "txtInput";
        txtInput.ScrollBars = ScrollBars.Vertical;
        txtInput.Size = new Size(560, 90);
        txtInput.TabIndex = 2;
        //
        // lblResult
        //
        lblResult.AutoSize = true;
        lblResult.Location = new Point(12, 215);
        lblResult.Name = "lblResult";
        lblResult.Size = new Size(63, 15);
        lblResult.TabIndex = 4;
        lblResult.Text = "Результат:";
        //
        // txtResult
        //
        txtResult.Location = new Point(12, 235);
        txtResult.Multiline = true;
        txtResult.Name = "txtResult";
        txtResult.ReadOnly = true;
        txtResult.ScrollBars = ScrollBars.Vertical;
        txtResult.Size = new Size(560, 70);
        txtResult.TabIndex = 5;
        //
        // lblInfo
        //
        lblInfo.Location = new Point(12, 310);
        lblInfo.Name = "lblInfo";
        lblInfo.Size = new Size(560, 20);
        lblInfo.TabIndex = 6;
        //
        // btnRun
        //
        btnRun.Location = new Point(12, 175);
        btnRun.Name = "btnRun";
        btnRun.Size = new Size(120, 30);
        btnRun.TabIndex = 3;
        btnRun.Text = "Выполнить";
        btnRun.UseVisualStyleBackColor = true;
        btnRun.Click += btnRun_Click;
        //
        // btnClear
        //
        btnClear.Location = new Point(326, 340);
        btnClear.Name = "btnClear";
        btnClear.Size = new Size(120, 30);
        btnClear.TabIndex = 7;
        btnClear.Text = "Очистить";
        btnClear.UseVisualStyleBackColor = true;
        btnClear.Click += btnClear_Click;
        //
        // btnExit
        //
        btnExit.Location = new Point(452, 340);
        btnExit.Name = "btnExit";
        btnExit.Size = new Size(120, 30);
        btnExit.TabIndex = 8;
        btnExit.Text = "Выход";
        btnExit.UseVisualStyleBackColor = true;
        btnExit.Click += btnExit_Click;
        //
        // MainForm
        //
        AcceptButton = btnRun;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(584, 381);
        Controls.Add(lblTask);
        Controls.Add(lblInput);
        Controls.Add(txtInput);
        Controls.Add(btnRun);
        Controls.Add(lblResult);
        Controls.Add(txtResult);
        Controls.Add(lblInfo);
        Controls.Add(btnClear);
        Controls.Add(btnExit);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Задание 16.11 — строки";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblTask;
    private Label lblInput;
    private TextBox txtInput;
    private Label lblResult;
    private TextBox txtResult;
    private Label lblInfo;
    private Button btnRun;
    private Button btnClear;
    private Button btnExit;
}
