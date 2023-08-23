//-----------------------------------------------------------------------
// LICENSE
// Zero-Clause BSD
// Permission to use, copy, modify, and/or distribute this software for any purpose with or without fee is hereby granted.
//
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS.IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TcHmiSrv.Core;
using TcHmiSrv.Core.Extensions;
using TcHmiSrv.Core.Listeners;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Tools.Settings;
using TcHmiSrv.Core.Tools.DynamicSymbols;
using TcHmiSrv.Core.Tools.Json.Extensions;
using TcHmiSrv.Core.Tools.Management;
using ValueExtensions = TcHmiSrv.Core.Tools.Json.Extensions.ValueExtensions;

// Declare the default type of the server extension
[assembly: TcHmiSrv.Core.Tools.TypeAttribute.ServerExtensionType(typeof(SQLiteConnector.SQLiteConnector))]


// This server extension is an example of the following:

// Dynamic Symbol Lists- Similair to how the ADS extension can update its symbol list from the target ADS runtime
// Dynamic Schema Generation - Importing definitions from remote symbols into the server for defining datatypes
// Additional Listeners - Default templates only have a request listener. This sample includes a Config listener based on any configurations updates 
// Server side events and event logging - Utilizing the built in TcHmiEventGrid Control to view server side events with more details
// Server Extension Diagnostics symbol - viewing extension diagnostic data within the config panel and symbol


namespace SQLiteConnector
{


    // Represents the default type of the TwinCAT HMI server extension.
    public class SQLiteConnector : IServerExtension
    {

        private object _shutdownLock = new object();
        private bool _isShuttingDown = false;

        private readonly RequestListener requestListener = new RequestListener();
        private readonly ShutdownListener shutdownListener = new ShutdownListener();

        // 2.0.0.0 API, listener is automatically register by default, with default settings, similair to Request and Shutdown listeners
        // ResgiterListener does not need to be called. See API changes for more info
        // TwinCAT\Functions\TE2000-HMI-Engineering\Infrastructure\TcHmiServer\docs
        private readonly ConfigListener _configListener = new ConfigListener(); 

        private readonly PerformanceCounter _cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private DateTime _startup;


        // Create Dynamic Symbol vars symbols provider and schema generator
        public static DynamicSymbolsProvider provider = null;
        private TcHmiJSchemaGenerator generator = TcHmiJSchemaGenerator.DefaultGenerator;

        // Initializes the TwinCAT HMI server extension.
        public ErrorValue Init()
        {
            try
            {
                // Uncomment to debug init phase
                //TcHmiApplication.AsyncDebugHost.WaitForDebugger(true);
                _startup = DateTime.Now;

                // Add listener handlers to existing registered listeners
                this.requestListener.OnRequest += this.OnRequest;
                this.shutdownListener.OnShutdown += this.OnShutdown;
                this._configListener.BeforeChange += this.BeforeChange;
                this._configListener.OnChange += this.OnChange;

                
                // Create a new empty 'DynamicSymbolsProvider'
                SQLiteConnector.provider = new DynamicSymbolsProvider();

                this.generator.GenerationProviders.Add(TcHmiJSchemaGenerator.DefaultEnumGenerationProvider);

                // Add symbols to the symbol list based on the extension config
                initDatabaseSymbols();

                // The number of symbols in the symbol list, which is only valid sql connection strings
                Globals.diagnostics.databaseCount = SQLiteConnector.provider.Count;

                TcHmiAsyncLogger.Send(Severity.Info, "MESSAGE_INIT");
                return ErrorValue.HMI_SUCCESS;
            }
            catch (Exception ex)
            {
                TcHmiAsyncLogger.Send(Severity.Error, "ERROR_INIT", ex.ToString());
                return ErrorValue.HMI_E_EXTENSION_LOAD;
            }
        }

