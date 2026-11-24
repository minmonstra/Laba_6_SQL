using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Laba_6
{ 
    internal record DbStatistics(int Apartments, int Rooms, double TotalArea, double AvgTemperature, int LightsOn);

    internal static class DatabaseManager
    {
        private static readonly string pathToDBFile = Path.Combine(AppContext.BaseDirectory, "lab6.db");
        private static readonly string scriptsFolder = Path.Combine(AppContext.BaseDirectory, "Database");

        public static string PathToDBFile => pathToDBFile;

        private static async Task<SqliteConnection> OpenAsync(bool readOnly = false)
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = pathToDBFile,
                Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite
            };

            var connection = new SqliteConnection(builder.ToString());
            await connection.OpenAsync();

             
            connection.CreateFunction("LOWER_U", (string? s) => s?.ToLowerInvariant());

           
            using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            await pragma.ExecuteNonQueryAsync();

            return connection;
        }

        
        public static void Initialize()
        {
            if (File.Exists(pathToDBFile)) return;

            try
            {
                ExecuteScript("schema.sql");
                ExecuteScript("seed.sql");
            }
            catch
            {
          
                DeleteDbFiles();
                throw;
            }
        }

      
        public static void ResetDatabase()
        {
            DeleteDbFiles();
            Initialize();
        }

        private static void DeleteDbFiles()
        {
            SqliteConnection.ClearAllPools(); // иначе файл может быть занят пулом соединений
            foreach (string file in new[] { pathToDBFile, pathToDBFile + "-journal", pathToDBFile + "-wal", pathToDBFile + "-shm" })
            {
                if (File.Exists(file)) File.Delete(file);
            }
        }

        private static void ExecuteScript(string fileName)
        {
            string path = Path.Combine(scriptsFolder, fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Не найден SQL-скрипт: {path}", path);

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = pathToDBFile,
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            using var connection = new SqliteConnection(builder.ToString());
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = File.ReadAllText(path);
            command.ExecuteNonQuery();
        }
 

        public static async Task<ObservableCollection<Apartment>> GetAllApartments()
        {
            var aparts = new ObservableCollection<Apartment>();

            await using var connection = await OpenAsync();

            string sql = "SELECT ID, ApartmentNumber, Description FROM Apartment ORDER BY ApartmentNumber;";

            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int id = reader.GetInt32(reader.GetOrdinal("ID"));
                int number = reader.GetInt32(reader.GetOrdinal("ApartmentNumber"));
                string descrip = reader.GetString(reader.GetOrdinal("Description"));

                aparts.Add(new Apartment(id, number, descrip));
            }

            return aparts;
        }

        public static Task<ObservableCollection<Room>> GetAllRooms() => GetRooms(null, null);

   
        public static async Task<ObservableCollection<Room>> GetRooms(int? apartmentId, string? search)
        {
            var rooms = new ObservableCollection<Room>();

   
            if (search != null)
                search = search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

            await using var connection = await OpenAsync();

            string sql = @"SELECT r.ID, r.ApartmentId, r.Name, ri.Area, ri.Temperature, ri.IsLightOn
                             FROM Rooms r
                             JOIN RoomInfo ri ON r.ID = ri.RoomId
                            WHERE (@apartId IS NULL OR r.ApartmentId = @apartId)
                              AND (@search  IS NULL OR LOWER_U(r.Name) LIKE '%' || LOWER_U(@search) || '%' ESCAPE '\')
                            ORDER BY r.ID;";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@apartId", (object?)apartmentId ?? DBNull.Value);
            command.Parameters.AddWithValue("@search", (object?)search ?? DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int id = reader.GetInt32(0);
                int apartId = reader.GetInt32(1);
                string name = reader.GetString(2);
                double area = reader.GetDouble(3);
                int temperature = reader.GetInt32(4);
                bool isLightOn = reader.GetInt32(5) != 0;

                rooms.Add(new Room(id, apartId, name, area, temperature, isLightOn));
            }

            return rooms;
        }

        
        public static async Task<DbStatistics> GetStatistics()
        {
            await using var connection = await OpenAsync();

            string sql = @"SELECT (SELECT COUNT(*) FROM Apartment),
                                  COUNT(r.ID),
                                  COALESCE(SUM(ri.Area), 0),
                                  COALESCE(AVG(ri.Temperature), 0),
                                  COALESCE(SUM(ri.IsLightOn), 0)
                             FROM Rooms r
                             JOIN RoomInfo ri ON ri.RoomId = r.ID;";

            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();

            return new DbStatistics(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetDouble(2),
                reader.GetDouble(3),
                reader.GetInt32(4));
        }

        
        public static async Task<DataTable> ExecuteReadOnlyQuery(string sql)
        {
            string trimmed = sql.TrimStart();
            while (trimmed.StartsWith("--"))
            {
                int eol = trimmed.IndexOf('\n');
                trimmed = eol < 0 ? string.Empty : trimmed.Substring(eol + 1).TrimStart();
            }

            if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("В этом окне разрешены только запросы SELECT / WITH.");

            await using var connection = await OpenAsync(readOnly: true);

            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            var table = new DataTable();
            table.Load(reader);
            return table;
        }

         
        public static async Task AddApart(Apartment apart)
        {
            await using var connection = await OpenAsync();

             
            string insertSql = @"INSERT INTO Apartment (ApartmentNumber, Description)
                                 VALUES (COALESCE((SELECT MAX(ApartmentNumber) FROM Apartment), 0) + 1, @description);";

            using var insertCommand = new SqliteCommand(insertSql, connection);
            insertCommand.Parameters.AddWithValue("@description", apart.Description);
            await insertCommand.ExecuteNonQueryAsync();

           
            using var selectCommand = new SqliteCommand(
                "SELECT ID, ApartmentNumber FROM Apartment WHERE ID = last_insert_rowid();", connection);
            using var reader = await selectCommand.ExecuteReaderAsync();
            await reader.ReadAsync();

            apart.Id = reader.GetInt32(0);
            apart.Number = reader.GetInt32(1);
        }

        public static async Task AddRoom(Room room)
        {
            await using var connection = await OpenAsync();

             
            using var transaction = connection.BeginTransaction();

            using var insertRoom = new SqliteCommand(
                "INSERT INTO Rooms (ApartmentId, Name) VALUES (@apartId, @name);", connection, transaction);
            insertRoom.Parameters.AddWithValue("@apartId", room.ApartId);
            insertRoom.Parameters.AddWithValue("@name", room.Name);
            await insertRoom.ExecuteNonQueryAsync();

            using var selectIdCommand = new SqliteCommand("SELECT last_insert_rowid();", connection, transaction);
            long lastId = Convert.ToInt64(await selectIdCommand.ExecuteScalarAsync());

            using var insertRoomInfo = new SqliteCommand(
                @"INSERT INTO RoomInfo (RoomId, Area, Temperature, IsLightOn)
                  VALUES (@roomId, @area, @temperature, @isLightOn);", connection, transaction);
            insertRoomInfo.Parameters.AddWithValue("@roomId", lastId);
            insertRoomInfo.Parameters.AddWithValue("@area", room.Area);
            insertRoomInfo.Parameters.AddWithValue("@temperature", room.Temperature);
            insertRoomInfo.Parameters.AddWithValue("@isLightOn", room.IsLight ? 1 : 0);
            await insertRoomInfo.ExecuteNonQueryAsync();

            transaction.Commit();

            room.Id = (int)lastId;
        }

        public static async Task UpdateApartments(Apartment apart)
        {
            await using var connection = await OpenAsync();

            string sql = "UPDATE Apartment SET ApartmentNumber = @number, Description = @description WHERE ID = @id;";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@id", apart.Id);
            command.Parameters.AddWithValue("@number", apart.Number);
            command.Parameters.AddWithValue("@description", apart.Description);
            await command.ExecuteNonQueryAsync();
        }

        public static async Task UpdateRoom(Room room)
        {
            await using var connection = await OpenAsync();
            using var transaction = connection.BeginTransaction();

             
            string sqlRoom = "UPDATE Rooms SET ApartmentId = @apartId, Name = @name WHERE ID = @id;";
            using var commandRoom = new SqliteCommand(sqlRoom, connection, transaction);
            commandRoom.Parameters.AddWithValue("@id", room.Id);
            commandRoom.Parameters.AddWithValue("@apartId", room.ApartId);
            commandRoom.Parameters.AddWithValue("@name", room.Name);
            await commandRoom.ExecuteNonQueryAsync();

            
            string sqlInfo = "UPDATE RoomInfo SET Area = @area, Temperature = @temperature, IsLightOn = @isLightOn WHERE RoomId = @roomId;";
            using var commandInfo = new SqliteCommand(sqlInfo, connection, transaction);
            commandInfo.Parameters.AddWithValue("@roomId", room.Id);
            commandInfo.Parameters.AddWithValue("@area", room.Area);
            commandInfo.Parameters.AddWithValue("@temperature", room.Temperature);
            commandInfo.Parameters.AddWithValue("@isLightOn", room.IsLight ? 1 : 0);
            await commandInfo.ExecuteNonQueryAsync();

            transaction.Commit();
        }

        public static async Task DeleteApartment(int apartmentId)
        {
            await using var connection = await OpenAsync();

             
            string sql = "DELETE FROM Apartment WHERE ID = @id;";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@id", apartmentId);
            await command.ExecuteNonQueryAsync();
        }

        public static async Task DeleteRoom(int roomId)
        {
            await using var connection = await OpenAsync();

            
            string sql = "DELETE FROM Rooms WHERE ID = @id;";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@id", roomId);
            await command.ExecuteNonQueryAsync();
        }
    }
}
