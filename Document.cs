using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JetJot.Models
{
    public class Document : INotifyPropertyChanged
    {
        private string _title = "Untitled";
        private string _text = "";
        private int _wordGoal = 1000;

        public Guid Id { get; set; } = Guid.NewGuid();

        public string FileName => $"{Id}.txt";

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged();
                }
            }
        }

        public int WordGoal
        {
            get => _wordGoal;
            set
            {
                if (_wordGoal != value)
                {
                    _wordGoal = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
