using System;
using System.Collections.ObjectModel;
using JetJot.Models;

namespace JetJot;

public class Manuscript
{
    public string Name { get; set; } = "Untitled Manuscript";
    public string FolderPath { get; set; } = string.Empty;
    public Guid? LastOpenDocumentId { get; set; }
    public ObservableCollection<Document> Documents { get; } = new();
}