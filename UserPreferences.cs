namespace JetJot.Models
{
    public class UserPreferences
    {
        public bool ShowToolbar { get; set; } = true;
        public bool ShowSidebar { get; set; } = true;
        public bool ShowFooter { get; set; } = true;
        public bool TypewriterMode { get; set; } = false;
    }
}
