using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using JetJot.Models;

namespace JetJot
{
    public class ManuscriptStorage
    {
        public void SaveManuscript(Manuscript manuscript)
        {
            if (string.IsNullOrEmpty(manuscript.FolderPath))
            {
                throw new InvalidOperationException("Manuscript FolderPath is not set");
            }

            // Create directory if it doesn't exist
            Console.WriteLine("CREATE DIRECTORY");
            Console.WriteLine($"FolderPath = {manuscript.FolderPath}");
            Directory.CreateDirectory(manuscript.FolderPath);

            // Build manifest
            var manifest = new ManuscriptManifest
            {
                Name = manuscript.Name,
                LastOpenDocumentId = manuscript.LastOpenDocumentId
            };

            for (int i = 0; i < manuscript.Documents.Count; i++)
            {
                var doc = manuscript.Documents[i];

                // Add to manifest
                manifest.Documents.Add(new DocumentManifest
                {
                    Id = doc.Id,
                    Title = doc.Title,
                    WordGoal = doc.WordGoal,
                    IsLocked = doc.IsLocked,
                    Order = i
                });

                // Write document text to file
                var filePath = Path.Combine(manuscript.FolderPath, doc.FileName);
                File.WriteAllText(filePath, doc.Text);
            }

            // Write manifest
            var manifestPath = Path.Combine(manuscript.FolderPath, "manuscript.json");
            var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(manifestPath, json);
        }

        public Manuscript LoadManuscript(string folderPath)
        {
            // Read manifest
            Console.WriteLine("ATTEMPTING TO READ / LOAD MANUSCRIPT");
            Console.WriteLine($"FolderPath = {folderPath}");
            var manifestPath = Path.Combine(folderPath, "manuscript.json");
            var json = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<ManuscriptManifest>(json);

            // Create manuscript
            var manuscript = new Manuscript
            {
                Name = manifest.Name,
                FolderPath = folderPath,
                LastOpenDocumentId = manifest.LastOpenDocumentId
            };

            // Load documents in order
            var orderedDocs = manifest.Documents.OrderBy(d => d.Order).ToList();

            foreach (var docManifest in orderedDocs)
            {
                var doc = new Document
                {
                    Id = docManifest.Id,
                    Title = docManifest.Title,
                    WordGoal = docManifest.WordGoal,
                    IsLocked = docManifest.IsLocked
                };

                // Load text from file
                var filePath = Path.Combine(folderPath, doc.FileName);
                doc.Text = File.ReadAllText(filePath);

                manuscript.Documents.Add(doc);
            }

            return manuscript;
        }
    }
}
