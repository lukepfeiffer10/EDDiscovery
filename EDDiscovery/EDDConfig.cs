using EDDiscovery.DB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using EDDiscovery2.Themes;

namespace EDDiscovery2
{
    public class EDDConfig
    {
        private bool _useDistances;
        private bool _EDSMLog;
        readonly public string LogIndex;
        private bool _canSkipSlowUpdates = false;
        private ObservableCollection<ITheme> _themes;

        SQLiteDBClass _db = new SQLiteDBClass();

        public EDDConfig()
        {
            LogIndex = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            SelectedTheme = new LightTheme();
        }

        public bool UseDistances
        {
            get
            {
                return _useDistances;
            }

            set
            {
                _useDistances = value;
                _db.PutSettingBool("EDSMDistances", value);
            }
        }

        public bool EDSMLog
        {
            get
            {
                return _EDSMLog;
            }

            set
            {
                _EDSMLog = value;
                _db.PutSettingBool("EDSMLog", value);
            }
        }

        public bool CanSkipSlowUpdates
        {
            get
            {
                return _canSkipSlowUpdates;
            }
            set
            {
                _canSkipSlowUpdates = value;
                _db.PutSettingBool("CanSkipSlowUpdates", value);
            }
        }

        public void Update()
        {
            _useDistances = _db.GetSettingBool("EDSMDistances", false);
            _EDSMLog = _db.GetSettingBool("EDSMLog", false);
            _canSkipSlowUpdates = _db.GetSettingBool("CanSkipSlowUpdates", false);
        }

        public ObservableCollection<ITheme> Themes
        {
            get
            {
                _themes = new ObservableCollection<ITheme> {new DarkTheme(), new LightTheme()};
                return _themes;
            }
            set { _themes = value; }
        }

        public ITheme SelectedTheme { get; set; }
    }
}
