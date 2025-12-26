using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using JetJot.Models;

namespace JetJot;

public partial class MainWindow : Window
{
    private readonly Manuscript _manuscript = new();
    private Document? _activeDocument;

    public MainWindow()
    {
        InitializeComponent();

        // Make title bar draggable
        var titleBar = this.FindControl<Border>("TitleBar");
        if (titleBar != null)
        {
            titleBar.PointerPressed += (sender, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    BeginMoveDrag(e);
                }
            };
        }

        // Create a couple of starter documents
        var doc1 = new Document { Title = "First Document", Text = "Start writing..." };
        var doc2 = new Document { Title = "Second Document", Text = "More ideas here..." };

        _manuscript.Documents.Add(doc1);
        _manuscript.Documents.Add(doc2);

        // Populate sidebar
        DocumentList.ItemsSource = _manuscript.Documents;

        // Select first document by default
        _activeDocument = doc1;
        DocumentList.SelectedItem = doc1;
        Editor.Text = doc1.Text;

        // Sidebar selection changes active document
        DocumentList.SelectionChanged += OnDocumentSelected;

        // Typing updates active document
        Editor.TextChanged += OnEditorTextChanged;

        // New document button
        NewDocumentButton.Click += OnNewDocumentClicked;

        // Font size selector
        FontSizeComboBox.SelectionChanged += OnFontSizeChanged;

        // Set goal button
        SetGoalButton.Click += OnSetGoalClicked;

