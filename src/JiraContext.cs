using System.ComponentModel;
using System.Runtime.CompilerServices;
using JiraToDgmlDump.Annotations;

namespace JiraToDgmlDump
{
    public class JiraContext : IJiraContext, INotifyPropertyChanged
    {
        private bool _useCachedRepo;
        private string _login;
        private string _password;
        private string _uri;
        private string _project;
        private int _daysBackToFetchIssues;
        private string[] _epics;

        public string Login
        {
            get => _login;
            set
            {
                if (value == _login)
                    return;
                _login = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (value == _password)
                    return;
                _password = value;
                OnPropertyChanged();
            }
        }

        public string Uri
        {
            get => _uri;
            set
            {
                if (value == _uri)
                    return;
                _uri = value;
                OnPropertyChanged();
            }
        }

        public string Project
        {
            get => _project;
            set
            {
                if (value == _project)
                    return;
                _project = value;
                OnPropertyChanged();
            }
        }

        public int DaysBackToFetchIssues
        {
            get => _daysBackToFetchIssues;
            set
            {
                if (value == _daysBackToFetchIssues)
                    return;
                _daysBackToFetchIssues = value;
                OnPropertyChanged();
            }
        }

        public bool UseCachedRepo
        {
            get => _useCachedRepo;
            set
            {
                if (value == _useCachedRepo)
                    return;
                _useCachedRepo = value;
                OnPropertyChanged();
            }
        }

        public string[] Epics
        {
            get => _epics;
            set
            {
                if (value == _epics)
                    return;
                _epics = value;
                OnPropertyChanged();
            }
        }

        public string[] LinkTypes { get; set; }

        public JiraContext()
        {
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
