﻿using System;
using System.Collections.Generic;
using System.Text.Json;

using Microsoft.Data.Sqlite;

using PInvoke.Common.Models;

namespace PInvoke.Storage
{
    public class SqliteStorage
    {
        private SqliteConnection sqliteConnection;

        public SqliteStorage(string databasePath)
        {
            sqliteConnection = new SqliteConnection($"Data Source={databasePath}");
            sqliteConnection.Open();

            void executeQuery(string query)
            {
                using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
                {
                    sqliteCommand.CommandText = query;
                    sqliteCommand.ExecuteNonQuery();
                }
            }

            foreach (string table in new[] { "methods", "structures", "enumerations" })
            {
                executeQuery($"CREATE TABLE IF NOT EXISTS {table} (source VARCHAR NOT NULL, library VARCHAR NOT NULL, name VARCHAR NOT NULL, content VARCHAR);");
                executeQuery($"CREATE INDEX IF NOT EXISTS idx_{table}_source ON {table} (source);");
                executeQuery($"CREATE INDEX IF NOT EXISTS idx_{table}_source_library ON {table} (source, library);");
                executeQuery($"CREATE INDEX IF NOT EXISTS idx_{table}_source_library_name ON {table} (source, library, name);");
            }
        }

        public IEnumerable<int> Insert(IEnumerable<MethodData> methods, int batchSize = 1000)
        {
            List<string> methodQueries = new List<string>(batchSize);
            int insertedMethods = 0;

            int flushMethods()
            {
                if (methodQueries.Count == 0)
                    return insertedMethods;

                string bulkQuery = "INSERT INTO methods VALUES " + string.Join(", ", methodQueries);

                using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
                {
                    sqliteCommand.CommandText = bulkQuery;
                    sqliteCommand.ExecuteNonQuery();
                }

                insertedMethods += methodQueries.Count;
                methodQueries.Clear();

                return insertedMethods;
            }

            IEnumerator<MethodData> methodEnumerator = methods.GetEnumerator();

            while (methodEnumerator.MoveNext())
            {
                MethodData method = methodEnumerator.Current;

                byte[] contentBytes = JsonSerializer.SerializeToUtf8Bytes(method.Content);
                string contentString = Convert.ToBase64String(contentBytes);

                methodQueries.Add($"('{method.Source}', '{method.Library}', '{method.Name}', '{contentString}')");

                if (methodQueries.Count == methodQueries.Capacity)
                    yield return flushMethods();
            }

            yield return flushMethods();
        }
        public IEnumerable<int> Insert(IEnumerable<EnumerationData> enumerations, int batchSize = 1000)
        {
            List<string> enumerationQueries = new List<string>(batchSize);
            int insertedEnumerations = 0;

            int flushEnumerations()
            {
                if (enumerationQueries.Count == 0)
                    return insertedEnumerations;

                string bulkQuery = "INSERT INTO enumerations VALUES " + string.Join(", ", enumerationQueries);

                using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
                {
                    sqliteCommand.CommandText = bulkQuery;
                    sqliteCommand.ExecuteNonQuery();
                }

                insertedEnumerations += enumerationQueries.Count;
                enumerationQueries.Clear();

                return insertedEnumerations;
            }

            IEnumerator<EnumerationData> enumerationEnumerator = enumerations.GetEnumerator();

            while (enumerationEnumerator.MoveNext())
            {
                EnumerationData enumeration = enumerationEnumerator.Current;

                byte[] contentBytes = JsonSerializer.SerializeToUtf8Bytes(enumeration.Content);
                string contentString = Convert.ToBase64String(contentBytes);

                enumerationQueries.Add($"('{enumeration.Source}', '{enumeration.Library}', '{enumeration.Name}', '{contentString}')");

                if (enumerationQueries.Count == enumerationQueries.Capacity)
                    yield return flushEnumerations();
            }

            yield return flushEnumerations();
        }
        public IEnumerable<int> Insert(IEnumerable<StructureData> structures, int batchSize = 1000)
        {
            List<string> structureQueries = new List<string>(batchSize);
            int insertedStructures = 0;

            int flushStructures()
            {
                if (structureQueries.Count == 0)
                    return insertedStructures;

                string bulkQuery = "INSERT INTO structures VALUES " + string.Join(", ", structureQueries);

                using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
                {
                    sqliteCommand.CommandText = bulkQuery;
                    sqliteCommand.ExecuteNonQuery();
                }

                insertedStructures += structureQueries.Count;
                structureQueries.Clear();

                return insertedStructures;
            }

            IEnumerator<StructureData> structureEnumerator = structures.GetEnumerator();

            while (structureEnumerator.MoveNext())
            {
                StructureData structure = structureEnumerator.Current;

                byte[] contentBytes = JsonSerializer.SerializeToUtf8Bytes(structure.Content);
                string contentString = Convert.ToBase64String(contentBytes);

                structureQueries.Add($"('{structure.Source}', '{structure.Library}', '{structure.Name}', '{contentString}')");

                if (structureQueries.Count == structureQueries.Capacity)
                    yield return flushStructures();
            }

            yield return flushStructures();
        }

