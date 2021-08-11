using System;
using System.Collections.Generic;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;


namespace SQLiteConnector
{

    // Validators to handle when config values change
    // Remove a symbol from the symbol list, or prevent a duplicate database name, or prevent an invalid name
    public class ConfigException : Exception
    {
        public ConfigException(string message) : base(message)
        {
        }

        public ConfigException(string message, Exception innerException) : base(message, innerException)
        {
        }

        // Ignore the full stack that System.Exception prints when using base.ToString(), only return message
        public override string ToString()
        {
            return this.Message;
            
        }

    }
    
    
    public class ConfigValidators
    {
        public static List<string> getDatabaseConfig()
        {
            List<string> tempList = new List<string>();
            var databases = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, StringConstants.DATABASES);

            foreach (Value value in databases)
            {
                tempList.Add(value.ToString());
            }
            return tempList;

        }

        /// <summary>
        /// Check the existing config connection strings against the current symbol list provider for duplicates
        /// Returns true if duplicate found
        /// </summary>
        public static bool checkDupeDatabaseConnections()
        {
            var currentDatabaseNames = getDatabaseConfig();
            bool result = false;
            foreach (string databaseName in currentDatabaseNames)
            {
                object value = null;

                if (!databaseName.Equals(String.Empty))
                {

                    if (SQLiteConnector.provider.ContainsKey(value.ToString()))
                    {
                        TcHmiAsyncLogger.Send(Severity.Error, "ERROR_DUPE_DATABASE", databaseName);
                        result = true;
                    }

                }
            }
            return result;
        }

        /// <summary>
        /// Check the existing config connection strings against the input database name for existing database config targets
        /// Return true id duplicate found
        /// </summary>
        public static bool checkDupeDatabaseConnections(string databaseName)
        {
            var currentDatabaseNames = getDatabaseConfig();

            foreach (string _databaseName in currentDatabaseNames)
            {
                if (!String.IsNullOrEmpty(databaseName))
                {
                    if (databaseName.Equals(_databaseName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool validateChangedDatabaseConfig(string databaseName)
        {

            // Validate for database entry being not null or empty
            if (!String.IsNullOrEmpty(databaseName))
            {
                // connection screen has a Database target entry
            }
            else
            {
                TcHmiAsyncLogger.Send(Severity.Error, "ERROR_INVALID_CONNECTION", databaseName.ToString());
                throw new ConfigException(TcHmiAsyncLogger.Localize("ERROR_INVALID_DATABASE_NAME", databaseName.ToString()));
            }

            // Validate for database entry being unique within the config list
            if (!checkDupeDatabaseConnections(databaseName))
            {
                // no duplicates found, incoming database name is unique
            }
            else
            {
                TcHmiAsyncLogger.Send(Severity.Error, "ERROR_INVALID_CONNECTION", databaseName.ToString());
                throw new ConfigException(TcHmiAsyncLogger.Localize("ERROR_DUPE_CONFIG", databaseName.ToString()));
            }

            return true;

        }

        public static bool validateChangedDatabaseConfig(List<string> databaseNames)
        {

            // Validate each string in list
            foreach (string databaseName in databaseNames)
            {

                if (!String.IsNullOrEmpty(databaseName))
                {
                    // connection screen has a Database target entry
                }
                else
                {
                    TcHmiAsyncLogger.Send(Severity.Error, "ERROR_INVALID_CONNECTION", databaseName.ToString());
                    throw new ConfigException(TcHmiAsyncLogger.Localize("ERROR_INVALID_DATABASE_NAME", databaseName.ToString()));
                }
            }
            return true;

        }
    }
}
