using System;
using System.Collections.Generic;
using System.Text;

namespace SQLiteConnector
{
    static class Globals
    {

        public static Diagnostics diagnostics = new Diagnostics();
        
    }

    public class Diagnostics
    {

        public Diagnostics()
        {

        }

        private int _databaseCount = 0;
        private object _databaseCountLock = new object();

        private int _queryCount = 0;
        private object _queryCountLock = new object();

        public int databaseCount
        {
            get
            {
                lock (_databaseCountLock)
                {
                    return _databaseCount;
                }
            }

            set
            {
                lock (_databaseCountLock)
                {
                    _databaseCount = value;
                }
            }
        }

        public int queryCount
        {
            get
            {
                lock (_queryCountLock)
                {
                    return _queryCount;
                }
            }

            set
            {
                lock (_queryCountLock)
                {
                    _queryCount = value;
                }
            }
        }
    }

}