        private void BeforeChange(object sender, TcHmiSrv.Core.Listeners.ConfigListenerEventArgs.BeforeChangeEventArgs e)
        {
            string path = e.Path;
            
            Value value = e.Value;

            // Use pattern to find path without array index if value changed/added was an index in an array of <>
            // Example: User adds a 4th item in the list, path is passed in as DATABASES[3]. 
            // Needed for array type config parameters to view just the key name
            string pattern = @"\[[^]]*\]";
            path = Regex.Replace(path, pattern, String.Empty);
            var currentDatabaseNames = ConfigValidators.getDatabaseConfig();

            try
            {
                // If the changed config = SQL Strings
                if (path.Equals(StringConstants.DATABASES))
                {

                    // If it is a single addition to the string array
                    if (value.Type == TcHmiSrv.Core.ValueType.String)
                    {
                        string changedValue = value.ToString();

                        // Validate the incoming single string addition against syntax, database value, and duplicate 
                        ConfigValidators.validateChangedDatabaseConfig(changedValue);
                    }

                    // Else it is a multi addition, or an entry was deleted and the original config minus the deletion is sent back here
                    else
                    {
                        // Convert the incoming value to a list of strings
                        List<string> changedValues = JsonConvert.DeserializeObject<List<string>>(value.ToJson());

                        // Validate the incoming single string addition against syntax, database value, and duplicate 
                        ConfigValidators.validateChangedDatabaseConfig(changedValues);
                    }
                }
            }
            catch (Exception ex)
            {
                // throw original excption
                throw;

            }


        }


        private void OnChange(object sender, TcHmiSrv.Core.Listeners.ConfigListenerEventArgs.OnChangeEventArgs e)
        {

            // Onchange would need to handle new entries in the config, and delete symbols no longer in the config

            if (e.Path.Equals(StringConstants.DATABASES))
            {

                syncSqlConfigAndSymbols();

            }
        }

        // Called when a client requests a symbol from the domain of the TwinCAT HMI server extension.
        private void OnRequest(object sender, TcHmiSrv.Core.Listeners.RequestListenerEventArgs.OnRequestEventArgs e)
        {
            ErrorValue ret = ErrorValue.HMI_SUCCESS;
            Context context = e.Context;
            CommandGroup commands = e.Commands;


            try
            {
                commands.Result = (uint)ExtensionErrorValue.HmiExtSuccess;
                string mapping = string.Empty;

                // Provider handle commands handles all Dynamic Symbol access. 
                // All symbols that don't match will be returned in this commands list here for handling
                foreach (Command command in provider.HandleCommands(commands))
                {
                    mapping = command.Mapping;

                    try
                    {
                        // Use the mapping to check which command is requested
                        switch (mapping)
                        {

                            case "Diagnostics":
                                ret = getDiagnostics(command);
                                break;

                            default:
                                ret = ErrorValue.HMI_E_EXTENSION;
                                break;
                        }

                        // if (ret != ErrorValue.HMI_SUCCESS)
                        //   Do something on error
                    }
                    catch (Exception ex)
                    {
                        command.ExtensionResult = Convert.ToUInt32(ExtensionErrorValue.HmiExtFail);
                        command.ResultString = TcHmiAsyncLogger.Localize(context, "ERROR_CALL_COMMAND", new string[] { mapping, ex.ToString() });
                    }
                }
            }
            catch (Exception ex)
            {
                commands.Result = Convert.ToUInt32(ExtensionErrorValue.HmiExtFail);
                throw new TcHmiException(ex.ToString(), (ret == ErrorValue.HMI_SUCCESS) ? ErrorValue.HMI_E_EXTENSION : ret);
            }
        }

        // Called when the server is shutting down. After exiting this function the extension dll will be unloaded.
        private void OnShutdown(object sender, TcHmiSrv.Core.Listeners.ShutdownListenerEventArgs.OnShutdownEventArgs e)

        {
            // If the extension does not shutdown after 10 seconds (blocking threads) OnShutdown will be called again
            lock (_shutdownLock)
            {
                if (_isShuttingDown)
                {

                }

                _isShuttingDown = true;

                Context context = e.Context;

                try
                {

                    // disconnect from remote DB cleanly

                }
                catch (Exception ex)
                {
                    TcHmiAsyncLogger.Send(Severity.Error, "ERROR_SHUTDOWN", ex.ToString());

                }
            }
        }

        private ErrorValue getDiagnostics(Command command)
        {


            var diagObject = new Value
            {
                { "cpuUsage", Math.Truncate(100*_cpuUsage.NextValue())/100 },
                { "sinceStartup", DateTime.Now - _startup },
                { "databaseCount", Globals.diagnostics.databaseCount},
                { "queryCount", Globals.diagnostics.queryCount }
            };

            // ResolveBy allows subsymbols of objects to be accessed directly
            
            command.ReadValue = diagObject.ResolveBy(command.Path);
            command.ExtensionResult = Convert.ToUInt32(ExtensionErrorValue.HmiExtSuccess);
            return ErrorValue.HMI_SUCCESS;
        }


