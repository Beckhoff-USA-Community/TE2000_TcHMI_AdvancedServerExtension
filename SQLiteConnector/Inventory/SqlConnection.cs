using System;
using System.Collections.Generic;
using System.Text;

namespace SQLiteConnector
{

    class TempZoneEntryList : List<TempZoneEntry>
    {

    }

    class SqliteConnection
    {
        public SqliteConnection(string Databasename)
        {
            _DatabaseName = Databasename;
        }

        private readonly object _DatabaseNameLock = new object();
        private string _DatabaseName;

        private readonly object _M_ReadEntriesLock = new object();
        private TempZoneEntryList _M_ReadEntries = new TempZoneEntryList();

        private readonly object _M_WriteEntryLock = new object();
        private TempZoneEntry _M_WriteEntry = new TempZoneEntry();

        private readonly object _TempZoneEntryLock = new object();
        private TempZoneEntry _TempZoneEntry = new TempZoneEntry();

        public TempZoneEntry TempZoneEntry
        {
            get
            {
                lock (_TempZoneEntry)
                {
                    return _TempZoneEntry;
                }
            }
            set
            {
                lock (_TempZoneEntry)
                {
                    _TempZoneEntry = value;
                }
            }
        }

        public string DatabaseName
        {
            get
            {
                lock (_DatabaseNameLock)
                {
                    return _DatabaseName;
                }
            }
            set
            {
                lock (_DatabaseNameLock)
                {
                    _DatabaseName = value;
                }
            }
        }

        public TempZoneEntryList M_ReadEntries
        {
            get
            {
                lock (_M_ReadEntriesLock)
                {
                    return _M_ReadEntries;
                }
            }
           //set
           //{
           //    lock (_M_ReadEntriesLock)
           //    {
           //        _M_ReadEntries = value;
           //    }
           //}
        }

        public TempZoneEntry M_WriteEntry
        {
            get
            {
                lock (_M_WriteEntry)
                {
                    return _M_WriteEntry;
                }
            }
            set
            {
                lock (_M_WriteEntry)
                {
                    _M_WriteEntry = value;
                }
            }
        }
    }
}
