using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using TcHmiSrv.Core;
using TcHmiSrv.Core.General;
using TcHmiSrv.Core.Tools.DynamicSymbols;
using TcHmiSrv.Core.Tools.Json.Extensions;
using ValueExtensions = TcHmiSrv.Core.Tools.Json.Extensions.ValueExtensions;

namespace SQLiteConnector
{
    class SqlConnectionSymbol : Symbol
    {
        public static TcHmiJSchemaGenerator CustomGenerator { get; } = CreateGenerator();

        public SqliteConnection SqlConnection { get; set; }

        private SqliteConnectionStringBuilder connectionString = new SqliteConnectionStringBuilder();
        private Microsoft.Data.Sqlite.SqliteConnection connection;
        private string tableName = "tempzones";

        public SqlConnectionSymbol(SqliteConnection sqlConnection, bool function) : base(CustomGenerator.Generate(sqlConnection.GetType()))
        {
            // new Symbol()
            var path = TcHmiApplication.Path;
            var curDir = Directory.GetCurrentDirectory();
            this.SqlConnection = sqlConnection;

            connectionString.Mode = SqliteOpenMode.ReadWrite;
            connectionString.DataSource = this.SqlConnection.DatabaseName + ".db";
            var fullPath = Path.Combine(curDir, connectionString.DataSource);

            connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString.ToString());

            if (!File.Exists(fullPath))
            {
                connectionString.Mode = SqliteOpenMode.ReadWriteCreate;
                connection.ConnectionString = connectionString.ToString();

                var _currentTime = DateTime.Now.Ticks;

                connection.Open();
                string sql = "create table tempzones (timestamp INTEGER ,zonename varchar(20), temperature float)";
                SqliteCommand command = new SqliteCommand(sql, connection);
                command.ExecuteNonQuery();
                sql = "insert into " + tableName + " (timestamp, zonename, temperature) values (" + _currentTime +  ",' Zone1', 32.1)";
                command = new SqliteCommand(sql, connection);
                command.ExecuteNonQuery();
                sql = "insert into " + tableName + " (timestamp, zonename, temperature) values (" + _currentTime + ",' Zone2', 35.1)";
                command = new SqliteCommand(sql, connection);
                command.ExecuteNonQuery();
                sql = "insert into " + tableName + " (timestamp, zonename, temperature) values (" + _currentTime + ",' Zone3', 36.2)";
                command = new SqliteCommand(sql, connection);
                command.ExecuteNonQuery();
                connection.Close();

                connectionString.Mode = SqliteOpenMode.ReadWrite;
                connection.ConnectionString = connectionString.ToString();

            }

        }

        private static TcHmiJSchemaGenerator CreateGenerator()
        {
            // The default JSON schema generator does not contain a 'JSchemaGenerationProvider' for enumerations by default
            // You can implement a custom 'JSchemaGenerationProvider' for enumerations, create a 'JSchemaGenerationProvider' for a specific type by calling 'TcHmiSchemaGenerator.CreateEnumGenerationProvider' or use the 'TcHmiJSchemaGenerator.DefaultEnumGenerationProvider' for all enum types
            var generator = TcHmiJSchemaGenerator.DefaultGenerator;
            generator.GenerationProviders.Add(TcHmiJSchemaGenerator.DefaultEnumGenerationProvider);
            //generator.GenerationProviders.Add()
            return generator;
        }

        // Read a value from the current machine
        protected override Value Read(Queue<string> elements)
        {
            if (elements.Count == 0)
            {
                return "This symbol is not meant to be directly read or subscribed to";
            }

            // Get the name of the requested sub-element
            var element = elements.Dequeue();

            switch (element)
            {
                case "DatabaseName":
                    return this.SqlConnection.DatabaseName;

                default:
                    return "This symbol is not meant to be directly read or subscribed to";
            }
        }

        // Write a value to the current machine
        protected override Value Write(Queue<string> elements, Value value)
        {

            if (elements.Count == 0)
                throw new ArgumentException("Missing elements because the entire handler cannot be overwritten.", nameof(elements));
            // TcHmiApplication.Context.Session.User

            var element = elements.Dequeue();

            if (elements.Count > 0)
                throw new ArgumentException("Too many elements.", nameof(elements));

            switch (element)
            {
                case "M_ReadEntries":
                    return readEntries(value);

                case "M_WriteEntry":
                    return writeEntry(value);

                default:
                    throw new ArgumentException(string.Concat("Symbol element is read-only or unkown: ", element), nameof(elements));
            }
        }

        private Value writeEntry(Value newEntry)
        {
            if (newEntry != null)
            {

                TempZoneEntry tempzoneentry = JsonConvert.DeserializeObject<TempZoneEntry>(newEntry.ToJson());

                long _ticks = DateTime.Parse(tempzoneentry.timestamp).Ticks;


                // Query string to pass as a stored procedure call to SQL
                string sql = $"insert into {tableName} (timestamp, zonename, temperature) values ({_ticks}, '{tempzoneentry.zonename}', {tempzoneentry.temperature})";

                using (connection)
                {
                    using (SqliteCommand command = new SqliteCommand(sql, connection))
                    {

                        connection.Open();
                        command.ExecuteNonQuery();
                        connection.Close();
                    }
                }
            }

            return new Value(ErrorValue.HMI_SUCCESS.ToString());
        }


        private Value readEntries(Value value)
        {
            string sql = "SELECT rowid, * FROM " + tableName;

            List<TempZoneEntry> tempzoneentries = new List<TempZoneEntry>();

            using (connection)
            {
                using (SqliteCommand command = new SqliteCommand(sql, connection))
                {
                    connection.Open();
                    SqliteDataReader reader = command.ExecuteReader();
                    try
                    {
                        if (reader.HasRows)
                        {
                            // Each Row
                            while (reader.Read())
                            {
                                TempZoneEntry tempzoneentry = new TempZoneEntry();

                                tempzoneentry.id = reader.GetInt64(0);
                                tempzoneentry.timestamp = new DateTime(reader.GetInt64(1)).ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                                tempzoneentry.zonename = reader.IsDBNull(2) ? null : reader.GetString(2);
                                tempzoneentry.temperature = reader.GetFloat(3);
                                
                                // Addiing the item or the list
                                if (tempzoneentry != null)
                                {
                                    tempzoneentries.Add(tempzoneentry);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw new TcHmiException(TcHmiAsyncLogger.Localize("ERROR_DATABASE_QUERY", e.ToString()), ErrorValue.HMI_E_FAIL);

                    }

                    finally
                    {
                        reader.Close();

                    }
                }
                //Encoding the object and sending it back to the HMI
                // command.ReadValue = TcHmiSerializer.SerializeObject(data.inventoryList);
                Globals.diagnostics.queryCount++;
                TcHmiAsyncLogger.SendAsync(Severity.Info, "QUERY_SUCCESS");
                return ValueExtensions.FromJson(JsonConvert.SerializeObject(tempzoneentries));
            }


        }
    }
}
