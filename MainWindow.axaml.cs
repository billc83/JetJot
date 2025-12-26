using Avalonia.Controls;
using Avalonia.Input;
using System.ComponentModel;
using System.Collections.Generic;

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
}
