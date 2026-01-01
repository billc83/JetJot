using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.VisualTree;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetJot.Models;

namespace JetJot;

public partial class MainWindow : Window
{
    private readonly Manuscript _manuscript = new();
    private Document? _activeDocument;
    private readonly ManuscriptStorage _storage = new();
    private readonly string _recentsFilePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "JetJot",
        "recents.json");
    private readonly string _preferencesFilePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "JetJot",
        "preferences.json");

    // Drag and drop state
    private Document? _draggedDocument;
    private Point _dragStartPoint;
    private bool _isDragging;
    private int _dropTargetIndex = -1;
    private bool _isOverTrash = false;

    private static readonly Avalonia.Thickness DropIndicatorTopThickness = new(0, 2, 0, 0);
    private static readonly Avalonia.Thickness DropIndicatorBottomThickness = new(0, 0, 0, 2);

    private int _lastSelectionStart;
    private int _lastSelectionEnd;
    private int _lastCaretIndex;
    private bool _hasSelectionSnapshot;

    // Focus mode state
    private bool _preFocusModeToolbarVisible = true;
    private bool _preFocusModeSidebarVisible = true;
    private bool _preFocusModeFooterVisible = true;

    // Super Focus mode state
    private bool _isInSuperFocusMode = false;
    private bool _preSuperFocusModeToolbarVisible = true;
    private bool _preSuperFocusModeSidebarVisible = true;
    private bool _preSuperFocusModeFooterVisible = true;
    private bool _preSuperFocusModeTitleBarVisible = true;

    // Typewriter mode state
    private bool _isTypewriterMode = false;

    // Accent color
    private string _accentColor = "#4A5D73";

    // Currently selected font family name
    private string _currentFontFamily = "IBM Plex Sans";

    // Flag to prevent font change events during initialization
    private bool _isLoadingPreferences = false;

    // Spell checker
    private readonly SpellCheckerService _spellChecker = new();
    private bool _showSpellCheck = true;
    private int _currentSpellCheckIndex = -1;

    // Margin adjustment
    private bool _isDraggingLeftMargin = false;
    private bool _isDraggingRightMargin = false;
    private double _leftMargin = 40;
    private double _rightMargin = 40;

    public MainWindow()
    {
        InitializeComponent();

        // Add keyboard shortcuts
        this.KeyDown += (sender, e) =>
        {
            // Ctrl+Shift+N - New Project
            if (e.Key == Avalonia.Input.Key.N && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
            {
                OnNewManuscriptMenuClicked(null, null!);
                e.Handled = true;
            }
            // Ctrl+O - Open/Import Project
            else if (e.Key == Avalonia.Input.Key.O && e.KeyModifiers == KeyModifiers.Control)
            {
                OnImportManuscriptClicked(null, null!);
                e.Handled = true;
            }
            // Ctrl+N - New Document
            else if (e.Key == Avalonia.Input.Key.N && e.KeyModifiers == KeyModifiers.Control)
            {
                OnNewDocumentClicked(null, null!);
                e.Handled = true;
            }
            // Ctrl+S - Save Document
            else if (e.Key == Avalonia.Input.Key.S && e.KeyModifiers == KeyModifiers.Control)
            {
                SaveCurrentDocument();
                e.Handled = true;
            }
            // Ctrl+C - Copy
            else if (e.Key == Avalonia.Input.Key.C && e.KeyModifiers == KeyModifiers.Control)
            {
                OnCopyClicked(null, null!);
                e.Handled = true;
            }
            // Ctrl+X - Cut
            else if (e.Key == Avalonia.Input.Key.X && e.KeyModifiers == KeyModifiers.Control)
            {
                OnCutClicked(null, null!);
                e.Handled = true;
            }
            // Ctrl+V - Paste
            else if (e.Key == Avalonia.Input.Key.V && e.KeyModifiers == KeyModifiers.Control)
            {
                OnPasteClicked(null, null!);
                e.Handled = true;
            }
            // Ctrl+Shift+F - Find and Replace
            else if (e.Key == Avalonia.Input.Key.F && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
            {
                OnFindAndReplaceClicked(null, null!);
                e.Handled = true;
            }
            // Ctrl+Shift+T - Toggle Typewriter Mode
            else if (e.Key == Avalonia.Input.Key.T && e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift))
            {
                OnToggleTypewriterMode(null, null!);
                e.Handled = true;
            }
            // Ctrl+1 - Toggle Focus Mode
            else if (e.Key == Avalonia.Input.Key.D1 && e.KeyModifiers == KeyModifiers.Control)
            {
                OnToggleFocusMode(null, null!);
                e.Handled = true;
            }
            // Ctrl+0 - Toggle Super Focus Mode
            else if (e.Key == Avalonia.Input.Key.D0 && e.KeyModifiers == KeyModifiers.Control)
            {
                OnToggleSuperFocusMode(null, null!);
                e.Handled = true;
            }
            // F11 - Toggle Super Focus Mode
            else if (e.Key == Avalonia.Input.Key.F11)
            {
                OnToggleSuperFocusMode(null, null!);
                e.Handled = true;
            }
            // Ctrl+2 - Toggle Toolbar
            else if (e.Key == Avalonia.Input.Key.D2 && e.KeyModifiers == KeyModifiers.Control)
            {
                OnToggleToolbar(null, null!);
                e.Handled = true;
            }
            // Ctrl+3 - Toggle Sidebar
            else if (e.Key == Avalonia.Input.Key.D3 && e.KeyModifiers == KeyModifiers.Control)
            {
                OnToggleSidebar(null, null!);
                e.Handled = true;
            }
            // Ctrl+4 - Toggle Footer (Progress Bar)
            else if (e.Key == Avalonia.Input.Key.D4 && e.KeyModifiers == KeyModifiers.Control)
            {
                OnToggleFooter(null, null!);
                e.Handled = true;
            }
            // Ctrl+5 - Toggle Spell Check
            else if (e.Key == Avalonia.Input.Key.D5 && e.KeyModifiers == KeyModifiers.Control)
            {
                OnToggleSpellCheck(null, null!);
                e.Handled = true;
            }
            // Escape - Exit Super Focus Mode
            else if (e.Key == Avalonia.Input.Key.Escape && _isInSuperFocusMode)
            {
                OnToggleSuperFocusMode(null, null!);
                e.Handled = true;
            }
        };

        // Set default save location - each manuscript in its own folder
        var jetJotRoot = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "JetJot");

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

        // Try to load the last open manuscript from preferences
        string? existingManuscriptFolder = null;
        var startupPreferences = LoadPreferences();

        // First, check if we have a last open manuscript path in preferences
        if (!string.IsNullOrEmpty(startupPreferences.LastOpenManuscriptPath) &&
            System.IO.File.Exists(System.IO.Path.Combine(startupPreferences.LastOpenManuscriptPath, "manuscript.json")))
        {
            existingManuscriptFolder = startupPreferences.LastOpenManuscriptPath;
        }
        // Otherwise, find any existing manuscript folder
        else if (System.IO.Directory.Exists(jetJotRoot))
        {
            var subDirs = System.IO.Directory.GetDirectories(jetJotRoot);
            foreach (var dir in subDirs)
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(dir, "manuscript.json")))
                {
                    existingManuscriptFolder = dir;
                    break;
                }
            }
        }

        if (existingManuscriptFolder != null)
        {
            // Load existing manuscript
            var loadedManuscript = _storage.LoadManuscript(existingManuscriptFolder);
            _manuscript.Name = loadedManuscript.Name;
            _manuscript.FolderPath = loadedManuscript.FolderPath;
            _manuscript.LastOpenDocumentId = loadedManuscript.LastOpenDocumentId;

            // Clear current lists and populate with loaded content
            _manuscript.Documents.Clear();

            foreach (var doc in loadedManuscript.Documents)
            {
                _manuscript.Documents.Add(doc);
            }

            // Add to recents
            AddToRecents(_manuscript.FolderPath, _manuscript.Name);
        }
        else
        {
            // Create new manuscript with starter documents in its own folder
            var manuscriptFolder = System.IO.Path.Combine(jetJotRoot, "Untitled Manuscript");
            _manuscript.FolderPath = manuscriptFolder;
            var doc1 = new Document { Title = "First Document", Text = "" };
            var doc2 = new Document { Title = "Second Document", Text = "" };
            _manuscript.Documents.Add(doc1);
            _manuscript.Documents.Add(doc2);
        }

        // Populate sidebar
        DocumentList.ItemsSource = _manuscript.Documents;
        ManuscriptNameText.DataContext = _manuscript;

        // Select document (last open or first)
        Document? docToSelect = null;
        if (_manuscript.LastOpenDocumentId.HasValue)
        {
            docToSelect = _manuscript.Documents.FirstOrDefault(d => d.Id == _manuscript.LastOpenDocumentId.Value);
        }

        // If no last document, find first available
        if (docToSelect == null)
        {
            docToSelect = _manuscript.Documents.FirstOrDefault();
        }

        if (docToSelect != null)
        {
            _activeDocument = docToSelect;
            DocumentList.SelectedItem = docToSelect;
            Editor.Text = docToSelect.Text;

            // Ensure the selected document (including section documents) uses the accent highlight
            UpdateSelectedDocumentColor();
        }

        // Sidebar selection changes active document
        DocumentList.SelectionChanged += OnDocumentSelected;

        // Typing updates active document
        Editor.TextChanged += OnEditorTextChanged;
        Editor.PropertyChanged += OnEditorPropertyChanged;

        // Spell checker - initialize dictionaries and wire up context menu
        InitializeSpellChecker();

        // Use AddHandler with tunneling to intercept the event before TextBox handles it
        Editor.AddHandler(PointerPressedEvent, OnEditorPointerPressedTunnel, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        Editor.AddHandler(PointerReleasedEvent, OnEditorPointerReleasedTunnel, Avalonia.Interactivity.RoutingStrategies.Tunnel);

        // New document button
        NewDocumentButton.Click += OnNewDocumentClicked;

        // Logo easter egg - plane flies away on double click
        LogoImage.DoubleTapped += OnLogoDoubleTapped;

        // Font selectors
        FontFamilyComboBox.SelectionChanged += OnFontFamilyChanged;
        FontSizeComboBox.SelectionChanged += OnFontSizeChanged;

        // Find functionality
        FindTextBox.KeyDown += OnFindTextBoxKeyDown;
        FindNextButton.Click += OnFindNextClicked;
        FindPreviousButton.Click += OnFindPreviousClicked;
        ClearFindButton.Click += OnClearFindClicked;

        // Set goal button
        SetGoalButton.Click += OnSetGoalClicked;

        // Spell check indicator click
        SpellCheckIndicatorBar.Click += OnSpellCheckIndicatorClicked;

        // Update word count initially
        UpdateWordCount();

        // Update recents menu
        UpdateRecentsMenu();

        // Load and apply user preferences
        LoadAndApplyPreferences();

        // Set up margin indicator dragging
        SetupMarginIndicators();

        // Update right margin indicator when canvas size changes
        var marginCanvas = this.FindControl<Canvas>("MarginIndicatorCanvas");
        if (marginCanvas != null)
        {
            marginCanvas.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == nameof(Canvas.Bounds))
                {
                    ApplyMargins();
                }
            };
        }

        // Set initial focus to editor
        Editor.Focus();

        // Keep editor focused - refocus when clicking in editor area
        // Set up typewriter mode spacer
        var editorScrollViewer = this.FindControl<ScrollViewer>("EditorScrollViewer");
        if (editorScrollViewer != null)
        {
            editorScrollViewer.PointerPressed += (s, e) =>
            {
                // Only refocus if clicking in empty space (not on the TextBox itself)
                if (e.Source == editorScrollViewer || e.Source is Grid)
                {
                    Editor.Focus();
                }
            };
        }

        // Global keyboard shortcuts
        this.KeyDown += OnWindowKeyDown;

        if (editorScrollViewer != null)
        {
            editorScrollViewer.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == nameof(ScrollViewer.Viewport))
                {
                    UpdateTypewriterSpacer();
                }
            };
        }

        Editor.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(TextBox.Bounds))
            {
                UpdateTypewriterSpacer();
            }
        };

        // Save on window close
        this.Closing += OnWindowClosing;
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        // F3 - Find Next
        if (e.Key == Key.F3)
        {
            if (e.KeyModifiers == KeyModifiers.Shift)
            {
                FindPrevious();
            }
            else
            {
                FindNext();
            }
            e.Handled = true;
        }
        // Ctrl+F - Focus find box
        else if (e.Key == Key.F && e.KeyModifiers == KeyModifiers.Control)
        {
            FindTextBox.Focus();
            FindTextBox.SelectAll();
            e.Handled = true;
        }
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!string.IsNullOrEmpty(_manuscript.FolderPath))
        {
            _storage.SaveManuscript(_manuscript);
        }

        // Save user preferences on close to ensure accent color and other settings persist
        SavePreferences();
    }

    private async void OnNewManuscriptMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Save current manuscript
        if (!string.IsNullOrEmpty(_manuscript.FolderPath))
        {
            _storage.SaveManuscript(_manuscript);
        }

        // Prompt for new manuscript name
        var dialog = CreateStyledDialog("New Project", 400, 185);

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("35,*")
        };

        var titleBar = CreateDialogTitleBar("New Project", dialog);
        Grid.SetRow(titleBar, 0);
        grid.Children.Add(titleBar);

        var textBox = new TextBox
        {
            Text = "Untitled Manuscript",
            Width = 350,
            Margin = new Avalonia.Thickness(0, 0, 0, 20)
        };

        ApplyAccentToTextBox(textBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10
        };

        var okButton = CreateAccentButton("Create", 80);

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 80,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2A2A2A")),
            Foreground = Avalonia.Media.Brushes.White,
            BorderThickness = new Avalonia.Thickness(0),
            CornerRadius = new Avalonia.CornerRadius(4),
            FontSize = 14,
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };

        okButton.Click += (s, args) =>
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                LoadNewManuscript(textBox.Text);
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

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20, 15, 20, 20)
        };
        panel.Children.Add(textBox);
        panel.Children.Add(buttonPanel);

        Grid.SetRow(panel, 1);
        grid.Children.Add(panel);

        dialog.Content = grid;

        textBox.AttachedToVisualTree += (s, args) =>
        {
            textBox.Focus();
            textBox.SelectAll();
        };

        await dialog.ShowDialog(this);
    }

    private async void OnImportManuscriptClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Save current manuscript
        if (!string.IsNullOrEmpty(_manuscript.FolderPath))
        {
            _storage.SaveManuscript(_manuscript);
        }

        var storageProvider = this.StorageProvider;

        var jetJotRoot = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "JetJot");

        // Let user select from ANY location, but suggest JetJot folder
        var folders = await storageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            Title = "Select Project Folder",
            AllowMultiple = false,
            SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(new Uri(jetJotRoot))
        });

        if (folders.Count > 0)
        {
            var sourceFolderPath = folders[0].Path.LocalPath;

            // Check if it's a valid manuscript folder
            var manifestPath = System.IO.Path.Combine(sourceFolderPath, "manuscript.json");
            if (!System.IO.File.Exists(manifestPath))
            {
                await ShowErrorDialog("Invalid Project", "The selected folder is not a valid JetJot project.");
                return;
            }

            // Check if the selected folder is already in the JetJot directory
            var normalizedSource = System.IO.Path.GetFullPath(sourceFolderPath).TrimEnd(System.IO.Path.DirectorySeparatorChar);
            var normalizedJetJotRoot = System.IO.Path.GetFullPath(jetJotRoot).TrimEnd(System.IO.Path.DirectorySeparatorChar);

            if (normalizedSource.StartsWith(normalizedJetJotRoot, StringComparison.OrdinalIgnoreCase))
            {
                // Already in JetJot folder, just open it directly
                LoadManuscriptFromFolder(sourceFolderPath);
            }
            else
            {
                // Outside JetJot folder, need to import (copy)
                var manuscriptFolderName = System.IO.Path.GetFileName(sourceFolderPath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar));
                var destinationPath = System.IO.Path.Combine(jetJotRoot, manuscriptFolderName);

                // Check if already exists in JetJot folder
                if (System.IO.Directory.Exists(destinationPath))
                {
                    // Ask user if they want to overwrite
                    var result = await AskYesNoQuestion(
                        "Project Already Exists",
                        $"A project named '{manuscriptFolderName}' already exists in your JetJot folder. Do you want to overwrite it?");

                    if (!result)
                    {
                        return; // User cancelled
                    }

                    // Delete existing
                    System.IO.Directory.Delete(destinationPath, true);
                }

                // Copy the entire folder to JetJot directory
                CopyDirectory(sourceFolderPath, destinationPath);

                // Load the imported manuscript
                LoadManuscriptFromFolder(destinationPath);
            }
        }
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        // Create destination directory
        System.IO.Directory.CreateDirectory(destDir);

        // Copy all files
        foreach (var file in System.IO.Directory.GetFiles(sourceDir))
        {
            var fileName = System.IO.Path.GetFileName(file);
            var destFile = System.IO.Path.Combine(destDir, fileName);
            System.IO.File.Copy(file, destFile, true);
        }

        // Copy all subdirectories (if any)
        foreach (var subDir in System.IO.Directory.GetDirectories(sourceDir))
        {
            var dirName = System.IO.Path.GetFileName(subDir);
            var destSubDir = System.IO.Path.Combine(destDir, dirName);
            CopyDirectory(subDir, destSubDir);
        }
    }

    private async System.Threading.Tasks.Task<bool> AskYesNoQuestion(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 20
        };

        var messageText = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10
        };

        bool result = false;

        var yesBtn = new Button
        {
            Content = "Yes",
            Width = 80
        };
        yesBtn.Click += (s, args) =>
        {
            result = true;
            dialog.Close();
        };

        var noBtn = new Button
        {
            Content = "No",
            Width = 80
        };
        noBtn.Click += (s, args) =>
        {
            result = false;
            dialog.Close();
        };

        buttonPanel.Children.Add(yesBtn);
        buttonPanel.Children.Add(noBtn);

        panel.Children.Add(messageText);
        panel.Children.Add(buttonPanel);

        dialog.Content = panel;

        await dialog.ShowDialog(this);
        return result;
    }

    private void LoadNewManuscript(string name)
    {
        // Clear current manuscript
        _manuscript.Documents.Clear();
        _manuscript.Name = name;

        var jetJotRoot = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "JetJot");
        _manuscript.FolderPath = System.IO.Path.Combine(jetJotRoot, name);

        // Create starter documents
        var doc1 = new Document { Title = "First Document", Text = "" };
        var doc2 = new Document { Title = "Second Document", Text = "" };
        _manuscript.Documents.Add(doc1);
        _manuscript.Documents.Add(doc2);

        // Select first document
        _activeDocument = doc1;
        DocumentList.SelectedItem = doc1;
        Editor.Text = doc1.Text;

        // Save new manuscript
        _storage.SaveManuscript(_manuscript);

        // Debug: print manifest after creating new manuscript
        try
        {
            var manifestPath = System.IO.Path.Combine(_manuscript.FolderPath ?? string.Empty, "manuscript.json");
            if (System.IO.File.Exists(manifestPath))
            {
                Console.WriteLine("Manifest after creating new manuscript:");
                Console.WriteLine(System.IO.File.ReadAllText(manifestPath));
            }
            else
            {
                Console.WriteLine("Manifest not found after creating new manuscript");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading manifest after creating manuscript: {ex.Message}");
        }

        // Add to recents
        AddToRecents(_manuscript.FolderPath, _manuscript.Name);

        // Remember this manuscript as last open so it will be restored on restart
        SavePreferences();
    }

    private async void LoadManuscriptFromFolder(string folderPath)
    {
        try
        {
            // Save current manuscript before switching
            if (!string.IsNullOrEmpty(_manuscript.FolderPath))
            {
                SaveCurrentDocument();
            }

            // Check if folder exists
            if (!System.IO.Directory.Exists(folderPath))
            {
                await ShowErrorDialog("Project Not Found", $"The folder does not exist:\n{folderPath}");
                RemoveFromRecents(folderPath);
                return;
            }

            // Check if manifest exists
            var manifestPath = System.IO.Path.Combine(folderPath, "manuscript.json");
            if (!System.IO.File.Exists(manifestPath))
            {
                await ShowErrorDialog("Invalid Project", "The selected folder is not a valid JetJot project.");
                RemoveFromRecents(folderPath);
                return;
            }

            // Load the manuscript
            var loadedManuscript = _storage.LoadManuscript(folderPath);

            // Clear current
            _manuscript.Documents.Clear();

            // Update manuscript
            _manuscript.Name = loadedManuscript.Name;
            _manuscript.FolderPath = loadedManuscript.FolderPath;
            _manuscript.LastOpenDocumentId = loadedManuscript.LastOpenDocumentId;

            foreach (var doc in loadedManuscript.Documents)
            {
                _manuscript.Documents.Add(doc);
            }

            // Select document (last open or first)
            Document? docToSelect = null;
            if (_manuscript.LastOpenDocumentId.HasValue)
            {
                docToSelect = _manuscript.Documents.FirstOrDefault(d => d.Id == _manuscript.LastOpenDocumentId.Value);
            }

            // If no last document, find first available
            if (docToSelect == null)
            {
                docToSelect = _manuscript.Documents.FirstOrDefault();
            }

            if (docToSelect != null)
            {
                _activeDocument = docToSelect;
                DocumentList.SelectedItem = docToSelect;
                Editor.Text = docToSelect.Text;
            }
            else
            {
                _activeDocument = null;
                Editor.Text = string.Empty;
            }

            // Add to recents
            AddToRecents(_manuscript.FolderPath, _manuscript.Name);

            // Save preferences to remember this manuscript for next launch
            SavePreferences();
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Error Loading Project", $"An error occurred:\n{ex.Message}");
            RemoveFromRecents(folderPath);
        }
    }

    private async System.Threading.Tasks.Task ShowErrorDialog(string title, string message)
    {
        var errorDialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var errorPanel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 20
        };

        var errorText = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var okBtn = new Button
        {
            Content = "OK",
            Width = 80,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        okBtn.Click += (s, args) => errorDialog.Close();

        errorPanel.Children.Add(errorText);
        errorPanel.Children.Add(okBtn);

        errorDialog.Content = errorPanel;

        await errorDialog.ShowDialog(this);
    }

    private void RemoveFromRecents(string folderPath)
    {
        var recents = LoadRecents();
        var normalizedPath = System.IO.Path.GetFullPath(folderPath);
        recents.Recents.RemoveAll(r =>
            string.Equals(System.IO.Path.GetFullPath(r.FolderPath), normalizedPath, StringComparison.OrdinalIgnoreCase));
        SaveRecents(recents);
        UpdateRecentsMenu();
    }

    private void AddToRecents(string folderPath, string name)
    {
        var recents = LoadRecents();

        // Normalize path for comparison
        var normalizedPath = System.IO.Path.GetFullPath(folderPath);

        // Remove if already exists (case-insensitive comparison)
        recents.Recents.RemoveAll(r =>
            string.Equals(System.IO.Path.GetFullPath(r.FolderPath), normalizedPath, StringComparison.OrdinalIgnoreCase));

        // Add to front
        recents.Recents.Insert(0, new RecentManuscriptEntry
        {
            Name = name,
            FolderPath = folderPath,
            LastOpened = DateTime.Now
        });

        // Keep only last 5
        if (recents.Recents.Count > 5)
        {
            recents.Recents = recents.Recents.Take(5).ToList();
        }

        SaveRecents(recents);
        UpdateRecentsMenu();
    }

    private RecentManuscripts LoadRecents()
    {
        if (System.IO.File.Exists(_recentsFilePath))
        {
            var json = System.IO.File.ReadAllText(_recentsFilePath);
            return System.Text.Json.JsonSerializer.Deserialize<RecentManuscripts>(json) ?? new RecentManuscripts();
        }
        return new RecentManuscripts();
    }

    private void SaveRecents(RecentManuscripts recents)
    {
        // Final de-duplication step - group by normalized path and keep only the first (most recent)
        var uniqueRecents = recents.Recents
            .GroupBy(r => System.IO.Path.GetFullPath(r.FolderPath).ToLowerInvariant())
            .Select(g => g.First())
            .ToList();

        recents.Recents = uniqueRecents;

        var dir = System.IO.Path.GetDirectoryName(_recentsFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        var json = System.Text.Json.JsonSerializer.Serialize(recents, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(_recentsFilePath, json);
    }

    private void UpdateRecentsMenu()
    {
        var fileMenu = this.FindControl<MenuItem>("FileMenu");
        if (fileMenu == null) return;

        // Remove old recent items
        var itemsToRemove = fileMenu.Items.OfType<MenuItem>()
            .Where(m => m.Tag?.ToString() == "RecentItem" || m.Tag?.ToString() == "RecentHeader")
            .ToList();

        foreach (var item in itemsToRemove)
        {
            fileMenu.Items.Remove(item);
        }

        // Remove old separator
        var separatorToRemove = fileMenu.Items.OfType<Separator>()
            .FirstOrDefault(s => s.Tag?.ToString() == "RecentSeparator");
        if (separatorToRemove != null)
        {
            fileMenu.Items.Remove(separatorToRemove);
        }

        // Add new recents
        var recents = LoadRecents();
        if (recents.Recents.Count > 0)
        {
            // Find position (after "Open Manuscript...")
            int insertIndex = 2;

            // Add separator
            var separator = new Separator { Tag = "RecentSeparator" };
            fileMenu.Items.Insert(insertIndex, separator);
            insertIndex++;

            // Add "Recent Projects" header (non-clickable)
            var headerItem = new MenuItem
            {
                Header = "Recent Projects",
                Tag = "RecentHeader",
                IsEnabled = false,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(150, 150, 150))
            };
            fileMenu.Items.Insert(insertIndex, headerItem);
            insertIndex++;

            // Add recent items with context menu for removal
            foreach (var recent in recents.Recents)
            {
                // Check if this is the current manuscript
                var isCurrentManuscript = !string.IsNullOrEmpty(_manuscript.FolderPath) &&
                                         string.Equals(_manuscript.FolderPath, recent.FolderPath, StringComparison.OrdinalIgnoreCase);

                var menuItem = new MenuItem
                {
                    Header = isCurrentManuscript ? $"{recent.Name} (Current)" : recent.Name,
                    Tag = "RecentItem"
                };

                var recentPath = recent.FolderPath;
                menuItem.Click += (s, e) =>
                {
                    LoadManuscriptFromFolder(recentPath);
                    FileMenu.Close(); // Close the File menu dropdown
                };

                // Add context menu for right-click removal
                var contextMenu = new ContextMenu();
                var removeItem = new MenuItem { Header = "Remove from Recent Projects" };
                removeItem.Click += (s, e) =>
                {
                    RemoveFromRecents(recentPath);
                    e.Handled = true; // Prevent the main click event from firing
                };
                contextMenu.Items.Add(removeItem);
                menuItem.ContextMenu = contextMenu;

                fileMenu.Items.Insert(insertIndex, menuItem);
                insertIndex++;
            }
        }
    }

    private void OnToggleToolbar(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ToolbarPanel.IsVisible = !ToolbarPanel.IsVisible;
        CheckToolbar.IsChecked = ToolbarPanel.IsVisible;

        // If showing toolbar, exit focus modes
        if (ToolbarPanel.IsVisible)
        {
            if (CheckFocusMode.IsChecked == true)
            {
                CheckFocusMode.IsChecked = false;
            }
            if (_isInSuperFocusMode)
            {
                ExitSuperFocusMode();
            }
        }

        SavePreferences();
        Editor.Focus();
    }

    private void OnToggleSidebar(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        bool isVisible = !SidebarPanel.IsVisible;
        SidebarPanel.IsVisible = isVisible;
        CheckSidebar.IsChecked = isVisible;

        // Adjust column width: 0 when hidden, restore saved width when visible
        var mainGrid = this.FindControl<Grid>("MainGrid");
        if (mainGrid != null && mainGrid.ColumnDefinitions.Count > 0)
        {
            if (isVisible)
            {
                // Restore the saved width
                var prefs = LoadPreferences();
                mainGrid.ColumnDefinitions[0].Width = new GridLength(prefs.SidebarWidth);
                mainGrid.ColumnDefinitions[0].MinWidth = 150;
            }
            else
            {
                // Save the current width before hiding
                var currentWidth = mainGrid.ColumnDefinitions[0].Width.Value;
                if (currentWidth > 0)
                {
                    var prefs = LoadPreferences();
                    prefs.SidebarWidth = currentWidth;
                    SavePreferencesWithCustomDict(prefs);
                }
                mainGrid.ColumnDefinitions[0].Width = new GridLength(0);
                mainGrid.ColumnDefinitions[0].MinWidth = 0;
            }
        }

        // If showing sidebar, exit focus modes
        if (isVisible)
        {
            if (CheckFocusMode.IsChecked == true)
            {
                CheckFocusMode.IsChecked = false;
            }
            if (_isInSuperFocusMode)
            {
                ExitSuperFocusMode();
            }
        }

        UpdateTitleBarText();
        SavePreferences();
        Editor.Focus();
    }

    private void OnSidebarResizeCompleted(object? sender, Avalonia.Input.VectorEventArgs e)
    {
        SavePreferences();
    }

    private void UpdateTitleBarText()
    {
        var titleBar = this.FindControl<TextBlock>("TitleBarText");
        if (titleBar != null)
        {
            if (SidebarPanel.IsVisible)
            {
                titleBar.Text = "JetJot - Let Your Writing Fly";
            }
            else
            {
                titleBar.Text = $"JetJot - {_manuscript.Name}";
            }
        }
    }

    private void OnToggleFooter(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        FooterPanel.IsVisible = !FooterPanel.IsVisible;
        CheckFooter.IsChecked = FooterPanel.IsVisible;

        // If showing footer, exit focus modes
        if (FooterPanel.IsVisible)
        {
            if (CheckFocusMode.IsChecked == true)
            {
                CheckFocusMode.IsChecked = false;
            }
            if (_isInSuperFocusMode)
            {
                ExitSuperFocusMode();
            }
        }

        SavePreferences();
        Editor.Focus();
    }

    private void OnToggleSpellCheck(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _showSpellCheck = !_showSpellCheck;
        CheckSpellCheck.IsChecked = _showSpellCheck;

        // Update the spell check indicator
        UpdateSpellCheckIndicator();

        SavePreferences();
        Editor.Focus();
    }

    private void OnToggleThemedTitleBar(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        bool isThemed = !CheckThemedTitleBar.IsChecked ?? false;
        CheckThemedTitleBar.IsChecked = isThemed;

        // Reapply accent color to update title bar
        ApplyAccentColor();

        SavePreferences();
        Editor.Focus();
    }

    private void OnToggleThemedCursor(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        bool isThemed = !CheckThemedCursor.IsChecked ?? false;
        CheckThemedCursor.IsChecked = isThemed;

        // Update cursor color
        ApplyCursorColor();

        SavePreferences();
        Editor.Focus();
    }

    private void OnToggleFocusMode(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        bool isFocusMode = !CheckFocusMode.IsChecked ?? false;
        CheckFocusMode.IsChecked = isFocusMode;

        if (isFocusMode)
        {
            // Remember current state before hiding
            _preFocusModeToolbarVisible = ToolbarPanel.IsVisible;
            _preFocusModeSidebarVisible = SidebarPanel.IsVisible;
            _preFocusModeFooterVisible = FooterPanel.IsVisible;

            // Hide all UI elements
            ToolbarPanel.IsVisible = false;
            FooterPanel.IsVisible = false;
            SidebarPanel.IsVisible = false;

            // Adjust grid to hide sidebar column
            var mainGrid = this.FindControl<Grid>("MainGrid");
            if (mainGrid != null && mainGrid.ColumnDefinitions.Count > 0)
            {
                mainGrid.ColumnDefinitions[0].Width = new GridLength(0);
                mainGrid.ColumnDefinitions[0].MinWidth = 0;
            }

            // Update checkboxes
            CheckToolbar.IsChecked = false;
            CheckSidebar.IsChecked = false;
            CheckFooter.IsChecked = false;
        }
        else
        {
            // Restore previous state
            ToolbarPanel.IsVisible = _preFocusModeToolbarVisible;
            FooterPanel.IsVisible = _preFocusModeFooterVisible;
            SidebarPanel.IsVisible = _preFocusModeSidebarVisible;

            // Restore sidebar column based on previous visibility
            var mainGrid = this.FindControl<Grid>("MainGrid");
            if (mainGrid != null && mainGrid.ColumnDefinitions.Count > 0)
            {
                if (_preFocusModeSidebarVisible)
                {
                    var prefs = LoadPreferences();
                    mainGrid.ColumnDefinitions[0].Width = new GridLength(prefs.SidebarWidth);
                    mainGrid.ColumnDefinitions[0].MinWidth = 150;
                }
                else
                {
                    mainGrid.ColumnDefinitions[0].Width = new GridLength(0);
                    mainGrid.ColumnDefinitions[0].MinWidth = 0;
                }
            }

            // Update checkboxes to match restored state
            CheckToolbar.IsChecked = _preFocusModeToolbarVisible;
            CheckSidebar.IsChecked = _preFocusModeSidebarVisible;
            CheckFooter.IsChecked = _preFocusModeFooterVisible;
        }

        UpdateTitleBarText();
        SavePreferences();
        Editor.Focus();
    }

    private async void OnToggleSuperFocusMode(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _isInSuperFocusMode = !_isInSuperFocusMode;
        CheckSuperFocusMode.IsChecked = _isInSuperFocusMode;

        if (_isInSuperFocusMode)
        {
            // Remember current state before hiding everything
            _preSuperFocusModeToolbarVisible = ToolbarPanel.IsVisible;
            _preSuperFocusModeSidebarVisible = SidebarPanel.IsVisible;
            _preSuperFocusModeFooterVisible = FooterPanel.IsVisible;
            _preSuperFocusModeTitleBarVisible = TitleBarPanel.IsVisible;

            // Enter fullscreen mode
            this.WindowState = Avalonia.Controls.WindowState.FullScreen;

            // Hide ALL UI elements including title bar
            ToolbarPanel.IsVisible = false;
            FooterPanel.IsVisible = false;
            SidebarPanel.IsVisible = false;
            TitleBarPanel.IsVisible = false;

            // Collapse top and bottom rows of outer grid
            var outerGrid = this.FindControl<Grid>("MainOuterGrid");
            if (outerGrid != null && outerGrid.RowDefinitions.Count >= 3)
            {
                outerGrid.RowDefinitions[0].Height = new GridLength(0); // Top row
                outerGrid.RowDefinitions[2].Height = new GridLength(0); // Bottom row
            }

            // Collapse footer row in inner grid and hide sidebar column
            var mainGrid = this.FindControl<Grid>("MainGrid");
            if (mainGrid != null)
            {
                if (mainGrid.ColumnDefinitions.Count > 0)
                {
                    mainGrid.ColumnDefinitions[0].Width = new GridLength(0);
                    mainGrid.ColumnDefinitions[0].MinWidth = 0;
                }
                if (mainGrid.RowDefinitions.Count >= 2)
                {
                    mainGrid.RowDefinitions[1].Height = new GridLength(0); // Footer row
                }
            }

            // Remove ALL padding for true fullscreen
            var editorArea = this.FindControl<Border>("EditorAreaBorder");
            if (editorArea != null)
            {
                editorArea.Padding = new Thickness(0);
            }

            // Remove corner radius for sharp edges in fullscreen
            var editorInner = this.FindControl<Border>("EditorInnerBorder");
            if (editorInner != null)
            {
                editorInner.CornerRadius = new CornerRadius(0);
            }

            // Update checkboxes
            CheckToolbar.IsChecked = false;
            CheckSidebar.IsChecked = false;
            CheckFooter.IsChecked = false;

            // Make sure focus mode is off
            CheckFocusMode.IsChecked = false;

            // Show the exit hint overlay
            await ShowSuperFocusHintAsync();
        }
        else
        {
            ExitSuperFocusMode();
        }

        SavePreferences();
        Editor.Focus();
    }

    private void ExitSuperFocusMode()
    {
        _isInSuperFocusMode = false;
        CheckSuperFocusMode.IsChecked = false;

        // Exit fullscreen mode
        WindowState = WindowState.Normal;

        // Restore top and bottom rows of main grid
        var outerGrid = this.FindControl<Grid>("MainOuterGrid");
        if (outerGrid != null && outerGrid.RowDefinitions.Count >= 3)
        {
            outerGrid.RowDefinitions[0].Height = new GridLength(40); // Top row (title bar)
            outerGrid.RowDefinitions[2].Height = GridLength.Auto; // Bottom row (footer) - Auto so it collapses when hidden
        }

        // Restore previous state
        ToolbarPanel.IsVisible = _preSuperFocusModeToolbarVisible;
        FooterPanel.IsVisible = _preSuperFocusModeFooterVisible;
        SidebarPanel.IsVisible = _preSuperFocusModeSidebarVisible;
        TitleBarPanel.IsVisible = _preSuperFocusModeTitleBarVisible;

        // Restore sidebar column and footer row in inner grid
        var mainGrid = this.FindControl<Grid>("MainGrid");
        if (mainGrid != null)
        {
            if (mainGrid.ColumnDefinitions.Count > 0)
            {
                if (_preSuperFocusModeSidebarVisible)
                {
                    var prefs = LoadPreferences();
                    mainGrid.ColumnDefinitions[0].Width = new GridLength(prefs.SidebarWidth);
                    mainGrid.ColumnDefinitions[0].MinWidth = 150;
                }
                else
                {
                    mainGrid.ColumnDefinitions[0].Width = new GridLength(0);
                    mainGrid.ColumnDefinitions[0].MinWidth = 0;
                }
            }
            if (mainGrid.RowDefinitions.Count >= 2)
            {
                mainGrid.RowDefinitions[1].Height = GridLength.Auto; // Footer row - Auto so it collapses when hidden
            }
        }

        // Restore normal padding
        var editorArea = this.FindControl<Border>("EditorAreaBorder");
        if (editorArea != null)
        {
            editorArea.Padding = new Thickness(0);
        }

        // Restore corner radius
        var editorInner = this.FindControl<Border>("EditorInnerBorder");
        if (editorInner != null)
        {
            editorInner.CornerRadius = new CornerRadius(0);
        }

        // Update checkboxes to match restored state
        CheckToolbar.IsChecked = _preSuperFocusModeToolbarVisible;
        CheckSidebar.IsChecked = _preSuperFocusModeSidebarVisible;
        CheckFooter.IsChecked = _preSuperFocusModeFooterVisible;

        UpdateTitleBarText();
    }

    private async System.Threading.Tasks.Task ShowSuperFocusHintAsync()
    {
        var overlay = this.FindControl<Border>("SuperFocusHintOverlay");
        if (overlay == null) return;

        // Show overlay with full opacity
        overlay.IsVisible = true;
        overlay.Opacity = 1.0;

        // Wait 2.5 seconds
        await System.Threading.Tasks.Task.Delay(2500);

        // Fade out over 0.5 seconds (50ms per step, 20 steps)
        for (int i = 20; i >= 0; i--)
        {
            overlay.Opacity = i / 20.0;
            await System.Threading.Tasks.Task.Delay(25);
        }

        overlay.IsVisible = false;
    }

    private void OnToggleTypewriterMode(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _isTypewriterMode = !_isTypewriterMode;
        CheckTypewriterMode.IsChecked = _isTypewriterMode;

        // Update spacer height
        UpdateTypewriterSpacer();

        if (_isTypewriterMode)
        {
            // Center the current line
            CenterCurrentLine();
        }

        SavePreferences();
        Editor.Focus();
    }

    private void UpdateTypewriterSpacer()
    {
        var topSpacer = this.FindControl<Border>("TypewriterTopSpacer");
        var bottomSpacer = this.FindControl<Border>("TypewriterBottomSpacer");
        var scrollViewer = this.FindControl<ScrollViewer>("EditorScrollViewer");

        if (topSpacer == null || bottomSpacer == null || scrollViewer == null) return;

        // When typewriter mode is on, add top and bottom padding equal to half the viewport height
        // This allows scrolling past both start and end so any line can be centered
        if (_isTypewriterMode && scrollViewer.Viewport.Height > 0)
        {
            var spacerHeight = scrollViewer.Viewport.Height / 2;
            topSpacer.Height = spacerHeight;
            bottomSpacer.Height = spacerHeight;
        }
        else
        {
            topSpacer.Height = 0;
            bottomSpacer.Height = 0;
        }
    }

    private void CenterCurrentLine()
    {
        if (!_isTypewriterMode) return;

        var scrollViewer = this.FindControl<ScrollViewer>("EditorScrollViewer");
        var topSpacer = this.FindControl<Border>("TypewriterTopSpacer");

        if (scrollViewer == null || topSpacer == null) return;

        // Calculate the line height (approximate)
        var lineHeight = Editor.FontSize * 1.35; // Typical line height multiplier

        // Calculate which line the caret is on
        var text = Editor.Text ?? string.Empty;
        var caretIndex = Editor.CaretIndex;

        var linesBeforeCaret = text.Substring(0, Math.Min(caretIndex, text.Length))
            .Count(c => c == '\n');

        // Calculate the Y position of the current line
        // This includes the top spacer height since the TextBox is in Grid.Row="1"
        var currentLineY = topSpacer.Height + (linesBeforeCaret * lineHeight);

        // Calculate the center position (middle of the viewport)
        var viewportHeight = scrollViewer.Viewport.Height;
        var targetOffset = currentLineY - (viewportHeight / 2) + (lineHeight / 2);

        // The extent height now includes both top and bottom spacers
        var maxOffset = Math.Max(0, scrollViewer.Extent.Height - viewportHeight);

        // Scroll to center the line
        var clampedOffset = Math.Clamp(targetOffset, 0, maxOffset);
        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, clampedOffset);
    }

    private UserPreferences LoadPreferences()
    {
        if (System.IO.File.Exists(_preferencesFilePath))
        {
            var json = System.IO.File.ReadAllText(_preferencesFilePath);
            return System.Text.Json.JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
        }
        return new UserPreferences();
    }

    private void SavePreferences()
    {
        var sidebarColumn = this.FindControl<Grid>("MainGrid")?.ColumnDefinitions[0];
        var currentSidebarWidth = sidebarColumn?.Width.Value ?? 240;

        // Only save sidebar width if it's greater than 0 (i.e., sidebar is visible and has a real width)
        // Otherwise, preserve the previously saved width from preferences
        var prefs = LoadPreferences();
        var sidebarWidthToSave = currentSidebarWidth > 0 ? currentSidebarWidth : prefs.SidebarWidth;

        var preferences = new UserPreferences
        {
            ShowToolbar = ToolbarPanel.IsVisible,
            ShowSidebar = SidebarPanel.IsVisible,
            ShowFooter = FooterPanel.IsVisible,
            ShowSpellCheck = _showSpellCheck,
            TypewriterMode = _isTypewriterMode,
            FontFamily = _currentFontFamily,
            FontSize = (int)Editor.FontSize,
            AccentColor = _accentColor,
            ThemedTitleBar = CheckThemedTitleBar.IsChecked ?? true,
            ThemedCursor = CheckThemedCursor.IsChecked ?? true,
            SidebarWidth = sidebarWidthToSave,
            LeftMargin = _leftMargin,
            RightMargin = _rightMargin,
            CustomDictionary = _spellChecker.GetCustomDictionary(),
            LastOpenManuscriptPath = _manuscript.FolderPath
        };

        var dir = System.IO.Path.GetDirectoryName(_preferencesFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        var json = System.Text.Json.JsonSerializer.Serialize(preferences, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(_preferencesFilePath, json);
    }

    private async void SaveCurrentDocument()
    {
        // Save the manuscript
        _storage.SaveManuscript(_manuscript);

        // Show save feedback
        await ShowSaveFeedback();
    }

    private async System.Threading.Tasks.Task ShowSaveFeedback()
    {
        var feedbackText = this.FindControl<TextBlock>("SaveFeedbackText");
        if (feedbackText != null)
        {
            feedbackText.IsVisible = true;
            feedbackText.Opacity = 1.0;

            // Wait 1.5 seconds
            await System.Threading.Tasks.Task.Delay(1500);

            // Fade out gradually
            for (int i = 10; i >= 0; i--)
            {
                feedbackText.Opacity = i / 10.0;
                await System.Threading.Tasks.Task.Delay(30);
            }

            feedbackText.IsVisible = false;
        }
    }

    private void LoadAndApplyPreferences()
    {
        _isLoadingPreferences = true;

        var preferences = LoadPreferences();

        // Apply toolbar preference
        ToolbarPanel.IsVisible = preferences.ShowToolbar;
        CheckToolbar.IsChecked = preferences.ShowToolbar;

        // Apply sidebar preference
        SidebarPanel.IsVisible = preferences.ShowSidebar;
        CheckSidebar.IsChecked = preferences.ShowSidebar;
        var mainGrid = this.FindControl<Grid>("MainGrid");
        if (mainGrid != null && mainGrid.ColumnDefinitions.Count > 0)
        {
            var width = preferences.ShowSidebar ? preferences.SidebarWidth : 0;
            mainGrid.ColumnDefinitions[0].Width = new GridLength(width);
        }

        // Apply footer preference
        FooterPanel.IsVisible = preferences.ShowFooter;
        CheckFooter.IsChecked = preferences.ShowFooter;

        // Apply spell check preference
        _showSpellCheck = preferences.ShowSpellCheck;
        CheckSpellCheck.IsChecked = preferences.ShowSpellCheck;

        // Apply typewriter mode preference
        _isTypewriterMode = preferences.TypewriterMode;
        CheckTypewriterMode.IsChecked = preferences.TypewriterMode;

        if (_isTypewriterMode)
        {
            CenterCurrentLine();
        }

        // Apply font preferences
        _currentFontFamily = preferences.FontFamily;
        Editor.FontFamily = new Avalonia.Media.FontFamily(preferences.FontFamily);
        Editor.FontSize = preferences.FontSize;

        // Update combo boxes to match
        SetFontFamilyComboBox(preferences.FontFamily);
        SetFontSizeComboBox(preferences.FontSize);

        // Apply dark mode (always on)
        ApplyThemeMode(true);

        // Apply themed title bar preference
        CheckThemedTitleBar.IsChecked = preferences.ThemedTitleBar;

        // Apply themed cursor preference
        CheckThemedCursor.IsChecked = preferences.ThemedCursor;

        // Apply accent color preference
        _accentColor = preferences.AccentColor;
        ApplyAccentColor();

        // Apply margin preferences
        _leftMargin = preferences.LeftMargin;
        _rightMargin = preferences.RightMargin;
        ApplyMargins();

        _isLoadingPreferences = false;
    }

    private void ApplyAccentColor()
    {
        var accentBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(_accentColor));

        // Update the dynamic resources
        this.Resources["AccentBrush"] = accentBrush;

        // 1. Title Bar - only apply theme if preference is enabled
        if (this.FindControl<Border>("TitleBarPanel") is Border titleBar)
        {
            bool themedTitleBar = CheckThemedTitleBar?.IsChecked ?? true;
            if (themedTitleBar)
            {
                titleBar.Background = accentBrush;
            }
            else
            {
                // Use default dark gray when themed title bar is disabled
                titleBar.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2A2A2A"));
            }
        }

        // 2. New Document Button
        if (this.FindControl<Button>("NewDocumentButton") is Button newDocBtn)
        {
            newDocBtn.Background = accentBrush;
        }

        // 3. Progress Bar - use white for Editor's Ebony (no theme)
        if (this.FindControl<ProgressBar>("WordProgressBar") is ProgressBar progressBar)
        {
            if (_accentColor.Equals("#3A3A3A", StringComparison.OrdinalIgnoreCase))
            {
                // Use white for Editor's Ebony theme (less distracting)
                progressBar.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
            }
            else
            {
                progressBar.Foreground = accentBrush;
            }
        }

        // 4. Toolbar bottom border (horizontal accent)
        if (this.FindControl<Border>("ToolbarPanel") is Border toolbar)
        {
            toolbar.BorderBrush = accentBrush;
        }

        // 5. Sidebar right border (vertical accent)
        if (this.FindControl<Border>("SidebarPanel") is Border sidebar)
        {
            sidebar.BorderBrush = accentBrush;
        }

        // 6. Manuscript divider line (themed accent line)
        if (this.FindControl<Border>("ManuscriptDivider") is Border divider)
        {
            divider.Background = accentBrush;
        }

        // 7. Accent Color Button in toolbar
        if (this.FindControl<Button>("AccentColorButton") is Button accentButton)
        {
            accentButton.Background = accentBrush;
        }

        // 8. Word count text - make it visible for dark themes
        if (this.FindControl<TextBlock>("WordCountText") is TextBlock wordCountText)
        {
            if (_accentColor.Equals("#3A3A3A", StringComparison.OrdinalIgnoreCase))
            {
                wordCountText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#CCCCCC"));
            }
            else
            {
                wordCountText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#CCCCCC"));
            }
        }

        // 9. Update selected document background
        UpdateSelectedDocumentColor();

        // 10. Apply cursor color based on preference
        ApplyCursorColor();
    }

    private void ApplyCursorColor()
    {
        bool themedCursor = CheckThemedCursor?.IsChecked ?? true;

        Avalonia.Media.SolidColorBrush caretBrush;

        if (themedCursor)
        {
            // Use accent color for cursor
            var accentBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(_accentColor));

            // For very dark accent colors, use a lighter color for better visibility
            if (_accentColor.Equals("#3A3A3A", StringComparison.OrdinalIgnoreCase))
            {
                caretBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#CCCCCC"));
            }
            else
            {
                caretBrush = accentBrush;
            }
        }
        else
        {
            // Use default white cursor when themed cursor is disabled
            caretBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
        }

        this.Resources["CaretBrush"] = caretBrush;
    }

    private void ApplyThemeMode(bool isDarkMode)
    {
        // Define color schemes
        var bgColor = isDarkMode ? "#1E1E1E" : "#FFFFFF";
        var textColor = isDarkMode ? "#CCCCCC" : "#1E1E1E";
        var editorBgColor = isDarkMode ? "#1A1A1A" : "#FFFFFF";
        var sidebarBgColor = isDarkMode ? "#2A2A2A" : "#F0F0F0";
        var menuBgColor = isDarkMode ? "#2A2A2A" : "#F8F8F8";
        var borderColor = isDarkMode ? "#3A3A3A" : "#D0D0D0";
        var menuTextColor = isDarkMode ? "#FFFFFF" : "#1E1E1E";

        var bgBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(bgColor));
        var textBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(textColor));
        var editorBgBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(editorBgColor));
        var sidebarBgBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(sidebarBgColor));
        var menuBgBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(menuBgColor));
        var borderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(borderColor));
        var menuTextBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(menuTextColor));

        // Main window background
        this.Background = bgBrush;

        // Editor
        if (Editor != null)
        {
            Editor.Background = editorBgBrush;
            Editor.Foreground = textBrush;
        }

        // Editor inner border (main editor background area)
        if (this.FindControl<Border>("EditorInnerBorder") is Border editorInner)
        {
            editorInner.Background = editorBgBrush;
        }

        // Sidebar
        if (this.FindControl<Border>("SidebarPanel") is Border sidebar)
        {
            sidebar.Background = sidebarBgBrush;
        }

        // Document list
        if (this.FindControl<ListBox>("DocumentList") is ListBox docList)
        {
            docList.Background = sidebarBgBrush;
            docList.Foreground = textBrush;
        }

        // Manuscript name text
        if (this.FindControl<TextBlock>("ManuscriptNameText") is TextBlock nameText)
        {
            nameText.Foreground = isDarkMode ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF")) : textBrush;
        }

        // Menu bar
        if (this.FindControl<Menu>("MenuBar") is Menu menuBar)
        {
            menuBar.Background = menuBgBrush;
            menuBar.Foreground = menuTextBrush;
        }

        // Toolbar
        if (this.FindControl<Border>("ToolbarPanel") is Border toolbar)
        {
            toolbar.Background = menuBgBrush;
        }

        // Footer
        if (this.FindControl<Border>("FooterPanel") is Border footer)
        {
            footer.Background = menuBgBrush;
        }

        // Word count text
        if (this.FindControl<TextBlock>("WordCountText") is TextBlock wordCount)
        {
            wordCount.Foreground = textBrush;
        }

        // Reapply accent color to ensure it works with the new theme
        ApplyAccentColor();
    }


    private void UpdateSelectedDocumentColor()
    {
        var listBox = this.FindControl<ListBox>("DocumentList");

        var accentBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(_accentColor));
        var transparentBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Transparent);

        // Clear all document backgrounds
        if (listBox != null)
        {
            for (int i = 0; i < _manuscript.Documents.Count; i++)
            {
                var container = listBox.ContainerFromIndex(i);
                if (container is Control control)
                {
                    var itemBorder = FindItemBorder(control);
                    if (itemBorder != null)
                    {
                        itemBorder.Background = transparentBrush;
                    }
                }
            }
        }

        // Set the background for the active document
        if (_activeDocument != null && listBox != null)
        {
            var index = _manuscript.Documents.IndexOf(_activeDocument);
            if (index >= 0)
            {
                var container = listBox.ContainerFromIndex(index);
                if (container is Control control)
                {
                    var itemBorder = FindItemBorder(control);
                    if (itemBorder != null)
                    {
                        itemBorder.Background = accentBrush;
                    }
                }
            }
        }
    }

    private void SetupMarginIndicators()
    {
        var leftIndicator = this.FindControl<Border>("LeftMarginIndicator");
        var rightIndicator = this.FindControl<Border>("RightMarginIndicator");
        var canvas = this.FindControl<Canvas>("MarginIndicatorCanvas");

        if (leftIndicator == null || rightIndicator == null || canvas == null) return;

        // Left margin indicator dragging
        leftIndicator.PointerPressed += (s, e) =>
        {
            _isDraggingLeftMargin = true;
            e.Pointer.Capture(leftIndicator);
            e.Handled = true;
        };

        leftIndicator.PointerMoved += (s, e) =>
        {
            if (_isDraggingLeftMargin)
            {
                var pos = e.GetPosition(canvas);
                _leftMargin = Math.Max(10, Math.Min(200, pos.X));
                _rightMargin = _leftMargin; // Mirror the margin
                ApplyMargins();
                e.Handled = true;
            }
        };

        leftIndicator.PointerReleased += (s, e) =>
        {
            if (_isDraggingLeftMargin)
            {
                _isDraggingLeftMargin = false;
                e.Pointer.Capture(null);
                SavePreferences();
                e.Handled = true;
            }
        };

        // Right margin indicator dragging
        rightIndicator.PointerPressed += (s, e) =>
        {
            _isDraggingRightMargin = true;
            e.Pointer.Capture(rightIndicator);
            e.Handled = true;
        };

        rightIndicator.PointerMoved += (s, e) =>
        {
            if (_isDraggingRightMargin)
            {
                var pos = e.GetPosition(canvas);
                var canvasWidth = canvas.Bounds.Width;
                _rightMargin = Math.Max(10, Math.Min(200, canvasWidth - pos.X));
                _leftMargin = _rightMargin; // Mirror the margin
                ApplyMargins();
                e.Handled = true;
            }
        };

        rightIndicator.PointerReleased += (s, e) =>
        {
            if (_isDraggingRightMargin)
            {
                _isDraggingRightMargin = false;
                e.Pointer.Capture(null);
                SavePreferences();
                e.Handled = true;
            }
        };
    }

    private void ApplyMargins()
    {
        var editorBorder = this.FindControl<Border>("EditorBorder");
        var leftIndicator = this.FindControl<Border>("LeftMarginIndicator");
        var rightIndicator = this.FindControl<Border>("RightMarginIndicator");
        var canvas = this.FindControl<Canvas>("MarginIndicatorCanvas");

        if (editorBorder != null)
        {
            editorBorder.Padding = new Avalonia.Thickness(_leftMargin, 20, _rightMargin, 20);
        }

        // The canvas is now outside the padding, so it spans the full width.
        // Position indicators at the margin positions
        if (leftIndicator != null)
        {
            Canvas.SetLeft(leftIndicator, _leftMargin);
        }

        if (rightIndicator != null && canvas != null)
        {
            var canvasWidth = canvas.Bounds.Width;
            if (canvasWidth > 0)
            {
                Canvas.SetLeft(rightIndicator, canvasWidth - _rightMargin - 2); // Subtract margin and indicator width
            }
        }
    }

    private Window CreateStyledDialog(string title, double width, double height)
    {
        var accentColor = Avalonia.Media.Color.Parse(_accentColor);
        var accentBrush = new Avalonia.Media.SolidColorBrush(accentColor);

        var dialog = new Window
        {
            Title = title,
            Width = width,
            Height = height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ExtendClientAreaToDecorationsHint = true,
            ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome,
            ExtendClientAreaTitleBarHeightHint = 35,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2A2A2A"))
        };

        // Ensure Fluent theme accent (focus rings, etc.) matches the user's accent color.
        dialog.Resources["AccentBrush"] = accentBrush;
        dialog.Resources["CaretBrush"] = accentBrush;
        dialog.Resources["SystemAccentColor"] = accentColor;
        dialog.Resources["SystemAccentColorLight1"] = accentColor;
        dialog.Resources["SystemAccentColorLight2"] = accentColor;
        dialog.Resources["SystemAccentColorLight3"] = accentColor;
        dialog.Resources["SystemAccentColorDark1"] = accentColor;
        dialog.Resources["SystemAccentColorDark2"] = accentColor;
        dialog.Resources["SystemAccentColorDark3"] = accentColor;

        return dialog;
    }

    private void ApplyAccentToTextBox(TextBox textBox)
    {
        var accentBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(_accentColor));

        // Set selection color
        textBox.SelectionBrush = accentBrush;
        textBox.SetValue(Avalonia.Controls.Primitives.TemplatedControl.FocusAdornerProperty, null);
        textBox.Classes.Add("dialog-textbox");

        // Set focus border color when template is applied
        textBox.TemplateApplied += (s, e) =>
        {
            if (e.NameScope.Find<Border>("PART_BorderElement") is Border border)
            {
                // Store original brush
                var originalBrush = border.BorderBrush;

                // Apply accent color immediately if already focused (for auto-focused dialogs)
                if (textBox.IsFocused)
                {
                    border.BorderBrush = accentBrush;
                }

                textBox.GotFocus += (sender, args) =>
                {
                    border.BorderBrush = accentBrush;
                };

                textBox.LostFocus += (sender, args) =>
                {
                    border.BorderBrush = originalBrush;
                };
            }
        };
    }

    private Border CreateDialogTitleBar(string title, Window dialog)
    {
        var titleBar = new Border
        {
            Height = 35,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(_accentColor))
        };

        var titleGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };

        var titleText = new TextBlock
        {
            Text = title,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF")),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(15, 0, 0, 0),
            FontSize = 13
        };

        var closeButton = new Button
        {
            Content = "✕",
            Width = 35,
            Height = 35,
            Background = Avalonia.Media.Brushes.Transparent,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF")),
            BorderThickness = new Avalonia.Thickness(0),
            FontSize = 12,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        closeButton.Click += (s, e) => dialog.Close();

        Grid.SetColumn(titleText, 0);
        Grid.SetColumn(closeButton, 1);

        titleGrid.Children.Add(titleText);
        titleGrid.Children.Add(closeButton);

        titleBar.Child = titleGrid;

        // Make title bar draggable
        titleBar.PointerPressed += (sender, e) =>
        {
            if (e.GetCurrentPoint(dialog).Properties.IsLeftButtonPressed)
            {
                dialog.BeginMoveDrag(e);
            }
        };

        return titleBar;
    }

    private Button CreateAccentButton(string content, double width = 80)
    {
        return new Button
        {
            Content = content,
            Width = width,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(_accentColor)),
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF")),
            BorderThickness = new Avalonia.Thickness(0),
            CornerRadius = new CornerRadius(4),
            Padding = new Avalonia.Thickness(12, 6)
        };
    }

    private (Border border, TextBox textBox) CreateDialogTextBox(Avalonia.Media.SolidColorBrush accentBrush)
    {
        var border = new Border
        {
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2A2A2A")),
            BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3A3A3A")),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new CornerRadius(4),
            Width = 360
        };

        var textBox = new TextBox
        {
            Background = Avalonia.Media.Brushes.Transparent,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF")),
            BorderThickness = new Avalonia.Thickness(0),
            SelectionBrush = accentBrush,
            Padding = new Avalonia.Thickness(8)
        };

        // Set the focus adorner to null to disable purple outline
        textBox.SetValue(Avalonia.Controls.Primitives.TemplatedControl.FocusAdornerProperty, null);
        textBox.Classes.Add("dialog-textbox");

        // Force the inner template border to stay neutral (avoid Fluent purple focus ring).
        textBox.TemplateApplied += (s, e) =>
        {
            if (e.NameScope.Find<Border>("PART_BorderElement") is Border innerBorder)
            {
                innerBorder.BorderBrush = Avalonia.Media.Brushes.Transparent;
                innerBorder.BorderThickness = new Avalonia.Thickness(0);
            }
        };

        // Change border color on focus
        textBox.GotFocus += (s, e) => border.BorderBrush = accentBrush;
        textBox.LostFocus += (s, e) => border.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3A3A3A"));

        border.Child = textBox;

        return (border, textBox);
    }

    private void SetFontFamilyComboBox(string fontFamily)
    {
        for (int i = 0; i < FontFamilyComboBox.Items.Count; i++)
        {
            if (FontFamilyComboBox.Items[i] is ComboBoxItem item &&
                item.Content is string content &&
                content == fontFamily)
            {
                FontFamilyComboBox.SelectedIndex = i;
                return;
            }
        }
        // Default to first item if not found
        FontFamilyComboBox.SelectedIndex = 0;
    }

    private void SetFontSizeComboBox(int fontSize)
    {
        for (int i = 0; i < FontSizeComboBox.Items.Count; i++)
        {
            if (FontSizeComboBox.Items[i] is ComboBoxItem item &&
                item.Content is string content &&
                int.TryParse(content, out int size) &&
                size == fontSize)
            {
                FontSizeComboBox.SelectedIndex = i;
                return;
            }
        }
        // Default to 16pt (index 2) if not found
        FontSizeComboBox.SelectedIndex = 2;
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

    private async void OnExportTxtClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ExportProject("txt", GenerateTxtExport());
    }

    private async void OnExportMdClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ExportProject("md", GenerateMarkdownExport());
    }

    private async void OnExportHtmlClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ExportProject("html", GenerateHtmlExport());
    }

    private async System.Threading.Tasks.Task ExportProject(string extension, string content)
    {
        try
        {
            var storageProvider = StorageProvider;
            var suggestedFileName = $"{_manuscript.Name}.{extension}";

            // Get Documents folder path
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            var file = await storageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Export Project",
                SuggestedFileName = suggestedFileName,
                DefaultExtension = extension,
                ShowOverwritePrompt = true,
                SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(new Uri(documentsPath))
            });

            if (file == null)
                return; // User cancelled

            // Write to file
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new System.IO.StreamWriter(stream);
            await writer.WriteAsync(content);
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Export Failed", $"An error occurred while exporting:\n{ex.Message}");
        }
    }

    private string GenerateTxtExport()
    {
        var content = new System.Text.StringBuilder();

        foreach (var doc in _manuscript.Documents)
        {
            content.AppendLine($"========== {doc.Title} ==========");
            content.AppendLine();
            content.AppendLine(doc.Text);
            content.AppendLine();
            content.AppendLine();
        }

        return content.ToString();
    }

    private string GenerateMarkdownExport()
    {
        var content = new System.Text.StringBuilder();

        foreach (var doc in _manuscript.Documents)
        {
            content.AppendLine($"# {doc.Title}");
            content.AppendLine();
            content.AppendLine(doc.Text);
            content.AppendLine();
            content.AppendLine("---");
            content.AppendLine();
        }

        return content.ToString();
    }

    private string GenerateHtmlExport()
    {
        var content = new System.Text.StringBuilder();

        content.AppendLine("<!DOCTYPE html>");
        content.AppendLine("<html>");
        content.AppendLine("<head>");
        content.AppendLine($"    <meta charset=\"UTF-8\">");
        content.AppendLine($"    <title>{_manuscript.Name}</title>");
        content.AppendLine("    <style>");
        content.AppendLine("        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; line-height: 1.6; }");
        content.AppendLine("        h1 { color: #333; border-bottom: 2px solid #4A5D73; padding-bottom: 10px; }");
        content.AppendLine("        .document { margin-bottom: 40px; }");
        content.AppendLine("        .document-content { white-space: pre-wrap; }");
        content.AppendLine("    </style>");
        content.AppendLine("</head>");
        content.AppendLine("<body>");

        foreach (var doc in _manuscript.Documents)
        {
            content.AppendLine("    <div class=\"document\">");
            content.AppendLine($"        <h1>{System.Web.HttpUtility.HtmlEncode(doc.Title)}</h1>");
            content.AppendLine($"        <div class=\"document-content\">{System.Web.HttpUtility.HtmlEncode(doc.Text)}</div>");
            content.AppendLine("    </div>");
        }

        content.AppendLine("</body>");
        content.AppendLine("</html>");

        return content.ToString();
    }

    private void OnExitClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void OnEditorPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.SelectionStartProperty ||
            e.Property == TextBox.SelectionEndProperty ||
            e.Property == TextBox.CaretIndexProperty)
        {
            if (Editor.IsFocused)
            {
                _lastSelectionStart = Editor.SelectionStart;
                _lastSelectionEnd = Editor.SelectionEnd;
                _lastCaretIndex = Editor.CaretIndex;
                _hasSelectionSnapshot = true;

                // Update typewriter mode centering when caret moves
                if (_isTypewriterMode)
                {
                    CenterCurrentLine();
                }
            }
        }

        // Property changed - no action needed
    }

    private void EnsureEditorReadyForCommand()
    {
        if (!_hasSelectionSnapshot)
        {
            Editor.Focus();
            return;
        }

        if (!Editor.IsFocused)
        {
            RestoreEditorSelectionSnapshot();
            Editor.Focus();
        }
    }

    private void RestoreEditorSelectionSnapshot()
    {
        var textLength = Editor.Text?.Length ?? 0;
        var start = Math.Clamp(_lastSelectionStart, 0, textLength);
        var end = Math.Clamp(_lastSelectionEnd, 0, textLength);
        var caret = Math.Clamp(_lastCaretIndex, 0, textLength);

        Editor.SelectionStart = start;
        Editor.SelectionEnd = end;
        Editor.CaretIndex = caret;
    }

    private void OnUndoClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        EnsureEditorReadyForCommand();
        Editor.Undo();
        Editor.Focus();
    }

    private void OnRedoClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        EnsureEditorReadyForCommand();
        Editor.Redo();
        Editor.Focus();
    }

    private async void OnCutClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        EnsureEditorReadyForCommand();
        // Get selection before focus is lost
        var selectedText = Editor.SelectedText;
        if (!string.IsNullOrEmpty(selectedText) && Clipboard != null)
        {
            await Clipboard.SetTextAsync(selectedText);

            // Remove the selected text
            var selectionStart = Editor.SelectionStart;
            var selectionEnd = Editor.SelectionEnd;
            var currentText = Editor.Text ?? string.Empty;
            Editor.Text = currentText.Remove(selectionStart, selectionEnd - selectionStart);
            Editor.CaretIndex = selectionStart;
        }
        Editor.Focus();
    }

    private async void OnCopyClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        EnsureEditorReadyForCommand();
        // Get selection before focus is lost
        var selectedText = Editor.SelectedText;
        if (!string.IsNullOrEmpty(selectedText) && Clipboard != null)
        {
            await Clipboard.SetTextAsync(selectedText);
        }
        Editor.Focus();
    }

    private async void OnPasteClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        EnsureEditorReadyForCommand();
        if (Clipboard == null) return;

        var clipboardText = await Avalonia.Input.Platform.ClipboardExtensions.TryGetTextAsync(Clipboard) ?? string.Empty;
        if (!string.IsNullOrEmpty(clipboardText))
        {
            var caretIndex = Editor.CaretIndex;
            var currentText = Editor.Text ?? string.Empty;

            // If there's a selection, replace it
            if (Editor.SelectionStart != Editor.SelectionEnd)
            {
                var selectionStart = Editor.SelectionStart;
                var selectionEnd = Editor.SelectionEnd;
                currentText = currentText.Remove(selectionStart, selectionEnd - selectionStart);
                Editor.Text = currentText.Insert(selectionStart, clipboardText);
                Editor.CaretIndex = selectionStart + clipboardText.Length;
            }
            else
            {
                // No selection, just insert at caret
                Editor.Text = currentText.Insert(caretIndex, clipboardText);
                Editor.CaretIndex = caretIndex + clipboardText.Length;
            }
        }
        Editor.Focus();
    }


    private async void OnKeyboardShortcutsClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dialog = CreateStyledDialog("Keyboard Shortcuts", 500, 600);

        var outerGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("35,*")
        };

        var titleBar = CreateDialogTitleBar("Keyboard Shortcuts", dialog);
        Grid.SetRow(titleBar, 0);

        var scrollViewer = new ScrollViewer
        {
            Margin = new Avalonia.Thickness(20),
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var stackPanel = new StackPanel
        {
            Spacing = 15
        };

        // File Operations Section
        stackPanel.Children.Add(CreateShortcutSectionHeader("File Operations"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+Shift+N", "New Project"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+O", "Open/Import Project"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+N", "New Document"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+S", "Save Document"));

        // Edit Operations Section
        stackPanel.Children.Add(CreateShortcutSectionHeader("Edit Operations"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+C", "Copy"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+X", "Cut"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+V", "Paste"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+Z", "Undo"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+Y", "Redo"));

        // Find/Search Section
        stackPanel.Children.Add(CreateShortcutSectionHeader("Find/Search"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+F", "Focus Find Box"));
        stackPanel.Children.Add(CreateShortcutItem("F3", "Find Next"));
        stackPanel.Children.Add(CreateShortcutItem("Shift+F3", "Find Previous"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+Shift+F", "Find and Replace"));

        // View/Focus Modes Section
        stackPanel.Children.Add(CreateShortcutSectionHeader("View & Focus"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+Shift+T", "Toggle Typewriter Mode"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+1", "Toggle Focus Mode"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+0 / F11", "Toggle Super Focus Mode"));
        stackPanel.Children.Add(CreateShortcutItem("Escape", "Exit Super Focus Mode"));

        // UI Toggles Section
        stackPanel.Children.Add(CreateShortcutSectionHeader("UI Toggles"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+2", "Toggle Toolbar"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+3", "Toggle Sidebar"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+4", "Toggle Progress Bar"));
        stackPanel.Children.Add(CreateShortcutItem("Ctrl+5", "Toggle Spell Check"));

        // Add extra bottom padding so content doesn't get covered by close button
        stackPanel.Margin = new Avalonia.Thickness(0, 0, 0, 60);

        scrollViewer.Content = stackPanel;
        Grid.SetRow(scrollViewer, 1);

        var closeButton = CreateAccentButton("Close", 100);
        closeButton.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        closeButton.Margin = new Avalonia.Thickness(0, 0, 0, 15);
        closeButton.Click += (s, args) => dialog.Close();

        var buttonPanel = new StackPanel
        {
            Children = { closeButton },
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        Grid.SetRow(buttonPanel, 1);
        buttonPanel.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;

        outerGrid.Children.Add(titleBar);
        outerGrid.Children.Add(scrollViewer);
        outerGrid.Children.Add(buttonPanel);

        dialog.Content = outerGrid;

        await dialog.ShowDialog(this);
    }

    private TextBlock CreateShortcutSectionHeader(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 14,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(_accentColor)),
            Margin = new Avalonia.Thickness(0, 10, 0, 5)
        };
    }

    private Grid CreateShortcutItem(string keys, string description)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("160,*"),
            Margin = new Avalonia.Thickness(0, 3, 0, 3)
        };

        var keyText = new TextBlock
        {
            Text = keys,
            FontFamily = new Avalonia.Media.FontFamily("Consolas, Courier New"),
            FontSize = 13,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF")),
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3A3A3A")),
            Padding = new Avalonia.Thickness(8, 3, 8, 3),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
        };

        var descText = new TextBlock
        {
            Text = description,
            FontSize = 13,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#CCCCCC")),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(10, 0, 0, 0)
        };

        Grid.SetColumn(keyText, 0);
        Grid.SetColumn(descText, 1);

        grid.Children.Add(keyText);
        grid.Children.Add(descText);

        return grid;
    }

    private async void OnAboutClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dialog = CreateStyledDialog("About JetJot", 400, 260);

        var outerGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("35,*")
        };

        var titleBar = CreateDialogTitleBar("About JetJot", dialog);
        Grid.SetRow(titleBar, 0);

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
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Avalonia.Media.Brushes.White
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
            Text = "Version 1.0.0",
            FontSize = 12,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#999999")),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var creatorText = new TextBlock
        {
            Text = "Created by William S. Coolman",
            FontSize = 11,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#888888")),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 5, 0, 0)
        };

        var okButton = CreateAccentButton("OK", 100);
        okButton.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        okButton.Margin = new Avalonia.Thickness(0, 10, 0, 0);

        okButton.Click += (s, args) => dialog.Close();

        stackPanel.Children.Add(titleText);
        stackPanel.Children.Add(taglineText);
        stackPanel.Children.Add(versionText);
        stackPanel.Children.Add(creatorText);
        stackPanel.Children.Add(okButton);

        Grid.SetRow(stackPanel, 1);

        outerGrid.Children.Add(titleBar);
        outerGrid.Children.Add(stackPanel);

        dialog.Content = outerGrid;

        await dialog.ShowDialog(this);
    }

    private async void OnAccentColorButtonClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dialog = CreateStyledDialog("Choose Theme Color", 500, 350);

        var outerGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("35,*")
        };

        var titleBar = CreateDialogTitleBar("Choose Theme Color", dialog);
        Grid.SetRow(titleBar, 0);

        var mainPanel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 20
        };

        var titleText = new TextBlock
        {
            Text = "Select your accent color:",
            FontSize = 14,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#EAEAEA")),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        // Color presets with your new theme names
        var colorGrid = new WrapPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            MaxWidth = 450
        };

        var colors = new[]
        {
            ("#3F5E5A", "Typist Teal"),
            ("#6B4E71", "Paperback Plum"),
            ("#4A5D73", "Bluebook Blue"),
            ("#4F6A5B", "Margin Moss"),
            ("#6A3F3F", "Redline Rust"),
            ("#51415C", "Midnight Manuscript"),
            ("#6A5A3A", "Dog-Eared Dune"),
            ("#3A3A3A", "Editor's Ebony")
        };

        foreach (var (colorHex, colorName) in colors)
        {
            var colorButton = new Button
            {
                Width = 100,
                Height = 70,
                Margin = new Avalonia.Thickness(5),
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(colorHex)),
                Content = colorName,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF")),
                FontSize = 11,
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Avalonia.Thickness(2),
                BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#555555"))
            };

            var capturedColor = colorHex;
            colorButton.Click += (s, args) =>
            {
                _accentColor = capturedColor;
                ApplyAccentColor();
                SavePreferences();
                dialog.Close();
            };

            colorGrid.Children.Add(colorButton);
        }

        mainPanel.Children.Add(titleText);
        mainPanel.Children.Add(colorGrid);

        Grid.SetRow(mainPanel, 1);

        outerGrid.Children.Add(titleBar);
        outerGrid.Children.Add(mainPanel);

        dialog.Content = outerGrid;

        await dialog.ShowDialog(this);
    }

    private void OnCustomizeAccentColorClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Redirect to the new color picker
        OnAccentColorButtonClicked(sender, e);
    }

    private async void OnManuscriptNameDoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ShowRenameManuscriptDialog();
    }

    private async void OnRenameManuscriptClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ShowRenameManuscriptDialog();
    }

    private async void OnRenameManuscriptMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ShowRenameManuscriptDialog();
    }

    private async void OnRenameDocumentMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_activeDocument != null)
        {
            await ShowRenameDialog(_activeDocument);
        }
    }

    private async System.Threading.Tasks.Task ShowRenameManuscriptDialog()
    {
        var dialog = CreateStyledDialog("Rename Project", 400, 185);

        var outerGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("35,*")
        };

        var titleBar = CreateDialogTitleBar("Rename Project", dialog);
        Grid.SetRow(titleBar, 0);

        var contentPanel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20)
        };

        var textBox = new TextBox
        {
            Text = _manuscript.Name,
            Margin = new Avalonia.Thickness(0, 0, 0, 20)
        };

        ApplyAccentToTextBox(textBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10
        };

        var okButton = CreateAccentButton("OK", 80);

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 80,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3A3A3A")),
            Foreground = Avalonia.Media.Brushes.White,
            BorderThickness = new Avalonia.Thickness(0),
            CornerRadius = new CornerRadius(4),
            Padding = new Avalonia.Thickness(12, 6)
        };

        okButton.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                var oldFolderPath = _manuscript.FolderPath;
                var newName = textBox.Text;
                _manuscript.Name = newName;

                // Update folder path to match new manuscript name
                var jetJotRoot = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "JetJot");
                var newFolderPath = System.IO.Path.Combine(jetJotRoot, newName);

                // Rename the folder if it exists and the name changed
                if (System.IO.Directory.Exists(oldFolderPath) && oldFolderPath != newFolderPath)
                {
                    // Save current state to old location first
                    _storage.SaveManuscript(_manuscript);

                    // Move to new folder
                    if (System.IO.Directory.Exists(newFolderPath))
                    {
                        // If target exists, just update the path without moving
                        _manuscript.FolderPath = newFolderPath;
                    }
                    else
                    {
                        System.IO.Directory.Move(oldFolderPath, newFolderPath);
                        _manuscript.FolderPath = newFolderPath;
                    }
                }
                else
                {
                    _manuscript.FolderPath = newFolderPath;
                }
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

        contentPanel.Children.Add(textBox);
        contentPanel.Children.Add(buttonPanel);

        Grid.SetRow(contentPanel, 1);

        outerGrid.Children.Add(titleBar);
        outerGrid.Children.Add(contentPanel);

        dialog.Content = outerGrid;

        textBox.AttachedToVisualTree += (s, e) =>
        {
            textBox.Focus();
            textBox.SelectAll();
        };

        await dialog.ShowDialog(this);
    }

    private void OnDocumentSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (DocumentList.SelectedItem is Document doc)
        {
            // Clear previous selection background
            var listBox = this.FindControl<ListBox>("DocumentList");
            if (listBox != null)
            {
                for (int i = 0; i < _manuscript.Documents.Count; i++)
                {
                    var container = listBox.ContainerFromIndex(i);
                    if (container is Control control)
                    {
                        var itemBorder = FindItemBorder(control);
                        if (itemBorder != null)
                        {
                            itemBorder.Background = Avalonia.Media.Brushes.Transparent;
                        }
                    }
                }
            }

            _activeDocument = doc;
            _manuscript.LastOpenDocumentId = doc.Id;
            Editor.Text = doc.Text;

            // Set read-only state based on lock status
            Editor.IsReadOnly = doc.IsLocked;

            // Apply accent color to selected document
            UpdateSelectedDocumentColor();
        }
    }

    // Removed: Document name is no longer displayed in toolbar
    // private void UpdateDocumentNameDisplay()
    // {
    //     if (_activeDocument != null)
    //     {
    //         DocumentNameText.Text = _activeDocument.Title;
    //         ToolTip.SetTip(DocumentNameText, _activeDocument.Title);
    //     }
    //     else
    //     {
    //         DocumentNameText.Text = "No Document";
    //         ToolTip.SetTip(DocumentNameText, null);
    //     }
    // }

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

        // Highlight the new document after UI updates
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            UpdateSelectedDocumentColor();
        }, Avalonia.Threading.DispatcherPriority.Loaded);
    }

    private void OnManuscriptNamePointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        var textBlock = sender as TextBlock;
        if (textBlock != null)
        {
            textBlock.Foreground = this.FindResource("AccentBrush") as Avalonia.Media.IBrush;
        }
    }

    private void OnManuscriptNamePointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        var textBlock = sender as TextBlock;
        if (textBlock != null)
        {
            textBlock.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));
        }
    }

    private void OnMenuSubmenuOpened(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                // Find the popup in the visual tree
                var popup = menuItem.GetVisualDescendants()
                    .OfType<Avalonia.Controls.Primitives.Popup>()
                    .FirstOrDefault();

                if (popup?.Host is Window popupWindow)
                {
                    // Get the menu item's screen position
                    var menuItemPosition = menuItem.PointToScreen(new Point(0, 0));
                    var menuItemBounds = menuItem.Bounds;

                    // Position the popup window directly below the menu item
                    popupWindow.Position = new PixelPoint(
                        (int)menuItemPosition.X,
                        (int)(menuItemPosition.Y + menuItemBounds.Height)
                    );
                }
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
    }

    private async void OnLogoDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        // Easter egg: Make the plane fly away!
        var logo = this.FindControl<Avalonia.Controls.Image>("LogoImage");
        if (logo?.RenderTransform is not Avalonia.Media.TranslateTransform transform)
            return;

        // Animate the plane flying up and to the right using a simple timer-based animation
        var duration = 1500; // 1.5 seconds
        var steps = 60;
        var delay = duration / steps;

        // Fly away animation
        for (int i = 0; i <= steps; i++)
        {
            var progress = (double)i / steps;
            transform.X = progress * 500;  // Move right
            transform.Y = progress * -300; // Move up
            logo.Opacity = 1.0 - progress;  // Fade out
            await System.Threading.Tasks.Task.Delay(delay);
        }

        // Wait a moment for mourning
        await System.Threading.Tasks.Task.Delay(1000);

        // Reset position
        transform.X = 0;
        transform.Y = 0;

        // Fade back in
        var fadeSteps = 40;
        var fadeDelay = 800 / fadeSteps;
        for (int i = 0; i <= fadeSteps; i++)
        {
            var progress = (double)i / fadeSteps;
            logo.Opacity = progress;
            await System.Threading.Tasks.Task.Delay(fadeDelay);
        }
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
        var dialog = CreateStyledDialog("Rename Document", 350, 155);

        var outerGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("35,*")
        };

        var titleBar = CreateDialogTitleBar("Rename Document", dialog);
        Grid.SetRow(titleBar, 0);

        var contentPanel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20)
        };

        var textBox = new TextBox
        {
            Text = doc.Title,
            Margin = new Avalonia.Thickness(0, 0, 0, 10),
            Watermark = "Document title"
        };

        ApplyAccentToTextBox(textBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var okButton = CreateAccentButton("OK", 80);

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 80,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3A3A3A")),
            Foreground = Avalonia.Media.Brushes.White,
            BorderThickness = new Avalonia.Thickness(0),
            CornerRadius = new CornerRadius(4),
            Padding = new Avalonia.Thickness(12, 6)
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

        contentPanel.Children.Add(textBox);
        contentPanel.Children.Add(buttonPanel);

        Grid.SetRow(contentPanel, 1);

        outerGrid.Children.Add(titleBar);
        outerGrid.Children.Add(contentPanel);

        dialog.Content = outerGrid;

        textBox.AttachedToVisualTree += (s, e) =>
        {
            textBox.Focus();
            textBox.SelectAll();
        };

        await dialog.ShowDialog(this);
    }

    private void OnLockMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is Document doc)
        {
            doc.IsLocked = true;

            // If this is the active document, make the editor read-only
            if (_activeDocument == doc)
            {
                Editor.IsReadOnly = true;
            }

            _storage.SaveManuscript(_manuscript);
        }
    }

    private void OnUnlockMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is Document doc)
        {
            doc.IsLocked = false;

            // If this is the active document, make the editor editable
            if (_activeDocument == doc)
            {
                Editor.IsReadOnly = false;
            }

            _storage.SaveManuscript(_manuscript);
        }
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

    private async System.Threading.Tasks.Task ShowLockedDocumentDialog(string documentTitle)
    {
        var dialog = CreateStyledDialog("Cannot Delete Locked Document", 450, 185);

        var outerGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("35,*")
        };

        var titleBar = CreateDialogTitleBar("Cannot Delete Locked Document", dialog);
        Grid.SetRow(titleBar, 0);

        var stackPanel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 20
        };

        var messageText = new TextBlock
        {
            Text = $"'{documentTitle}' is locked and cannot be deleted.\n\nPlease unlock it first to delete.",
            FontSize = 14,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            TextAlignment = Avalonia.Media.TextAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Foreground = Avalonia.Media.Brushes.White
        };

        var okButton = CreateAccentButton("OK", 100);
        okButton.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        okButton.Click += (s, e) => dialog.Close();

        stackPanel.Children.Add(messageText);
        stackPanel.Children.Add(okButton);

        Grid.SetRow(stackPanel, 1);

        outerGrid.Children.Add(titleBar);
        outerGrid.Children.Add(stackPanel);

        dialog.Content = outerGrid;

        await dialog.ShowDialog(this);
    }

    private async void OnDeleteMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is Document doc)
        {
            // Check if document is locked
            if (doc.IsLocked)
            {
                await ShowLockedDocumentDialog(doc.Title);
                return;
            }

            var dialog = CreateStyledDialog("Delete Document", 400, 185);

            var outerGrid = new Grid
            {
                RowDefinitions = new RowDefinitions("35,*")
            };

            var titleBar = CreateDialogTitleBar("Delete Document", dialog);
            Grid.SetRow(titleBar, 0);

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
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Foreground = Avalonia.Media.Brushes.White
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
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D32F2F")),
                Foreground = Avalonia.Media.Brushes.White,
                BorderThickness = new Avalonia.Thickness(0),
                CornerRadius = new CornerRadius(4),
                Padding = new Avalonia.Thickness(12, 6)
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 100,
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3A3A3A")),
                Foreground = Avalonia.Media.Brushes.White,
                BorderThickness = new Avalonia.Thickness(0),
                CornerRadius = new CornerRadius(4),
                Padding = new Avalonia.Thickness(12, 6)
            };

            confirmButton.Click += (s, args) =>
            {
                _manuscript.Documents.Remove(doc);

                // If we just deleted the active document, select another one
                if (_activeDocument == doc)
                {
                    if (_manuscript.Documents.Count > 0)
                    {
                        // Select the first remaining document
                        _activeDocument = _manuscript.Documents[0];
                        DocumentList.SelectedItem = _activeDocument;
                        Editor.Text = _activeDocument.Text;
                    }
                    else
                    {
                        // No documents left
                        _activeDocument = null;
                        Editor.Text = string.Empty;
                    }
                }

                // Save the manuscript
                _storage.SaveManuscript(_manuscript);

                dialog.Close();
            };

            cancelButton.Click += (s, args) => dialog.Close();

            buttonPanel.Children.Add(confirmButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(messageText);
            stackPanel.Children.Add(buttonPanel);

            Grid.SetRow(stackPanel, 1);

            outerGrid.Children.Add(titleBar);
            outerGrid.Children.Add(stackPanel);

            dialog.Content = outerGrid;

            await dialog.ShowDialog(this);
        }
    }

    private void OnFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Skip if we're loading preferences to avoid conflicts
        if (_isLoadingPreferences)
            return;

        if (FontFamilyComboBox.SelectedItem is ComboBoxItem item &&
            item.Content is string fontName)
        {
            _currentFontFamily = fontName;

            // Apply the font
            var newFontFamily = new Avalonia.Media.FontFamily(fontName);
            Editor.FontFamily = newFontFamily;

            // Debug: Print what font is actually being used
            System.Diagnostics.Debug.WriteLine($"Font changed to: {fontName}");
            System.Diagnostics.Debug.WriteLine($"Editor.FontFamily.Name: {Editor.FontFamily.Name}");
            System.Diagnostics.Debug.WriteLine($"Editor.FontFamily.ToString(): {Editor.FontFamily.ToString()}");

            // Force invalidation of the visual to ensure re-render
            Editor.InvalidateVisual();
            Editor.InvalidateMeasure();

            SavePreferences();
        }
    }

    private void OnFontSizeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FontSizeComboBox.SelectedItem is ComboBoxItem item &&
            item.Content is string sizeText &&
            int.TryParse(sizeText, out int fontSize))
        {
            Editor.FontSize = fontSize;
            SavePreferences();
        }
    }

    private void OnFindTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (e.KeyModifiers == KeyModifiers.Shift)
            {
                FindPrevious();
            }
            else
            {
                FindNext();
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            ClearFind();
            Editor.Focus();
            e.Handled = true;
        }
    }

    private void OnFindNextClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        e.Handled = true;
        FindNext();
    }

    private void OnFindPreviousClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        e.Handled = true;
        FindPrevious();
    }

    private void OnClearFindClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ClearFind();
    }

    private void FindNext()
    {
        var searchText = FindTextBox.Text;
        if (string.IsNullOrEmpty(searchText)) return;

        var editorText = Editor.Text ?? string.Empty;
        if (string.IsNullOrEmpty(editorText)) return;

        // Start searching from after the current selection or last known selection
        var startIndex = Editor.SelectionEnd > 0 ? Editor.SelectionEnd : (_lastSelectionEnd > 0 ? _lastSelectionEnd : Editor.CaretIndex);
        var foundIndex = editorText.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);

        // If not found from current position, wrap around to beginning
        if (foundIndex == -1)
        {
            foundIndex = editorText.IndexOf(searchText, 0, StringComparison.OrdinalIgnoreCase);
        }

        if (foundIndex != -1)
        {
            Editor.Focus();
            Editor.CaretIndex = foundIndex;
            Editor.SelectionStart = foundIndex;
            Editor.SelectionEnd = foundIndex + searchText.Length;

            // Update last selection tracking
            _lastSelectionEnd = Editor.SelectionEnd;
            _lastCaretIndex = Editor.CaretIndex;
        }
    }

    private void FindPrevious()
    {
        var searchText = FindTextBox.Text;
        if (string.IsNullOrEmpty(searchText)) return;

        var editorText = Editor.Text ?? string.Empty;
        if (string.IsNullOrEmpty(editorText)) return;

        // Start searching backwards from current selection start or last known selection
        var selectionStart = Editor.SelectionStart > 0 ? Editor.SelectionStart : (_lastSelectionStart > 0 ? _lastSelectionStart : Editor.CaretIndex);
        var startIndex = Math.Max(0, selectionStart - 1);
        var foundIndex = editorText.LastIndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);

        // If not found before current position, wrap around to end
        if (foundIndex == -1)
        {
            foundIndex = editorText.LastIndexOf(searchText, StringComparison.OrdinalIgnoreCase);
        }

        if (foundIndex != -1)
        {
            Editor.Focus();
            Editor.CaretIndex = foundIndex;
            Editor.SelectionStart = foundIndex;
            Editor.SelectionEnd = foundIndex + searchText.Length;

            // Update last selection tracking
            _lastSelectionStart = Editor.SelectionStart;
            _lastSelectionEnd = Editor.SelectionEnd;
            _lastCaretIndex = Editor.CaretIndex;
        }
    }

    private void ClearFind()
    {
        FindTextBox.Text = string.Empty;
        Editor.SelectionStart = Editor.CaretIndex;
        Editor.SelectionEnd = Editor.CaretIndex;
    }

    private async void OnFindAndReplaceClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dialog = CreateStyledDialog("Find and Replace", 500, 280);

        var outerGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("35,*")
        };

        var titleBar = CreateDialogTitleBar("Find and Replace", dialog);
        Grid.SetRow(titleBar, 0);

        var stackPanel = new StackPanel
        {
            Margin = new Avalonia.Thickness(30, 20, 30, 20),
            Spacing = 15
        };

        var accentBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(_accentColor));

        // Find text box
        var findLabel = new TextBlock
        {
            Text = "Find:",
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#CCCCCC")),
            FontSize = 13
        };

        var (findBorder, findTextBox) = CreateDialogTextBox(accentBrush);

        // Replace text box
        var replaceLabel = new TextBlock
        {
            Text = "Replace with:",
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#CCCCCC")),
            FontSize = 13
        };

        var (replaceBorder, replaceTextBox) = CreateDialogTextBox(accentBrush);

        // Add Enter key handler to trigger Replace All
        void HandleEnterKey(object? s, Avalonia.Input.KeyEventArgs args)
        {
            if (args.Key == Avalonia.Input.Key.Enter)
            {
                args.Handled = true;
                if (!string.IsNullOrEmpty(findTextBox.Text))
                {
                    var editorText = Editor.Text ?? string.Empty;
                    var searchText = findTextBox.Text;
                    var replaceText = replaceTextBox.Text ?? string.Empty;

                    var pattern = @"\b" + System.Text.RegularExpressions.Regex.Escape(searchText) + @"\b";
                    var newText = System.Text.RegularExpressions.Regex.Replace(editorText, pattern, replaceText);

                    Editor.Text = newText;
                    Editor.CaretIndex = 0;
                    dialog.Close();
                }
            }
        }

        findTextBox.KeyDown += HandleEnterKey;
        replaceTextBox.KeyDown += HandleEnterKey;

        // Buttons panel
        var buttonsPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10,
            Margin = new Avalonia.Thickness(0, 10, 0, 0)
        };

        var replaceAllButton = CreateAccentButton("Replace All", 120);
        replaceAllButton.Click += (s, args) =>
        {
            if (string.IsNullOrEmpty(findTextBox.Text)) return;

            var editorText = Editor.Text ?? string.Empty;
            var searchText = findTextBox.Text;
            var replaceText = replaceTextBox.Text ?? string.Empty;

            // Use whole word matching with case sensitivity
            // \b ensures we match whole words only
            var pattern = @"\b" + System.Text.RegularExpressions.Regex.Escape(searchText) + @"\b";
            var newText = System.Text.RegularExpressions.Regex.Replace(
                editorText,
                pattern,
                replaceText
            );

            Editor.Text = newText;
            Editor.CaretIndex = 0;

            dialog.Close();
        };

        var cancelButton = CreateAccentButton("Cancel", 100);
        cancelButton.Click += (s, args) => dialog.Close();

        buttonsPanel.Children.Add(replaceAllButton);
        buttonsPanel.Children.Add(cancelButton);

        stackPanel.Children.Add(findLabel);
        stackPanel.Children.Add(findBorder);
        stackPanel.Children.Add(replaceLabel);
        stackPanel.Children.Add(replaceBorder);
        stackPanel.Children.Add(buttonsPanel);

        Grid.SetRow(stackPanel, 1);

        outerGrid.Children.Add(titleBar);
        outerGrid.Children.Add(stackPanel);

        dialog.Content = outerGrid;

        // Focus the find text box when dialog opens
        findTextBox.AttachedToVisualTree += (s, args) => findTextBox.Focus();

        await dialog.ShowDialog(this);
    }

    private void FindNextOccurrence(string searchText, int startIndex)
    {
        var editorText = Editor.Text ?? string.Empty;
        var foundIndex = editorText.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);

        // If not found from current position, wrap around to beginning
        if (foundIndex == -1)
        {
            foundIndex = editorText.IndexOf(searchText, 0, StringComparison.OrdinalIgnoreCase);
        }

        if (foundIndex != -1)
        {
            Editor.Focus();
            Editor.CaretIndex = foundIndex;
            Editor.SelectionStart = foundIndex;
            Editor.SelectionEnd = foundIndex + searchText.Length;

            // Update last selection tracking
            _lastSelectionStart = Editor.SelectionStart;
            _lastSelectionEnd = Editor.SelectionEnd;
            _lastCaretIndex = Editor.CaretIndex;
        }
    }

    private void UpdateWordCount()
    {
        if (_activeDocument == null)
        {
            WordCountText.Text = "0 / 1000 words";
            WordProgressBar.Maximum = 1000;
            WordProgressBar.Value = 0;
            SpellCheckIndicatorBar.IsVisible = false;
            return;
        }

        // Count words in the current document
        int wordCount = CountWords(_activeDocument.Text);
        int goal = _activeDocument.WordGoal;

        // Update UI
        WordCountText.Text = $"{wordCount} / {goal} words";
        WordProgressBar.Maximum = goal;
        WordProgressBar.Value = Math.Min(wordCount, goal);

        // Update spell check indicator
        UpdateSpellCheckIndicator();
    }

    private void UpdateSpellCheckIndicator()
    {
        if (!_showSpellCheck || _activeDocument == null)
        {
            SpellCheckIndicatorBar.IsVisible = false;
            return;
        }

        string text = _activeDocument.Text;
        if (string.IsNullOrEmpty(text))
        {
            SpellCheckIndicatorBar.IsVisible = false;
            return;
        }

        // Count misspelled words
        int misspelledCount = 0;
        var wordMatches = Regex.Matches(text, @"\b[\w']+\b");

        foreach (Match match in wordMatches)
        {
            string word = match.Value;
            if (!_spellChecker.IsWordCorrect(word))
            {
                misspelledCount++;
            }
        }

        if (misspelledCount > 0)
        {
            SpellCheckIndicator.Text = $"⚠ {misspelledCount}";
            SpellCheckIndicatorBar.IsVisible = true;
        }
        else
        {
            SpellCheckIndicatorBar.IsVisible = false;
        }
    }

    private void OnSpellCheckIndicatorClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_activeDocument == null)
            return;

        string text = _activeDocument.Text;
        if (string.IsNullOrEmpty(text))
            return;

        // Collect all misspelled words
        var wordMatches = Regex.Matches(text, @"\b[\w']+\b");
        var misspelledWords = new List<Match>();

        foreach (Match match in wordMatches)
        {
            string word = match.Value;
            if (!_spellChecker.IsWordCorrect(word))
            {
                misspelledWords.Add(match);
            }
        }

        if (misspelledWords.Count == 0)
            return;

        // Cycle through misspelled words
        _currentSpellCheckIndex = (_currentSpellCheckIndex + 1) % misspelledWords.Count;
        var targetMatch = misspelledWords[_currentSpellCheckIndex];

        // Focus the editor first
        Editor.Focus();

        // Set caret position first
        Editor.CaretIndex = targetMatch.Index;

        // Then select the text
        Editor.SelectionStart = targetMatch.Index;
        Editor.SelectionEnd = targetMatch.Index + targetMatch.Length;
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

        var dialog = CreateStyledDialog("Set Word Goal", 350, 175);

        var outerGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("35,*")
        };

        var titleBar = CreateDialogTitleBar("Set Word Goal", dialog);
        Grid.SetRow(titleBar, 0);

        var stackPanel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20)
        };

        var label = new TextBlock
        {
            Text = "Enter your word count goal:",
            Margin = new Avalonia.Thickness(0, 0, 0, 10),
            Foreground = Avalonia.Media.Brushes.White
        };

        var textBox = new TextBox
        {
            Text = _activeDocument.WordGoal.ToString(),
            Watermark = "e.g., 1000",
            Margin = new Avalonia.Thickness(0, 0, 0, 10)
        };

        ApplyAccentToTextBox(textBox);

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var okButton = CreateAccentButton("OK", 80);

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 80,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3A3A3A")),
            Foreground = Avalonia.Media.Brushes.White,
            BorderThickness = new Avalonia.Thickness(0),
            CornerRadius = new CornerRadius(4),
            Padding = new Avalonia.Thickness(12, 6)
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

        Grid.SetRow(stackPanel, 1);

        outerGrid.Children.Add(titleBar);
        outerGrid.Children.Add(stackPanel);

        dialog.Content = outerGrid;

        textBox.AttachedToVisualTree += (s, args) =>
        {
            textBox.Focus();
            textBox.SelectAll();
        };

        await dialog.ShowDialog(this);
    }

    // Spell Checker Methods
    private void InitializeSpellChecker()
    {
        try
        {
            string dictionaryPath = "en_US.dic";
            string affixPath = "en_US.aff";

            if (_spellChecker.Initialize(dictionaryPath, affixPath))
            {
                Console.WriteLine("Spell checker initialized successfully");

                // Load custom dictionary from preferences
                var preferences = LoadPreferences();
                _spellChecker.LoadCustomDictionary(preferences.CustomDictionary);
            }
            else
            {
                Console.WriteLine("Failed to initialize spell checker");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing spell checker: {ex.Message}");
        }
    }

    private string GetWordAtPosition(int caretIndex)
    {
        string text = Editor.Text ?? "";
        if (string.IsNullOrEmpty(text) || caretIndex < 0 || caretIndex > text.Length)
            return "";

        // Find word boundaries
        int start = caretIndex;
        int end = caretIndex;

        // Move start backwards to beginning of word
        while (start > 0 && (char.IsLetter(text[start - 1]) || text[start - 1] == '\''))
            start--;

        // Move end forwards to end of word
        while (end < text.Length && (char.IsLetter(text[end]) || text[end] == '\''))
            end++;

        if (end > start)
            return text.Substring(start, end - start);

        return "";
    }

    private void ReplaceWordAtPosition(int caretIndex, string oldWord, string newWord)
    {
        string text = Editor.Text ?? "";
        if (string.IsNullOrEmpty(text))
            return;

        // Find the word position
        int start = caretIndex;
        while (start > 0 && (char.IsLetter(text[start - 1]) || text[start - 1] == '\''))
            start--;

        int end = start;
        while (end < text.Length && (char.IsLetter(text[end]) || text[end] == '\''))
            end++;

        if (end > start)
        {
            string wordAtPos = text.Substring(start, end - start);
            if (wordAtPos.Equals(oldWord, StringComparison.OrdinalIgnoreCase))
            {
                Editor.Text = text.Substring(0, start) + newWord + text.Substring(end);
                Editor.CaretIndex = start + newWord.Length;
            }
        }
    }

    private void OnEditorPointerPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        // Only handle right-click
        var point = e.GetCurrentPoint(Editor);

        if (!point.Properties.IsRightButtonPressed)
            return;

        // Show spell check context menu
        ShowSpellCheckContextMenu();
        e.Handled = true;
    }

    private void OnEditorPointerReleasedTunnel(object? sender, PointerReleasedEventArgs e)
    {
        // Only handle right-click
        var point = e.GetCurrentPoint(Editor);

        if (point.Properties.PointerUpdateKind != PointerUpdateKind.RightButtonReleased)
            return;

        // Show spell check context menu
        ShowSpellCheckContextMenu();
        e.Handled = true;
    }

    private void ShowSpellCheckContextMenu()
    {
        var menu = new ContextMenu();
        menu.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2A2A2A"));
        menu.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"));

        int caretIndex = Editor.CaretIndex;
        string word = GetWordAtPosition(caretIndex);

        if (!string.IsNullOrEmpty(word) && !_spellChecker.IsWordCorrect(word))
        {
            // Word is misspelled - show suggestions
            var suggestions = _spellChecker.GetSuggestions(word);

            if (suggestions.Count > 0)
            {
                // Add up to 5 suggestions
                for (int i = 0; i < Math.Min(5, suggestions.Count); i++)
                {
                    var suggestion = suggestions[i];
                    var menuItem = new MenuItem
                    {
                        Header = suggestion,
                        Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF")),
                        FontWeight = Avalonia.Media.FontWeight.Bold
                    };
                    int currentCaretIndex = caretIndex;
                    string currentWord = word;
                    menuItem.Click += (s, args) =>
                    {
                        ReplaceWordAtPosition(currentCaretIndex, currentWord, suggestion);
                    };
                    menu.Items.Add(menuItem);
                }

                menu.Items.Add(new Separator());
            }

            // Add "Ignore" option
            var ignoreMenuItem = new MenuItem
            {
                Header = "Ignore",
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"))
            };
            string wordToIgnore = word;
            ignoreMenuItem.Click += (s, args) =>
            {
                _spellChecker.AddToCustomDictionary(wordToIgnore);

                // Save to preferences
                var prefs = LoadPreferences();
                prefs.CustomDictionary = _spellChecker.GetCustomDictionary();
                SavePreferencesWithCustomDict(prefs);

                // Update the spell check indicator
                UpdateSpellCheckIndicator();
            };
            menu.Items.Add(ignoreMenuItem);

            // Add "Add to Dictionary" option
            var addToDictMenuItem = new MenuItem
            {
                Header = "Add to Dictionary",
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"))
            };
            string wordToAdd = word;
            addToDictMenuItem.Click += (s, args) =>
            {
                _spellChecker.AddToCustomDictionary(wordToAdd);

                // Save to preferences
                var prefs = LoadPreferences();
                prefs.CustomDictionary = _spellChecker.GetCustomDictionary();
                SavePreferencesWithCustomDict(prefs);

                // Update the spell check indicator
                UpdateSpellCheckIndicator();
            };
            menu.Items.Add(addToDictMenuItem);

            menu.Items.Add(new Separator());
        }

        // Add standard editing options
        var cutItem = new MenuItem
        {
            Header = "Cut",
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"))
        };
        cutItem.Click += OnCutClicked;
        menu.Items.Add(cutItem);

        var copyItem = new MenuItem
        {
            Header = "Copy",
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"))
        };
        copyItem.Click += OnCopyClicked;
        menu.Items.Add(copyItem);

        var pasteItem = new MenuItem
        {
            Header = "Paste",
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FFFFFF"))
        };
        pasteItem.Click += OnPasteClicked;
        menu.Items.Add(pasteItem);

        Editor.ContextMenu = menu;
        menu.Open(Editor);
    }

    private void SavePreferencesWithCustomDict(UserPreferences preferences)
    {
        var dir = System.IO.Path.GetDirectoryName(_preferencesFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        var json = System.Text.Json.JsonSerializer.Serialize(preferences, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(_preferencesFilePath, json);
    }


    // Drag and Drop handlers for document reordering
    private void OnDocumentPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Document doc)
        {
            _draggedDocument = doc;
            _dragStartPoint = e.GetPosition(border);
            _isDragging = false;
        }
    }

    private void OnDocumentPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedDocument == null || sender is not Border border) return;

        var currentPoint = e.GetPosition(border);
        var diff = currentPoint - _dragStartPoint;

        // Start dragging if moved more than 5 pixels
        if (!_isDragging && (Math.Abs(diff.X) > 5 || Math.Abs(diff.Y) > 5))
        {
            _isDragging = true;
        }

        if (_isDragging)
        {
            var listBox = this.FindControl<ListBox>("DocumentList");
            var trashZone = this.FindControl<Border>("TrashZone");
            if (listBox == null || trashZone == null) return;

            // Clear all drop indicators first
            HideAllDropIndicators();

            // Check if we're over the trash zone
            var trashPosition = e.GetPosition(trashZone);
            var trashBounds = new Rect(0, 0, trashZone.Bounds.Width, trashZone.Bounds.Height);

            if (trashBounds.Contains(trashPosition))
            {
                _isOverTrash = true;
                _dropTargetIndex = -1;
                // Highlight trash zone
                trashZone.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E81123"));
                return;
            }
            else
            {
                _isOverTrash = false;
                trashZone.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2A2A2A"));
            }

            var position = e.GetPosition(listBox);
            int draggedIndex = _manuscript.Documents.IndexOf(_draggedDocument);

            // Find which position we're hovering over
            for (int i = 0; i < _manuscript.Documents.Count; i++)
            {
                var container = listBox.ContainerFromIndex(i);
                if (container is Control control)
                {
                    var controlPos = control.TranslatePoint(new Point(0, 0), listBox);
                    if (controlPos.HasValue)
                    {
                        var itemBounds = new Rect(controlPos.Value, control.Bounds.Size);

                        if (itemBounds.Contains(position))
                        {
                            // Determine if we should show indicator above or below
                            var relativeY = position.Y - controlPos.Value.Y;
                            var midPoint = itemBounds.Height / 2;

                            if (relativeY < midPoint)
                            {
                                // Show drop indicator above this item
                                _dropTargetIndex = i;
                            }
                            else
                            {
                                // Show drop indicator below this item
                                _dropTargetIndex = i + 1;
                            }

                            // Don't show indicator if dropping in same position
                            if (_dropTargetIndex == draggedIndex || _dropTargetIndex == draggedIndex + 1)
                            {
                                _dropTargetIndex = -1;
                            }
                            else
                            {
                                ShowDropIndicator(_dropTargetIndex);
                            }
                            break;
                        }
                    }
                }
            }

            // If there are no root documents, allow dropping into the empty root list by setting index 0 and showing a visual cue
            if (_dropTargetIndex == -1 && _manuscript.Documents.Count == 0)
            {
                var positionInList = e.GetPosition(listBox);
                var listBounds = new Rect(0, 0, listBox.Bounds.Width, listBox.Bounds.Height);

                if (listBounds.Contains(positionInList))
                {
                    _dropTargetIndex = 0;

                    // Show a simple visual cue by tinting the list background with the accent color
                    var accentBrush = this.FindResource("AccentBrush") as Avalonia.Media.SolidColorBrush
                                      ?? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(_accentColor));
                    listBox.Background = accentBrush;
                }
            }
        }
    }

    private async void OnDocumentPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var trashZone = this.FindControl<Border>("TrashZone");

        if (_isDragging && _draggedDocument != null)
        {
            // Check if dropped on trash zone
            if (_isOverTrash)
            {
                // Check if document is locked
                if (_draggedDocument.IsLocked)
                {
                    // Show error dialog for locked document
                    var errorDialog = CreateStyledDialog("Cannot Delete", 400, 160);

                    var outerGrid = new Grid
                    {
                        RowDefinitions = new RowDefinitions("35,*")
                    };

                    var titleBar = CreateDialogTitleBar("Cannot Delete", errorDialog);
                    Grid.SetRow(titleBar, 0);

                    var stackPanel = new StackPanel
                    {
                        Margin = new Avalonia.Thickness(20),
                        Spacing = 20
                    };

                    var messageText = new TextBlock
                    {
                        Text = $"Cannot delete '{_draggedDocument.Title}' because it is locked. Unlock it first.",
                        FontSize = 14,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Foreground = Avalonia.Media.Brushes.White
                    };

                    var okButton = new Button
                    {
                        Content = "OK",
                        Width = 100,
                        Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(_accentColor)),
                        Foreground = Avalonia.Media.Brushes.White,
                        BorderThickness = new Avalonia.Thickness(0),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Avalonia.Thickness(12, 6),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    };

                    okButton.Click += (s, args) => errorDialog.Close();

                    stackPanel.Children.Add(messageText);
                    stackPanel.Children.Add(okButton);

                    Grid.SetRow(stackPanel, 1);
                    outerGrid.Children.Add(titleBar);
                    outerGrid.Children.Add(stackPanel);

                    errorDialog.Content = outerGrid;
                    await errorDialog.ShowDialog(this);
                }
                else
                {
                    // Show confirmation dialog before deleting
                    var dialog = CreateStyledDialog("Delete Document", 400, 185);

                    var outerGrid = new Grid
                    {
                        RowDefinitions = new RowDefinitions("35,*")
                    };

                    var titleBar = CreateDialogTitleBar("Delete Document", dialog);
                    Grid.SetRow(titleBar, 0);

                    var stackPanel = new StackPanel
                    {
                        Margin = new Avalonia.Thickness(20),
                        Spacing = 20
                    };

                    var messageText = new TextBlock
                    {
                        Text = $"Are you sure you want to delete '{_draggedDocument.Title}'?",
                        FontSize = 14,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Foreground = Avalonia.Media.Brushes.White
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
                        Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D32F2F")),
                        Foreground = Avalonia.Media.Brushes.White,
                        BorderThickness = new Avalonia.Thickness(0),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Avalonia.Thickness(12, 6)
                    };

                    var cancelButton = new Button
                    {
                        Content = "Cancel",
                        Width = 100,
                        Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3A3A3A")),
                        Foreground = Avalonia.Media.Brushes.White,
                        BorderThickness = new Avalonia.Thickness(0),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Avalonia.Thickness(12, 6)
                    };

                    var docToDelete = _draggedDocument; // Capture for the lambda

                    confirmButton.Click += (s, args) =>
                    {
                        // Delete the document
                        _manuscript.Documents.Remove(docToDelete);

                        // Update active document if needed
                        if (_activeDocument == docToDelete)
                        {
                            // Find a new document to select
                            Document? newActiveDoc = null;

                            if (_manuscript.Documents.Count > 0)
                            {
                                newActiveDoc = _manuscript.Documents[0];
                            }

                            if (newActiveDoc != null)
                            {
                                _activeDocument = newActiveDoc;
                                DocumentList.SelectedItem = _activeDocument;
                                Editor.Text = _activeDocument.Text;
                            }
                            else
                            {
                                _activeDocument = null;
                                Editor.Text = string.Empty;
                            }
                        }

                        _storage.SaveManuscript(_manuscript);
                        dialog.Close();
                    };

                    cancelButton.Click += (s, args) => dialog.Close();

                    buttonPanel.Children.Add(confirmButton);
                    buttonPanel.Children.Add(cancelButton);

                    stackPanel.Children.Add(messageText);
                    stackPanel.Children.Add(buttonPanel);

                    Grid.SetRow(stackPanel, 1);

                    outerGrid.Children.Add(titleBar);
                    outerGrid.Children.Add(stackPanel);

                    dialog.Content = outerGrid;

                    await dialog.ShowDialog(this);

                    // Reset trash zone background after dialog closes
                    if (trashZone != null)
                    {
                        trashZone.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2A2A2A"));
                    }
                }
            }
            else if (_dropTargetIndex >= 0)
            {
                // Reorder the document
                int oldIndex = _manuscript.Documents.IndexOf(_draggedDocument);
                int newIndex = _dropTargetIndex;

                // Adjust index if moving down
                if (newIndex > oldIndex)
                {
                    newIndex--;
                }

                if (oldIndex != newIndex)
                {
                    _manuscript.Documents.Move(oldIndex, newIndex);
                    _storage.SaveManuscript(_manuscript);
                }
            }
        }

        // If this was a simple click (no drag) then select the document we pressed on (covers section documents)
        if (!_isDragging && _draggedDocument != null)
        {
            _activeDocument = _draggedDocument;
            _manuscript.LastOpenDocumentId = _activeDocument.Id;
            Editor.Text = _activeDocument.Text;
            Editor.IsReadOnly = _activeDocument.IsLocked;

            // Clear root list selection unless the selected doc lives there
            var list = this.FindControl<ListBox>("DocumentList");
            if (list != null)
            {
                list.SelectedItem = _manuscript.Documents.Contains(_activeDocument) ? _activeDocument : null;
            }

            UpdateSelectedDocumentColor();
        }

        HideAllDropIndicators();

        // Reset trash zone background
        if (trashZone != null)
        {
            trashZone.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2A2A2A"));
        }

        _draggedDocument = null;
        _isDragging = false;
        _dropTargetIndex = -1;
        _isOverTrash = false;
    }

    private void HideAllDropIndicators()
    {
        var listBox = this.FindControl<ListBox>("DocumentList");
        if (listBox == null) return;

        for (int i = 0; i < _manuscript.Documents.Count; i++)
        {
            var container = listBox.ContainerFromIndex(i);
            if (container is Control control)
            {
                var itemBorder = FindItemBorder(control);
                if (itemBorder != null)
                {
                    itemBorder.BorderBrush = Avalonia.Media.Brushes.Transparent;
                    itemBorder.BorderThickness = DropIndicatorTopThickness;
                }
            }
        }
        // Reset list background if it had been used as an empty-list drop indicator
        var sidebar = this.FindControl<Border>("SidebarPanel");
        if (sidebar != null)
        {
            listBox.Background = sidebar.Background;
        }
        else
        {
            listBox.Background = Avalonia.Media.Brushes.Transparent;
        }
    }

    private void ShowDropIndicator(int position)
    {
        var listBox = this.FindControl<ListBox>("DocumentList");
        if (listBox == null) return;

        // Show indicator at the drop position by changing the border color
        if (position == 0 && _manuscript.Documents.Count > 0)
        {
            // Show at top of first item
            var container = listBox.ContainerFromIndex(0);
            if (container is Control control)
            {
                var itemBorder = FindItemBorder(control);
                if (itemBorder != null)
                {
                    itemBorder.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(_accentColor));
                    itemBorder.BorderThickness = DropIndicatorTopThickness;
                }
            }
        }
        else if (position > 0 && position < _manuscript.Documents.Count)
        {
            // Show at top of the target item
            var container = listBox.ContainerFromIndex(position);
            if (container is Control control)
            {
                var itemBorder = FindItemBorder(control);
                if (itemBorder != null)
                {
                    itemBorder.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(_accentColor));
                    itemBorder.BorderThickness = DropIndicatorTopThickness;
                }
            }
        }
        else if (position == _manuscript.Documents.Count && _manuscript.Documents.Count > 0)
        {
            // Show at bottom of the last item for end-of-list drops
            var container = listBox.ContainerFromIndex(_manuscript.Documents.Count - 1);
            if (container is Control control)
            {
                var itemBorder = FindItemBorder(control);
                if (itemBorder != null)
                {
                    itemBorder.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(_accentColor));
                    itemBorder.BorderThickness = DropIndicatorBottomThickness;
                }
            }
        }
    }

    private Border? FindItemBorder(Control container)
    {
        if (container is Border directBorder && directBorder.Name == "ItemContainer")
        {
            return directBorder;
        }

        return container.GetVisualDescendants()
            .OfType<Border>()
            .FirstOrDefault(border => border.Name == "ItemContainer");
    }

    // Trash zone hover handlers
    private void OnTrashZonePointerEnter(object? sender, PointerEventArgs e)
    {
        if (_isDragging && sender is Border trashZone)
        {
            trashZone.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E81123"));
        }
    }

    private void OnTrashZonePointerLeave(object? sender, PointerEventArgs e)
    {
        if (sender is Border trashZone)
        {
            trashZone.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2A2A2A"));
        }
    }

}
