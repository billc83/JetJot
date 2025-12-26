using Avalonia.Controls;
using Avalonia.Input;
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
}
