namespace JetJot.Models
{
    public class UserPreferences
    {
        public bool ShowToolbar { get; set; } = true;
        public bool ShowSidebar { get; set; } = true;
        public bool ShowFooter { get; set; } = true;
        public bool TypewriterMode { get; set; } = false;
        public string FontFamily { get; set; } = "IBM Plex Sans";
        public int FontSize { get; set; } = 16;
        public string AccentColor { get; set; } = "#4A5D73";
        public bool ThemedTitleBar { get; set; } = true;
        public string? LastOpenManuscriptPath { get; set; }
    }
}
