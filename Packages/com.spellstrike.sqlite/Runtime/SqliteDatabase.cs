using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using UnityEngine;

namespace SpellStrike.SQLite
{
    public class SqliteDatabase
    {
        public bool IsAvailable { get; private set; }
        private IDbConnection connection;

        private static readonly string[] providerTypeNames = new string[]
        {
            "Mono.Data.Sqlite.SqliteConnection, Mono.Data.Sqlite",
            "Mono.Data.Sqlite.SqliteConnection, Mono.Data.SqliteClient",
            "System.Data.SQLite.SQLiteConnection, System.Data.SQLite"
        };

        public void Initialize(string databasePath)
        {
            if (string.IsNullOrEmpty(databasePath))
            {
                Debug.LogError("SQLite database path is empty.");
                IsAvailable = false;
                return;
            }

            Type connectionType = null;
            foreach (var typeName in providerTypeNames)
            {
                connectionType = Type.GetType(typeName);
                if (connectionType != null)
                    break;
            }

            if (connectionType == null)
            {
                Debug.LogWarning("SQLite provider type not found in runtime. SQLite support is unavailable.");
                IsAvailable = false;
                return;
            }

            try
            {
                object instance = Activator.CreateInstance(connectionType, $"URI=file:{databasePath}");
                connection = instance as IDbConnection;
                if (connection == null)
                {
                    Debug.LogWarning("SQLite connection type does not implement IDbConnection.");
                    IsAvailable = false;
                    return;
                }

                connection.Open();
                IsAvailable = true;
                Debug.Log($"SQLite database opened at: {databasePath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SQLite initialization failed: {ex.Message}");
                IsAvailable = false;
            }
        }

        public void Close()
        {
            if (connection != null)
            {
                connection.Close();
                connection.Dispose();
                connection = null;
            }

            IsAvailable = false;
        }

        public void ExecuteNonQuery(string sql)
        {
            if (!IsAvailable || connection == null) return;

            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        public object ExecuteScalar(string sql)
        {
            if (!IsAvailable || connection == null) return null;

            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = sql;
                return command.ExecuteScalar();
            }
        }

        public List<Dictionary<string, object>> ExecuteQuery(string sql)
        {
            var result = new List<Dictionary<string, object>>();
            if (!IsAvailable || connection == null) return result;

            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = sql;
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }
                        result.Add(row);
                    }
                }
            }

            return result;
        }

        public static string Escape(string value)
        {
            if (value == null) return string.Empty;
            return value.Replace("'", "''");
        }
    }
}