        // Update word count initially
        UpdateWordCount();
    }

    private void OnToggleToolbar(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ToolbarPanel.IsVisible = !ToolbarPanel.IsVisible;
        CheckToolbar.IsChecked = ToolbarPanel.IsVisible;
    }

    private void OnToggleSidebar(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        bool isVisible = !SidebarPanel.IsVisible;
        SidebarPanel.IsVisible = isVisible;
        CheckSidebar.IsChecked = isVisible;

        // Adjust column width: 0 when hidden, 240 when visible
        var mainGrid = this.FindControl<Grid>("MainGrid");
        if (mainGrid != null && mainGrid.ColumnDefinitions.Count > 0)
        {
            mainGrid.ColumnDefinitions[0].Width = isVisible ? new GridLength(240) : new GridLength(0);
        }
    }

    private void OnToggleFooter(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FooterPanel.IsVisible = !FooterPanel.IsVisible;
        CheckFooter.IsChecked = FooterPanel.IsVisible;
    }

    private void OnMinimizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void OnNewManuscriptClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Clear all documents and start fresh
        _manuscript.Documents.Clear();
        _activeDocument = null;
        Editor.Text = string.Empty;
    }

    private void OnExitClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void OnUndoClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Editor.Undo();
    }

    private void OnRedoClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Editor.Redo();
    }

    private void OnCutClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Editor.Cut();
    }

    private void OnCopyClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Editor.Copy();
    }

    private void OnPasteClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Editor.Paste();
    }

    private async void OnAboutClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "About JetJot",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var stackPanel = new StackPanel
        {
            Margin = new Avalonia.Thickness(30),
            Spacing = 15,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var titleText = new TextBlock
        {
            Text = "JetJot",
            FontSize = 28,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var taglineText = new TextBlock
        {
            Text = "Let Your Creativity Fly",
            FontSize = 16,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#CCCCCC")),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var versionText = new TextBlock
        {
            Text = "Version 0.05",
            FontSize = 12,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#999999")),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var okButton = new Button
        {
            Content = "OK",
            Width = 100,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 10, 0, 0)
        };

        okButton.Click += (s, args) => dialog.Close();

        stackPanel.Children.Add(titleText);
        stackPanel.Children.Add(taglineText);
        stackPanel.Children.Add(versionText);
        stackPanel.Children.Add(okButton);

        dialog.Content = stackPanel;

        await dialog.ShowDialog(this);
    }

    private void OnDocumentSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (DocumentList.SelectedItem is Document doc)
        {
            _activeDocument = doc;
            Editor.Text = doc.Text;
        }
    }

    private void OnEditorTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_activeDocument != null)
        {
            _activeDocument.Text = Editor.Text ?? string.Empty;
            UpdateWordCount();
        }
    }

    private void OnNewDocumentClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var newDoc = new Document { Title = "Untitled", Text = "" };
        _manuscript.Documents.Add(newDoc);

        // Select the new document
        DocumentList.SelectedItem = newDoc;
        _activeDocument = newDoc;
        Editor.Text = newDoc.Text;
    }

    private async void OnDocumentItemDoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Document doc)
        {
            await ShowRenameDialog(doc);
        }
    }

    private async void OnRenameMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is Document doc)
        {
            await ShowRenameDialog(doc);
        }
    }

    private async System.Threading.Tasks.Task ShowRenameDialog(Document doc)
    {
        var dialog = new Window
        {
            Title = "Rename Document",
            Width = 350,
            Height = 120,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var textBox = new TextBox
        {
            Text = doc.Title,
            Margin = new Avalonia.Thickness(20, 20, 20, 10),
            Watermark = "Document title"
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(20, 0, 20, 20)
        };

        var okButton = new Button
        {
            Content = "OK",
            Width = 80,
            Margin = new Avalonia.Thickness(0, 0, 10, 0)
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 80
        };

        okButton.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                doc.Title = textBox.Text;
            }
            dialog.Close();
        };

        cancelButton.Click += (s, e) => dialog.Close();

        textBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                okButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            }
            else if (e.Key == Key.Escape)
            {
                dialog.Close();
            }
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        var panel = new StackPanel();
        panel.Children.Add(textBox);
        panel.Children.Add(buttonPanel);

        dialog.Content = panel;

        textBox.AttachedToVisualTree += (s, e) =>
        {
            textBox.Focus();
            textBox.SelectAll();
        };

        await dialog.ShowDialog(this);
    }

    private void OnMoveUpClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is Document doc)
        {
            int index = _manuscript.Documents.IndexOf(doc);
            if (index > 0)
            {
                _manuscript.Documents.Move(index, index - 1);
            }
        }
    }

    private void OnMoveDownClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is Document doc)
        {
            int index = _manuscript.Documents.IndexOf(doc);
            if (index < _manuscript.Documents.Count - 1)
            {
                _manuscript.Documents.Move(index, index + 1);
            }
        }
    }

    private async void OnDeleteMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is Document doc)
        {
            var dialog = new Window
            {
                Title = "Delete Document",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var stackPanel = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 20
            };

            var messageText = new TextBlock
            {
                Text = $"Are you sure you want to delete '{doc.Title}'?",
                FontSize = 14,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Spacing = 10
            };

            var confirmButton = new Button
            {
                Content = "Delete",
                Width = 100,
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D32F2F"))
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 100
            };

            confirmButton.Click += (s, args) =>
            {
                _manuscript.Documents.Remove(doc);

                // If we just deleted the active document, clear the editor
                if (_activeDocument == doc)
                {
                    _activeDocument = null;
                    Editor.Text = string.Empty;
                }

                dialog.Close();
            };

            cancelButton.Click += (s, args) => dialog.Close();

            buttonPanel.Children.Add(confirmButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(messageText);
            stackPanel.Children.Add(buttonPanel);

            dialog.Content = stackPanel;

            await dialog.ShowDialog(this);
        }
    }

    private void OnFontSizeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FontSizeComboBox.SelectedItem is ComboBoxItem item &&
            item.Content is string sizeText &&
            int.TryParse(sizeText, out int fontSize))
        {
            Editor.FontSize = fontSize;
        }
    }

    private void UpdateWordCount()
    {
        if (_activeDocument == null)
        {
            WordCountText.Text = "0 / 1000 words";
            WordProgressBar.Maximum = 1000;
            WordProgressBar.Value = 0;
            return;
        }

        // Count words in the current document
        int wordCount = CountWords(_activeDocument.Text);
        int goal = _activeDocument.WordGoal;

        // Update UI
        WordCountText.Text = $"{wordCount} / {goal} words";
        WordProgressBar.Maximum = goal;
        WordProgressBar.Value = Math.Min(wordCount, goal);
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        // Split by whitespace and count non-empty entries
        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private async void OnSetGoalClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_activeDocument == null)
            return;

        var dialog = new Window
        {
            Title = "Set Word Goal",
            Width = 350,
            Height = 140,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var stackPanel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20)
        };

        var label = new TextBlock
        {
            Text = "Enter your word count goal:",
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        };

        var textBox = new TextBox
        {
            Text = _activeDocument.WordGoal.ToString(),
            Watermark = "e.g., 1000",
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        var okButton = new Button
        {
            Content = "OK",
            Width = 80,
            Margin = new Avalonia.Thickness(0, 0, 10, 0)
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 80
        };

        okButton.Click += (s, args) =>
        {
            if (int.TryParse(textBox.Text, out int goal) && goal > 0)
            {
                _activeDocument.WordGoal = goal;
                UpdateWordCount();
            }
            dialog.Close();
        };

        cancelButton.Click += (s, args) => dialog.Close();

        textBox.KeyDown += (s, args) =>
        {
            if (args.Key == Key.Enter)
            {
                okButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            }
            else if (args.Key == Key.Escape)
            {
                dialog.Close();
            }
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        stackPanel.Children.Add(label);
        stackPanel.Children.Add(textBox);
        stackPanel.Children.Add(buttonPanel);

        dialog.Content = stackPanel;

        textBox.AttachedToVisualTree += (s, args) =>
        {
            textBox.Focus();
            textBox.SelectAll();
        };

        await dialog.ShowDialog(this);
    }

}
