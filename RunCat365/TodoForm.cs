using RunCat365.Properties;

namespace RunCat365
{
    internal class TodoForm : Form
    {
        private static readonly Color FormBackground = Color.FromArgb(45, 45, 45);
        private static readonly Color ControlBackground = Color.FromArgb(60, 60, 60);
        private static readonly Color ButtonBackground = Color.FromArgb(70, 70, 70);
        private static readonly Color BorderAccent = Color.FromArgb(100, 100, 100);
        private static readonly Color TextPrimary = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(150, 150, 150);

        private readonly TodoRepository repository = new();
        private readonly TextBox titleTextBox;
        private readonly ListView todoListView;
        private readonly ColumnHeader titleColumn;
        private readonly Button addButton;
        private readonly Button deleteButton;
        private bool isRefreshing = false;

        internal TodoForm()
        {
            var fontFamily = SupportedLanguageExtension.GetCurrentLanguage().GetFontName();

            Text = Strings.Window_Todo;
            Icon = Resources.AppIcon;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            BackColor = FormBackground;
            ForeColor = TextPrimary;
            Font = new Font(fontFamily, 9F);
            Padding = new Padding(16);
            ClientSize = new Size(360, 420);
            MinimumSize = new Size(300, 300);

            titleTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = ControlBackground,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = Strings.Todo_Placeholder,
                Margin = new Padding(0, 0, 8, 8)
            };

            addButton = CreateButton(Strings.Todo_Add);
            addButton.Margin = new Padding(0, 0, 0, 8);

            titleColumn = new ColumnHeader();
            todoListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                HeaderStyle = ColumnHeaderStyle.None,
                CheckBoxes = true,
                FullRowSelect = true,
                MultiSelect = false,
                HideSelection = false,
                BackColor = ControlBackground,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 8)
            };
            todoListView.Columns.Add(titleColumn);

            deleteButton = CreateButton(Strings.Todo_Delete);
            deleteButton.Anchor = AnchorStyles.Left;
            deleteButton.Margin = new Padding(0);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(titleTextBox, 0, 0);
            root.Controls.Add(addButton, 1, 0);
            root.Controls.Add(todoListView, 0, 1);
            root.SetColumnSpan(todoListView, 2);
            root.Controls.Add(deleteButton, 0, 2);
            Controls.Add(root);

            AcceptButton = addButton;
            addButton.Click += (sender, e) => AddTodo();
            deleteButton.Click += (sender, e) => DeleteSelectedTodo();
            todoListView.ItemChecked += TodoListViewItemChecked;
            todoListView.SelectedIndexChanged += (sender, e) => UpdateDeleteButtonState();
            todoListView.KeyDown += TodoListViewKeyDown;
            todoListView.Resize += (sender, e) => FitTitleColumnWidth();

            RefreshTodoList();
        }

        private static Button CreateButton(string text)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12, 2, 12, 2),
                FlatStyle = FlatStyle.Flat,
                BackColor = ButtonBackground,
                ForeColor = TextPrimary,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = BorderAccent;
            return button;
        }

        private void AddTodo()
        {
            var title = titleTextBox.Text.Trim();
            if (title.Length == 0) return;
            if (!repository.Add(title)) ShowSaveFailedMessage();
            titleTextBox.Clear();
            RefreshTodoList();
            titleTextBox.Focus();
        }

        private void DeleteSelectedTodo()
        {
            if (todoListView.SelectedIndices.Count == 0) return;
            var index = todoListView.SelectedIndices[0];
            if (!repository.RemoveAt(index)) ShowSaveFailedMessage();
            RefreshTodoList();
            if (todoListView.Items.Count == 0) return;
            var nextIndex = Math.Min(index, todoListView.Items.Count - 1);
            todoListView.Items[nextIndex].Selected = true;
            todoListView.Items[nextIndex].Focused = true;
        }

        private void TodoListViewItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            if (isRefreshing) return;
            ApplyCompletedStyle(e.Item);
            if (!repository.SetCompleted(e.Item.Index, e.Item.Checked)) ShowSaveFailedMessage();
        }

        private void TodoListViewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete) return;
            DeleteSelectedTodo();
            e.Handled = true;
        }

        private void RefreshTodoList()
        {
            isRefreshing = true;
            todoListView.BeginUpdate();
            todoListView.Items.Clear();
            foreach (var todo in repository.GetAll())
            {
                var item = new ListViewItem(todo.Title) { Checked = todo.IsCompleted };
                ApplyCompletedStyle(item);
                todoListView.Items.Add(item);
            }
            todoListView.EndUpdate();
            isRefreshing = false;
            FitTitleColumnWidth();
            UpdateDeleteButtonState();
        }

        private static void ApplyCompletedStyle(ListViewItem item)
        {
            item.ForeColor = item.Checked ? TextMuted : TextPrimary;
        }

        private void FitTitleColumnWidth()
        {
            titleColumn.Width = todoListView.ClientSize.Width;
        }

        private void UpdateDeleteButtonState()
        {
            deleteButton.Enabled = todoListView.SelectedIndices.Count > 0;
        }

        private static void ShowSaveFailedMessage()
        {
            MessageBox.Show(Strings.Todo_SaveFailed, Strings.Message_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
