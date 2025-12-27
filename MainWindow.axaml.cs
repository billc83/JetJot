using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.VisualTree;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
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
    private static readonly Avalonia.Media.IBrush DropIndicatorBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B5CA8"));

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

    public MainWindow()
    {
        InitializeComponent();

        // Add F11 and Escape key handlers for Super Focus Mode
        this.KeyDown += (sender, e) =>
        {
            if (e.Key == Avalonia.Input.Key.F11)
            {
                OnToggleSuperFocusMode(null, null!);
                e.Handled = true;
            }
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

        // Try to find an existing manuscript folder (look for any manuscript.json)
        string? existingManuscriptFolder = null;
        if (System.IO.Directory.Exists(jetJotRoot))
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
            var doc1 = new Document { Title = "First Document", Text = "Start writing..." };
            var doc2 = new Document { Title = "Second Document", Text = "More ideas here..." };
            _manuscript.Documents.Add(doc1);
            _manuscript.Documents.Add(doc2);
        }

        // Populate sidebar
        DocumentList.ItemsSource = _manuscript.Documents;
        ManuscriptNameText.DataContext = _manuscript;

        // Select document (last open or first)
        Document docToSelect;
        if (_manuscript.LastOpenDocumentId.HasValue)
        {
            docToSelect = _manuscript.Documents.FirstOrDefault(d => d.Id == _manuscript.LastOpenDocumentId.Value)
                          ?? _manuscript.Documents.FirstOrDefault();
        }
        else
        {
            docToSelect = _manuscript.Documents.FirstOrDefault();
        }

        if (docToSelect != null)
        {
            _activeDocument = docToSelect;
            DocumentList.SelectedItem = docToSelect;
            Editor.Text = docToSelect.Text;
        }

        // Sidebar selection changes active document
        DocumentList.SelectionChanged += OnDocumentSelected;

        // Typing updates active document
        Editor.TextChanged += OnEditorTextChanged;
        Editor.PropertyChanged += OnEditorPropertyChanged;

        // New document button
        NewDocumentButton.Click += OnNewDocumentClicked;

        // Font size selector
        FontSizeComboBox.SelectionChanged += OnFontSizeChanged;

        // Set goal button
        SetGoalButton.Click += OnSetGoalClicked;

        // Update word count initially
        UpdateWordCount();

        // Update recents menu
        UpdateRecentsMenu();

        // Load and apply user preferences
        LoadAndApplyPreferences();

        // Save on window close
        this.Closing += OnWindowClosing;
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!string.IsNullOrEmpty(_manuscript.FolderPath))
        {
            _storage.SaveManuscript(_manuscript);
        }
    }

    private async void OnNewManuscriptMenuClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Save current manuscript
        if (!string.IsNullOrEmpty(_manuscript.FolderPath))
        {
            _storage.SaveManuscript(_manuscript);
        }

        // Prompt for new manuscript name
        var dialog = new Window
        {
            Title = "New Manuscript",
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var textBox = new TextBox
        {
            Text = "Untitled Manuscript",
            Width = 350,
            Margin = new Avalonia.Thickness(0, 0, 0, 20)
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10
        };

        var okButton = new Button
        {
            Content = "Create",
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
            Margin = new Avalonia.Thickness(20)
        };
        panel.Children.Add(textBox);
        panel.Children.Add(buttonPanel);

        dialog.Content = panel;

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
            Title = "Select Manuscript Folder",
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
                await ShowErrorDialog("Invalid Manuscript", "The selected folder is not a valid JetJot manuscript.");
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
                        "Manuscript Already Exists",
                        $"A manuscript named '{manuscriptFolderName}' already exists in your JetJot folder. Do you want to overwrite it?");

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
        var doc1 = new Document { Title = "First Document", Text = "Start writing..." };
        var doc2 = new Document { Title = "Second Document", Text = "More ideas here..." };
        _manuscript.Documents.Add(doc1);
        _manuscript.Documents.Add(doc2);

        // Select first document
        _activeDocument = doc1;
        DocumentList.SelectedItem = doc1;
        Editor.Text = doc1.Text;

        // Save new manuscript
        _storage.SaveManuscript(_manuscript);

        // Add to recents
        AddToRecents(_manuscript.FolderPath, _manuscript.Name);
    }

    private async void LoadManuscriptFromFolder(string folderPath)
    {
        try
        {
            // Check if folder exists
            if (!System.IO.Directory.Exists(folderPath))
            {
                await ShowErrorDialog("Manuscript Not Found", $"The folder does not exist:\n{folderPath}");
                RemoveFromRecents(folderPath);
                return;
            }

            // Check if manifest exists
            var manifestPath = System.IO.Path.Combine(folderPath, "manuscript.json");
            if (!System.IO.File.Exists(manifestPath))
            {
                await ShowErrorDialog("Invalid Manuscript", "The selected folder is not a valid JetJot manuscript.");
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
            Document? docToSelect;
            if (_manuscript.LastOpenDocumentId.HasValue)
            {
                docToSelect = _manuscript.Documents.FirstOrDefault(d => d.Id == _manuscript.LastOpenDocumentId.Value)
                              ?? _manuscript.Documents.FirstOrDefault();
            }
            else
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
        }
        catch (Exception ex)
        {
            await ShowErrorDialog("Error Loading Manuscript", $"An error occurred:\n{ex.Message}");
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
                var menuItem = new MenuItem
                {
                    Header = recent.Name,
                    Tag = "RecentItem"
                };

                var recentPath = recent.FolderPath;
                menuItem.Click += (s, e) => LoadManuscriptFromFolder(recentPath);

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

        // Adjust column width: 0 when hidden, 240 when visible
        var mainGrid = this.FindControl<Grid>("MainGrid");
        if (mainGrid != null && mainGrid.ColumnDefinitions.Count > 0)
        {
            mainGrid.ColumnDefinitions[0].Width = isVisible ? new GridLength(240) : new GridLength(0);
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

        SavePreferences();
        Editor.Focus();
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
                mainGrid.ColumnDefinitions[0].Width = _preFocusModeSidebarVisible ? new GridLength(240) : new GridLength(0);
            }

            // Update checkboxes to match restored state
            CheckToolbar.IsChecked = _preFocusModeToolbarVisible;
            CheckSidebar.IsChecked = _preFocusModeSidebarVisible;
            CheckFooter.IsChecked = _preFocusModeFooterVisible;
        }

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
            outerGrid.RowDefinitions[0].Height = new GridLength(40); // Top row
            outerGrid.RowDefinitions[2].Height = new GridLength(40); // Bottom row
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
                mainGrid.ColumnDefinitions[0].Width = _preSuperFocusModeSidebarVisible ? new GridLength(240) : new GridLength(0);
            }
            if (mainGrid.RowDefinitions.Count >= 2)
            {
                mainGrid.RowDefinitions[1].Height = new GridLength(40); // Footer row
            }
        }

        // Restore normal padding
        var editorArea = this.FindControl<Border>("EditorAreaBorder");
        if (editorArea != null)
        {
            editorArea.Padding = new Thickness(20);
        }

        // Restore corner radius
        var editorInner = this.FindControl<Border>("EditorInnerBorder");
        if (editorInner != null)
        {
            editorInner.CornerRadius = new CornerRadius(6);
        }

        // Update checkboxes to match restored state
        CheckToolbar.IsChecked = _preSuperFocusModeToolbarVisible;
        CheckSidebar.IsChecked = _preSuperFocusModeSidebarVisible;
        CheckFooter.IsChecked = _preSuperFocusModeFooterVisible;
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

        if (_isTypewriterMode)
        {
            // Center the current line
            CenterCurrentLine();
        }

        SavePreferences();
        Editor.Focus();
    }

    private void CenterCurrentLine()
    {
        if (!_isTypewriterMode) return;

        // Find the ScrollViewer inside the Editor TextBox
        var scrollViewer = Editor.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault();

        if (scrollViewer == null) return;

        // Get the TextPresenter to calculate line positions
        var textPresenter = Editor.GetVisualDescendants()
            .OfType<TextPresenter>()
            .FirstOrDefault();

        if (textPresenter == null) return;

        // Calculate the line height (approximate)
        var lineHeight = Editor.FontSize * 1.35; // Typical line height multiplier

        // Calculate which line the caret is on
        var text = Editor.Text ?? string.Empty;
        var caretIndex = Editor.CaretIndex;

        var linesBeforeCaret = text.Substring(0, Math.Min(caretIndex, text.Length))
            .Count(c => c == '\n');

        // Calculate the Y position of the current line
        var currentLineY = linesBeforeCaret * lineHeight;

        // Calculate the center position (middle of the viewport)
        var viewportHeight = scrollViewer.Viewport.Height;
        var targetOffset = currentLineY - (viewportHeight / 2) + (lineHeight / 2);

        // Scroll to center the line
        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, Math.Max(0, targetOffset));
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
        var preferences = new UserPreferences
        {
            ShowToolbar = ToolbarPanel.IsVisible,
            ShowSidebar = SidebarPanel.IsVisible,
            ShowFooter = FooterPanel.IsVisible,
            TypewriterMode = _isTypewriterMode
        };

        var dir = System.IO.Path.GetDirectoryName(_preferencesFilePath);
        if (!string.IsNullOrEmpty(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        var json = System.Text.Json.JsonSerializer.Serialize(preferences, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(_preferencesFilePath, json);
    }

    private void LoadAndApplyPreferences()
    {
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
            mainGrid.ColumnDefinitions[0].Width = preferences.ShowSidebar ? new GridLength(240) : new GridLength(0);
        }

        // Apply footer preference
        FooterPanel.IsVisible = preferences.ShowFooter;
        CheckFooter.IsChecked = preferences.ShowFooter;

        // Apply typewriter mode preference
        _isTypewriterMode = preferences.TypewriterMode;
        CheckTypewriterMode.IsChecked = preferences.TypewriterMode;
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

        var clipboardText = await Clipboard.GetTextAsync() ?? string.Empty;
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
        var dialog = new Window
        {
            Title = "Rename Manuscript",
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var textBox = new TextBox
        {
            Text = _manuscript.Name,
            Width = 350,
            Margin = new Avalonia.Thickness(0, 0, 0, 20)
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 10
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

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20)
        };
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

    private void OnDocumentSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (DocumentList.SelectedItem is Document doc)
        {
            _activeDocument = doc;
            _manuscript.LastOpenDocumentId = doc.Id;
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
        }
    }

    private void OnDocumentPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var trashZone = this.FindControl<Border>("TrashZone");

        if (_isDragging && _draggedDocument != null)
        {
            // Check if dropped on trash zone
            if (_isOverTrash)
            {
                // Delete the document
                _manuscript.Documents.Remove(_draggedDocument);

                // Update active document if needed
                if (_activeDocument == _draggedDocument)
                {
                    if (_manuscript.Documents.Count > 0)
                    {
                        _activeDocument = _manuscript.Documents[0];
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

                // Reset trash zone background
                if (trashZone != null)
                {
                    trashZone.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2A2A2A"));
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
                    itemBorder.BorderBrush = DropIndicatorBrush;
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
                    itemBorder.BorderBrush = DropIndicatorBrush;
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
                    itemBorder.BorderBrush = DropIndicatorBrush;
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
