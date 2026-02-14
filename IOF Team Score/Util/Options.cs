using System.ComponentModel;

namespace IOF_Team_Score.Util
{
    public class Options : INotifyPropertyChanged
    {
        private string _eventType;

        public string EventType { get => _eventType; 
            set
            {
                _eventType = value;
                OnPropertyChanged(nameof(EventType));
            } 
        }

        public bool ExportCSS { get; set; } = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
