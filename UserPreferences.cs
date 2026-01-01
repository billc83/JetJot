using System.Collections.Generic;

namespace JetJot.Models
{
    public class UserPreferences
    {
        public bool ShowToolbar { get; set; } = true;
        public bool ShowSidebar { get; set; } = true;
        public bool ShowFooter { get; set; } = true;
        public bool ShowSpellCheck { get; set; } = true;
        public bool TypewriterMode { get; set; } = false;
        public string FontFamily { get; set; } = "IBM Plex Sans";
        public int FontSize { get; set; } = 16;
        public string AccentColor { get; set; } = "#4A5D73";
        public bool ThemedTitleBar { get; set; } = true;
        public bool ThemedCursor { get; set; } = true;
        public double SidebarWidth { get; set; } = 240;
        public double LeftMargin { get; set; } = 40;
        public double RightMargin { get; set; } = 40;
        public string? LastOpenManuscriptPath { get; set; }
        public List<string> CustomDictionary { get; set; } = new List<string>();
    }
}
