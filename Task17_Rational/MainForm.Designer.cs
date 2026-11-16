namespace Task17_Rational;

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
        lblNumerator = new Label();
        txtNumerator = new TextBox();
        lblDenominator = new Label();
        txtDenominator = new TextBox();
        btnAdd = new Button();
        lblList = new Label();
        lstNumbers = new ListBox();
        lblCount = new Label();
        btnFindMax = new Button();
        lblResult = new Label();
        btnClear = new Button();
        btnExit = new Button();
        SuspendLayout();
        //
        // lblTask
        //
        lblTask.Location = new Point(12, 9);
        lblTask.Name = "lblTask";
        lblTask.Size = new Size(460, 40);
        lblTask.TabIndex = 0;
        lblTask.Text = "Рациональное число — запись из двух полей: числитель и знаменатель. " +
            "Дан массив из N рациональных чисел. Найти максимальное среди них.";
        //
        // lblNumerator
        //
        lblNumerator.AutoSize = true;
        lblNumerator.Location = new Point(12, 60);
        lblNumerator.Name = "lblNumerator";
        lblNumerator.Size = new Size(64, 15);
        lblNumerator.TabIndex = 1;
        lblNumerator.Text = "Числитель:";
        //
        // txtNumerator
        //
        txtNumerator.Location = new Point(12, 80);
        txtNumerator.Name = "txtNumerator";
        txtNumerator.Size = new Size(120, 23);
        txtNumerator.TabIndex = 2;
        //
        // lblDenominator
        //
        lblDenominator.AutoSize = true;
        lblDenominator.Location = new Point(150, 60);
        lblDenominator.Name = "lblDenominator";
        lblDenominator.Size = new Size(77, 15);
        lblDenominator.TabIndex = 3;
        lblDenominator.Text = "Знаменатель:";
        //
        // txtDenominator
        //
        txtDenominator.Location = new Point(150, 80);
        txtDenominator.Name = "txtDenominator";
        txtDenominator.Size = new Size(120, 23);
        txtDenominator.TabIndex = 4;
        //
        // btnAdd
        //
        btnAdd.Location = new Point(290, 76);
        btnAdd.Name = "btnAdd";
        btnAdd.Size = new Size(120, 30);
        btnAdd.TabIndex = 5;
        btnAdd.Text = "Добавить";
        btnAdd.UseVisualStyleBackColor = true;
        btnAdd.Click += btnAdd_Click;
        //
        // lblList
        //
        lblList.AutoSize = true;
        lblList.Location = new Point(12, 120);
        lblList.Name = "lblList";
        lblList.Size = new Size(95, 15);
        lblList.TabIndex = 6;
        lblList.Text = "Введённые числа:";
        //
        // lstNumbers
        //
        lstNumbers.FormattingEnabled = true;
        lstNumbers.ItemHeight = 15;
        lstNumbers.Location = new Point(12, 140);
        lstNumbers.Name = "lstNumbers";
        lstNumbers.Size = new Size(258, 154);
        lstNumbers.TabIndex = 7;
        //
        // lblCount
        //
        lblCount.AutoSize = true;
        lblCount.Location = new Point(290, 140);
        lblCount.Name = "lblCount";
        lblCount.Size = new Size(38, 15);
        lblCount.TabIndex = 8;
        lblCount.Text = "N = 0";
        //
        // btnFindMax
        //
        btnFindMax.Location = new Point(290, 165);
        btnFindMax.Name = "btnFindMax";
        btnFindMax.Size = new Size(160, 30);
        btnFindMax.TabIndex = 9;
        btnFindMax.Text = "Найти максимум";
        btnFindMax.UseVisualStyleBackColor = true;
        btnFindMax.Click += btnFindMax_Click;
        //
        // lblResult
        //
        lblResult.Location = new Point(12, 305);
        lblResult.Name = "lblResult";
        lblResult.Size = new Size(460, 20);
        lblResult.TabIndex = 10;
        //
        // btnClear
        //
        btnClear.Location = new Point(226, 340);
        btnClear.Name = "btnClear";
        btnClear.Size = new Size(120, 30);
        btnClear.TabIndex = 11;
        btnClear.Text = "Очистить";
        btnClear.UseVisualStyleBackColor = true;
        btnClear.Click += btnClear_Click;
        //
        // btnExit
        //
        btnExit.Location = new Point(352, 340);
        btnExit.Name = "btnExit";
        btnExit.Size = new Size(120, 30);
        btnExit.TabIndex = 12;
        btnExit.Text = "Выход";
        btnExit.UseVisualStyleBackColor = true;
        btnExit.Click += btnExit_Click;
        //
        // MainForm
        //
        AcceptButton = btnAdd;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(484, 381);
        Controls.Add(lblTask);
        Controls.Add(lblNumerator);
        Controls.Add(txtNumerator);
        Controls.Add(lblDenominator);
        Controls.Add(txtDenominator);
        Controls.Add(btnAdd);
        Controls.Add(lblList);
        Controls.Add(lstNumbers);
        Controls.Add(lblCount);
        Controls.Add(btnFindMax);
        Controls.Add(lblResult);
        Controls.Add(btnClear);
        Controls.Add(btnExit);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Задание 17.11 — структуры";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblTask;
    private Label lblNumerator;
    private TextBox txtNumerator;
    private Label lblDenominator;
    private TextBox txtDenominator;
    private Button btnAdd;
    private Label lblList;
    private ListBox lstNumbers;
    private Label lblCount;
    private Button btnFindMax;
    private Label lblResult;
    private Button btnClear;
    private Button btnExit;
}
