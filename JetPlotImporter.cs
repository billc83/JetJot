using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using JetJot.Models;

namespace JetJot
{
    internal static class JetPlotImporter
    {
        // Minimal DTOs — only the fields JetJot cares about; unknown fields are silently skipped.
        private sealed class JetPlotFile
        {
            public string Name { get; set; } = string.Empty;
            public List<JetPlotNode> Nodes { get; set; } = new();
        }

        private sealed class JetPlotNode
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public double X { get; set; }
        }

        // Unknown JSON members (connections, intensity, shape, y, …) are silently skipped by default.
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Reads only the project name from a .jetplot file without fully deserializing it.
        /// </summary>
        public static string ReadProjectName(string jetplotFilePath)
        {
            var json = File.ReadAllText(jetplotFilePath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("name", out var nameProp))
                return nameProp.GetString() ?? "Imported Project";
            return "Imported Project";
        }

        /// <summary>
        /// Imports a .jetplot file and writes a JetJot manuscript folder to <paramref name="outputFolderPath"/>.
        /// Nodes are ordered left-to-right by their X canvas coordinate.
        /// </summary>
        public static void Import(string jetplotFilePath, string outputFolderPath)
        {
            var json = File.ReadAllText(jetplotFilePath);
            var plotFile = JsonSerializer.Deserialize<JetPlotFile>(json, _options)
                ?? throw new InvalidDataException("Failed to deserialize the .jetplot file.");

            // Sort nodes left-to-right by canvas X position (best approximation of narrative order).
            var sortedNodes = plotFile.Nodes.OrderBy(n => n.X).ToList();

            var manuscript = new Manuscript
            {
                Name = string.IsNullOrWhiteSpace(plotFile.Name) ? "Imported Project" : plotFile.Name,
                FolderPath = outputFolderPath
            };

            foreach (var node in sortedNodes)
            {
                manuscript.Documents.Add(new Document
                {
                    Id = Guid.Parse(node.Id),
                    Title = string.IsNullOrWhiteSpace(node.Title) ? "Untitled" : node.Title,
                    Text = node.Description ?? string.Empty,
                    WordGoal = 1000,
                    IsLocked = false
                });
            }

            new ManuscriptStorage().SaveManuscript(manuscript);
        }

        /// <summary>
        /// Returns <paramref name="basePath"/> if it does not exist, otherwise appends " (2)", " (3)", … until unique.
        /// </summary>
        public static string GetUniqueFolderPath(string basePath)
        {
            if (!Directory.Exists(basePath))
                return basePath;

            int suffix = 2;
            string candidate;
            do
            {
                candidate = $"{basePath} ({suffix++})";
            } while (Directory.Exists(candidate));

            return candidate;
        }

        /// <summary>
        /// Replaces characters that are invalid in folder names with underscores.
        /// </summary>
        public static string SanitizeFolderName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = string.Concat(name.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c)).Trim();
            return string.IsNullOrEmpty(sanitized) ? "Imported Project" : sanitized;
        }
    }
}