        /// <summary>
        /// Returns the string values of SqlConnectionString from the Extension Config
        /// </summary>
        private List<string> GetServerConnectionStrings()
        {
            var connectionStringsValues = TcHmiApplication.AsyncHost.GetConfigValue(TcHmiApplication.Context, StringConstants.DATABASES);
            var connectionStrings = new List<string>();
            if (connectionStringsValues != null)
            {
                foreach (Value value in connectionStringsValues)
                {
                    connectionStrings.Add(value.ToString());
                }
                return connectionStrings;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Synchronize the database connection string list to the database symbols list. If one is not present in the other, update.
        /// If symbol list contains a symbol not in the config after a config change, delete the symbol
        /// If after a config list change the list contains a target database not in the symbol list, add a new symbol
        /// </summary>
        private void syncSqlConfigAndSymbols()
        {

            var currentDifferences = diffSqlConfigandSymbols();

            foreach (string removedDatabaseName in currentDifferences.presentInSymListNotConfig)
            {
                if (!String.IsNullOrEmpty(removedDatabaseName))
                {
                    // Add a new machine to the dynamic symbols provider
                    provider.Remove(removedDatabaseName);
                }
            }

            foreach (string addedDatabaseName in currentDifferences.presentInConfigNotSymlist)
            {
                if (!String.IsNullOrEmpty(addedDatabaseName))
                {
                    // Add a new machine to the dynamic symbols provider
                    provider.Add(addedDatabaseName, new SqlConnectionSymbol(new SqliteConnection(addedDatabaseName), true));
                }
            }



        }

        /// <summary>
        /// Returns an object containing any differences between the config list and the symbol list
        /// </summary>
        private SymbolConfigDifferences diffSqlConfigandSymbols()
        {

            var databaseNamesFromConfig = ConfigValidators.getDatabaseConfig();

            List<string> databaseNamesFromSymbolProvider = new List<string>();

            foreach (SqlConnectionSymbol databaseName in provider.Values)
            {
                databaseNamesFromSymbolProvider.Add(databaseName.SqlConnection.DatabaseName);
            }

            var differences = new SymbolConfigDifferences();
            differences.presentInConfigNotSymlist = databaseNamesFromConfig.Except(databaseNamesFromSymbolProvider).ToList();
            differences.presentInSymListNotConfig = databaseNamesFromSymbolProvider.Except(databaseNamesFromConfig).ToList();

            return differences;

        }


        private ErrorValue initDatabaseSymbols()
        {
            var databaseNamesFromConfig = ConfigValidators.getDatabaseConfig();

            foreach (string databaseName in databaseNamesFromConfig)
            {

                // test for database target entry
                if (!String.IsNullOrEmpty(databaseName))
                {

                    // Test for database entry already present from previous database entry
                    if (provider.ContainsKey(databaseName))
                    {
                        TcHmiAsyncLogger.Send(Severity.Error, "ERROR_CONNECTIONS_INIT", databaseName, "Duplicate Database Entry");
                        throw new ConfigException(TcHmiAsyncLogger.Localize("ERROR_CONNECTIONS_INIT", databaseName, "Duplicate Database Entry"));
                    }
                    else
                    {
                        // Add a new machine to the dynamic symbols provider
                        provider.Add(databaseName, new SqlConnectionSymbol(new SqliteConnection(databaseName), true));
                    }

                }
                else
                {
                    TcHmiAsyncLogger.Send(Severity.Error, "ERROR_INVALID_CONNECTION", databaseName);
                    throw new ConfigException(TcHmiAsyncLogger.Localize("ERROR_INVALID_CONNECTION", databaseName));
                }
            }

            TcHmiAsyncLogger.Send(Severity.Info, TcHmiAsyncLogger.Localize("MESSAGE_CONNECTIONS_INIT"));
            return ErrorValue.HMI_SUCCESS;

        }



        public class SymbolConfigDifferences
        {
            public SymbolConfigDifferences()
            {
                presentInConfigNotSymlist = new List<string>();
                presentInSymListNotConfig = new List<string>();
            }

            public List<string> presentInConfigNotSymlist;
            public List<string> presentInSymListNotConfig;


        }
    }
}


