using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Tepi.Linux.Core;
using Tepi.Linux.Services;

namespace Tepi.Linux.UI.Tabs
{
    public class TasksTabView : UserControl
    {
        private readonly TaskManager _taskManager;
        private readonly StackPanel _taskListContainer;
        private readonly TextBox _inputBox;

        public TasksTabView(TaskManager taskManager)
        {
            _taskManager = taskManager;

            var root = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                Margin = new Thickness(4, 2, 4, 2)
            };

            // 1. Input Add Row
            var inputGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Margin = new Thickness(0, 0, 0, 8)
            };

            _inputBox = new TextBox
            {
                Watermark = "Tambah tugas kuliah / to-do baru...",
                FontSize = 11.5,
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                Foreground = TepiTheme.BrushText,
                CornerRadius = new CornerRadius(8),
                Height = 32
            };
            _inputBox.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Enter)
                {
                    SubmitNewTask();
                }
            };
            inputGrid.Children.Add(_inputBox);
            Grid.SetColumn(_inputBox, 0);

            var addBtn = new Button
            {
                Content = "+",
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(8),
                Background = TepiTheme.BrushAccent,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            addBtn.Click += (s, e) => SubmitNewTask();
            inputGrid.Children.Add(addBtn);
            Grid.SetColumn(addBtn, 1);

            root.Children.Add(inputGrid);
            Grid.SetRow(inputGrid, 0);

            // 2. Scrollable Task List
            var scroll = new ScrollViewer
            {
                Height = 140,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
            };

            _taskListContainer = new StackPanel { Spacing = 6 };
            scroll.Content = _taskListContainer;
            root.Children.Add(scroll);
            Grid.SetRow(scroll, 1);

            // 3. Footer summary
            var footerGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Margin = new Thickness(2, 6, 2, 0)
            };
            var storageNote = new TextBlock
            {
                Text = "Tersimpan offline di ~/.config/tepi/tasks.json",
                FontSize = 10,
                Foreground = TepiTheme.BrushFaint
            };
            footerGrid.Children.Add(storageNote);
            Grid.SetColumn(storageNote, 0);

            root.Children.Add(footerGrid);
            Grid.SetRow(footerGrid, 2);

            Content = root;

            _taskManager.TasksChanged += () =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(RenderTaskList);
            };

            RenderTaskList();
        }

        private void SubmitNewTask()
        {
            string text = _inputBox.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(text))
            {
                _taskManager.Add(text, "Hari ini");
                _inputBox.Text = "";
            }
        }

        private void RenderTaskList()
        {
            _taskListContainer.Children.Clear();
            var tasks = _taskManager.Tasks;

            if (tasks.Count == 0)
            {
                var emptyCard = new Border
                {
                    Background = TepiTheme.CardBackground,
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(14),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                emptyCard.Child = new TextBlock
                {
                    Text = "✓ Semua tugas & to-do kuliah selesai!",
                    FontSize = 11.5,
                    Foreground = TepiTheme.BrushPositive
                };
                _taskListContainer.Children.Add(emptyCard);
                return;
            }

            foreach (var task in tasks)
            {
                var row = CreateTaskRow(task);
                _taskListContainer.Children.Add(row);
            }
        }

        private Border CreateTaskRow(TaskItem task)
        {
            var card = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 6)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto")
            };

            // Checkbox
            var cb = new CheckBox
            {
                IsChecked = task.IsDone,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            cb.IsCheckedChanged += (s, e) => _taskManager.Toggle(task.Id);
            grid.Children.Add(cb);
            Grid.SetColumn(cb, 0);

            // Title
            var title = new TextBlock
            {
                Text = task.Title,
                FontSize = 12,
                Foreground = task.IsDone ? TepiTheme.BrushFaint : TepiTheme.BrushText,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (task.IsDone)
            {
                title.TextDecorations = TextDecorations.Strikethrough;
            }
            grid.Children.Add(title);
            Grid.SetColumn(title, 1);

            // Due Pill
            var duePill = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6, 2),
                Margin = new Thickness(6, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            duePill.Child = new TextBlock
            {
                Text = task.DueText,
                FontSize = 9.5,
                Foreground = TepiTheme.BrushSub
            };
            grid.Children.Add(duePill);
            Grid.SetColumn(duePill, 2);

            // Delete Button
            var delBtn = new Button
            {
                Content = "✕",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromRgb(180, 80, 80)),
                FontSize = 11,
                Padding = new Thickness(4, 2),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                VerticalAlignment = VerticalAlignment.Center
            };
            delBtn.Click += (s, e) => _taskManager.Delete(task.Id);
            grid.Children.Add(delBtn);
            Grid.SetColumn(delBtn, 3);

            card.Child = grid;
            return card;
        }
    }
}
