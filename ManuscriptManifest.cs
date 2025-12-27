using System;
using System.Collections.Generic;

namespace JetJot.Models
{
    public class ManuscriptManifest
    {
        public string Name { get; set; } = "Untitled Manuscript";
        public Guid? LastOpenDocumentId { get; set; }
        public List<DocumentManifest> Documents { get; set; } = new List<DocumentManifest>();
    }
}
