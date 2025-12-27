using System;
using System.Collections.Generic;

namespace JetJot.Models
{
    public class RecentManuscripts
    {
        public List<RecentManuscriptEntry> Recents { get; set; } = new List<RecentManuscriptEntry>();
    }

    public class RecentManuscriptEntry
    {
        public string Name { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public DateTime LastOpened { get; set; } = DateTime.Now;
    }
}
