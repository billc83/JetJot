using System;
using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell;

namespace JetJot
{
    public class SpellCheckerService
    {
        private WordList? _wordList;
        private HashSet<string> _customDictionary = new();

        public bool Initialize(string dictionaryPath, string affixPath)
        {
            try
            {
                _wordList = WordList.CreateFromFiles(dictionaryPath);
                Console.WriteLine($"Loaded dictionary with {_wordList.RootWords.Count()} root words");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize spell checker: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public bool IsWordCorrect(string word)
        {
            if (_wordList == null || string.IsNullOrWhiteSpace(word))
                return true;

            // Check if in custom dictionary first
            if (_customDictionary.Contains(word.ToLower()))
                return true;

            return _wordList.Check(word);
        }

        public List<string> GetSuggestions(string word)
        {
            if (_wordList == null || string.IsNullOrWhiteSpace(word))
                return new List<string>();

            return _wordList.Suggest(word).ToList();
        }

        public void AddToCustomDictionary(string word)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                _customDictionary.Add(word.ToLower());
            }
        }

        public void RemoveFromCustomDictionary(string word)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                _customDictionary.Remove(word.ToLower());
            }
        }

        public void LoadCustomDictionary(List<string> words)
        {
            _customDictionary.Clear();
            foreach (var word in words)
            {
                AddToCustomDictionary(word);
            }
        }

        public List<string> GetCustomDictionary()
        {
            return _customDictionary.ToList();
        }
    }
}
