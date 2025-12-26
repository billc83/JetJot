using System.Collections.ObjectModel;
using JetJot.Models;

namespace JetJot;

public class Manuscript
{
    public ObservableCollection<Document> Documents { get; } = new();
}