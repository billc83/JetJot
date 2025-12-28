using System;

namespace JetJot.Models
{
    public class DocumentManifest
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "Untitled";
        public int WordGoal { get; set; } = 1000;
        public bool IsLocked { get; set; } = false;
        public int Order { get; set; }
    }
}