        public IEnumerable<SourceData> GetSources()
        {
            using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
            {
                sqliteCommand.CommandText = "SELECT DISTINCT source FROM methods";

                using (SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
                {
                    while (sqliteDataReader.Read())
                    {
                        string source = sqliteDataReader.GetString(0);

                        yield return new SourceData()
                        {
                            Name = source
                        };
                    }
                }
            }
        }
        public SourceData GetSource(string source)
        {
            using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
            {
                sqliteCommand.CommandText = "SELECT source FROM methods WHERE source = @source";
                sqliteCommand.Parameters.AddWithValue("source", source);

                using (SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
                {
                    if (!sqliteDataReader.Read())
                        return null;

                    return new SourceData()
                    {
                        Name = source
                    };
                }
            }
        }

        public IEnumerable<LibraryData> GetLibraries()
        {
            using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
            {
                sqliteCommand.CommandText = "SELECT DISTINCT source, library FROM methods";

                using (SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
                {
                    while (sqliteDataReader.Read())
                    {
                        string source = sqliteDataReader.GetString(0);
                        string library = sqliteDataReader.GetString(1);

                        yield return new LibraryData()
                        {
                            Source = source,
                            Name = library
                        };
                    }
                }
            }
        }
        public IEnumerable<LibraryData> GetLibraries(string source)
        {
            using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
            {
                sqliteCommand.CommandText = "SELECT DISTINCT library FROM methods WHERE source = @source";
                sqliteCommand.Parameters.AddWithValue("source", source);

                using (SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
                {
                    while (sqliteDataReader.Read())
                    {
                        string library = sqliteDataReader.GetString(0);

                        yield return new LibraryData()
                        {
                            Source = source,
                            Name = library
                        };
                    }
                }
            }
        }
        public LibraryData GetLibrary(string source, string library)
        {
            using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
            {
                sqliteCommand.CommandText = "SELECT library FROM methods WHERE source = @source AND library = @library";
                sqliteCommand.Parameters.AddWithValue("source", source);
                sqliteCommand.Parameters.AddWithValue("library", library);

                using (SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
                {
                    if (!sqliteDataReader.Read())
                        return null;

                    return new LibraryData()
                    {
                        Source = source,
                        Name = library
                    };
                }
            }
        }

        public IEnumerable<MethodData> GetMethods(string source)
        {
            using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
            {
                sqliteCommand.CommandText = "SELECT library, name FROM methods WHERE source = @source";
                sqliteCommand.Parameters.AddWithValue("source", source);

                using (SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
                {
                    while (sqliteDataReader.Read())
                    {
                        string library = sqliteDataReader.GetString(0);
                        string name = sqliteDataReader.GetString(1);

                        yield return new MethodData()
                        {
                            Source = source,
                            Library = library,
                            Name = name
                        };
                    }
                }
            }
        }
        public IEnumerable<MethodData> GetMethods(string source, string library)
        {
            using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
            {
                sqliteCommand.CommandText = "SELECT name, content FROM methods WHERE source = @source AND library = @library";
                sqliteCommand.Parameters.AddWithValue("source", source);
                sqliteCommand.Parameters.AddWithValue("library", library);

                using (SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
                {
                    while (sqliteDataReader.Read())
                    {
                        string name = sqliteDataReader.GetString(0);

                        string contentString = sqliteDataReader.GetString(1);
                        byte[] contentBytes = Convert.FromBase64String(contentString);
                        Method content = JsonSerializer.Deserialize<Method>(contentBytes);

                        yield return new MethodData()
                        {
                            Source = source,
                            Library = library,
                            Name = name,
                            Content = content
                        };
                    }
                }
            }
        }
        public MethodData GetMethod(string source, string library, string method)
        {
            using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
            {
                sqliteCommand.CommandText = "SELECT content FROM methods WHERE source = @source AND library = @library AND name = @name";
                sqliteCommand.Parameters.AddWithValue("source", source);
                sqliteCommand.Parameters.AddWithValue("library", library);
                sqliteCommand.Parameters.AddWithValue("name", method);

                using (SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
                {
                    if (!sqliteDataReader.Read())
                        return null;

                    string contentString = sqliteDataReader.GetString(1);
                    byte[] contentBytes = Convert.FromBase64String(contentString);
                    Method content = JsonSerializer.Deserialize<Method>(contentBytes);

                    return new MethodData()
                    {
                        Source = source,
                        Library = library,
                        Name = method,
                        Content = content
                    };
                }
            }
        }

        public IEnumerable<EnumerationData> GetEnumerations(string source)
        {
            using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
            {
                sqliteCommand.CommandText = "SELECT library, name FROM enumerations WHERE source = @source";
                sqliteCommand.Parameters.AddWithValue("source", source);

                using (SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
                {
                    while (sqliteDataReader.Read())
                    {
                        string library = sqliteDataReader.GetString(0);
                        string name = sqliteDataReader.GetString(1);

                        yield return new EnumerationData()
                        {
                            Source = source,
                            Library = library,
                            Name = name
                        };
                    }
                }
            }
        }
        public IEnumerable<EnumerationData> GetEnumerations(string source, string library)
        {
            using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
            {
                sqliteCommand.CommandText = "SELECT name, content FROM enumerations WHERE source = @source AND library = @library";
                sqliteCommand.Parameters.AddWithValue("source", source);
                sqliteCommand.Parameters.AddWithValue("library", library);

                using (SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
                {
                    while (sqliteDataReader.Read())
                    {
                        string name = sqliteDataReader.GetString(0);

                        string contentString = sqliteDataReader.GetString(1);
                        byte[] contentBytes = Convert.FromBase64String(contentString);
                        Enumeration content = JsonSerializer.Deserialize<Enumeration>(contentBytes);

                        yield return new EnumerationData()
                        {
                            Source = source,
                            Library = library,
                            Name = name,
                            Content = content
                        };
                    }
                }
            }
        }
        public EnumerationData GetEnumeration(string source, string library, string enumeration)
        {
            using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
            {
                sqliteCommand.CommandText = "SELECT content FROM enumerations WHERE source = @source AND library = @library AND name = @name";
                sqliteCommand.Parameters.AddWithValue("source", source);
                sqliteCommand.Parameters.AddWithValue("library", library);
                sqliteCommand.Parameters.AddWithValue("name", enumeration);

                using (SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader())
                {
                    if (!sqliteDataReader.Read())
                        return null;

                    string contentString = sqliteDataReader.GetString(1);
                    byte[] contentBytes = Convert.FromBase64String(contentString);
                    Enumeration content = JsonSerializer.Deserialize<Enumeration>(contentBytes);

                    return new EnumerationData()
                    {
                        Source = source,
                        Library = library,
                        Name = enumeration,
                        Content = content
                    };
                }
            }
        }
    }
}
