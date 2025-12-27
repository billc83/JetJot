using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetJot.Models;

namespace JetJot;

public class Manuscript : INotifyPropertyChanged
{
    private string _name = "Untitled Manuscript";

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public string FolderPath { get; set; } = string.Empty;
    public Guid? LastOpenDocumentId { get; set; }
    public ObservableCollection<Document> Documents { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}