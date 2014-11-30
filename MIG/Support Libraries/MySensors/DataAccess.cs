using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;

namespace MySensors
{
    public static class DataAccess
    {
        private static SQLiteConnection _dbConnection;

        static DataAccess()
        {
            OpenStatisticsDatabase();
        }

        #region Public Methods
        public static ushort GetNewNodeId()
        {
            var dbCommand = _dbConnection.CreateCommand();
            dbCommand.CommandText = "SELECT ifnull(MAX(Id),0) FROM Nodes";
            var executeResult = ExecuteScalarCommand(dbCommand);
            if (executeResult == null)
                return 255;

            var newId = Convert.ToUInt16(executeResult) + 1;
            return (ushort)(newId > 255 ? 255 : newId);
        }

        public static List<Sensor> GetSensors()
        {
            List<Sensor> result = new List<Sensor>();
            //lock (dbLock)
            {
                var dbCommand = _dbConnection.CreateCommand();
                string query = "select Id, NodeId, [Type], Name, LastConnectTime from Sensors";
                dbCommand.CommandText = query;
                dbCommand.CommandType = CommandType.Text;
                try
                {
                    var reader = dbCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        var id = (ushort) reader.GetInt32(0);
                        var nodeId = (ushort) reader.GetInt32(1);
                        var type = Convert.ToByte(reader.GetInt32(2));
                        var name = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                        var lastConnectTime = DateTime.Parse(reader.GetString(4));
                        result.Add(new Sensor() { Id = id, NodeId = nodeId, SensorType = (SensorPresentationType)type, Name = name, LastConnectTime = lastConnectTime });
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error Get Sensors: {0}", ex.Message);
                }

            }
            return result;
        }

        public static void UpdateSketchName(ushort nodeId, string sketchName)
        {
            if (!string.IsNullOrEmpty(sketchName))
            {
                var dbCommand = _dbConnection.CreateCommand();
                dbCommand.CommandText = "INSERT OR IGNORE Into Nodes (Id) VALUES (@id)";
                dbCommand.Parameters.Add(new SQLiteParameter("@id", nodeId));
                ExecuteNonQueryCommand(dbCommand);

                dbCommand = _dbConnection.CreateCommand();
                dbCommand.CommandText = "UPDATE Nodes SET SketchName = @sketchName WHERE Id = @id";
                dbCommand.Parameters.Add(new SQLiteParameter("@id", nodeId));
                dbCommand.Parameters.Add(new SQLiteParameter("@sketchName", sketchName));
                ExecuteNonQueryCommand(dbCommand);
            }
        }

        public static void UpdateSketchVersion(ushort nodeId, string sketchVersion)
        {
            if (!string.IsNullOrEmpty(sketchVersion))
            {
                var dbCommand = _dbConnection.CreateCommand();
                dbCommand.CommandText = "INSERT OR IGNORE Into Nodes (Id) VALUES (@id)";
                dbCommand.Parameters.Add(new SQLiteParameter("@id", nodeId));
                ExecuteNonQueryCommand(dbCommand);

                dbCommand = _dbConnection.CreateCommand();
                dbCommand.CommandText = "UPDATE Nodes SET SketchVersion = @sketchVersion WHERE Id = @id";
                dbCommand.Parameters.Add(new SQLiteParameter("@id", nodeId));
                dbCommand.Parameters.Add(new SQLiteParameter("@sketchVersion", sketchVersion));
                ExecuteNonQueryCommand(dbCommand);
            }
        }

        public static void UpdateSensor(ushort nodeId, ushort sensorId, ushort sensorType, string name, DateTime? connectTime)
        {
            var dbCommand = _dbConnection.CreateCommand();
            dbCommand.CommandText = "INSERT OR IGNORE Into Sensors (Id, NodeId) VALUES (@id, @nodeId)";
            dbCommand.Parameters.Add(new SQLiteParameter("@id", sensorId));
            dbCommand.Parameters.Add(new SQLiteParameter("@nodeId", nodeId));
            ExecuteNonQueryCommand(dbCommand);

            dbCommand = _dbConnection.CreateCommand();
            dbCommand.CommandText = "UPDATE Sensors SET Type = @type, Name = Coalesce(@name, Name), LastConnectTime = Coalesce(@connectTime, LastConnectTime) WHERE Id = @id AND NodeId = @nodeId";
            dbCommand.Parameters.Add(new SQLiteParameter("@id", sensorId));
            dbCommand.Parameters.Add(new SQLiteParameter("@nodeId", nodeId));
            dbCommand.Parameters.Add(new SQLiteParameter("@type", sensorType));
            dbCommand.Parameters.Add(string.IsNullOrEmpty(name)
                ? new SQLiteParameter("@name", DBNull.Value)
                : new SQLiteParameter("@name", name));

            dbCommand.Parameters.Add(connectTime == null
                ? new SQLiteParameter("@connectTime", DBNull.Value)
                : new SQLiteParameter("@connectTime", connectTime.ToString()));

            ExecuteNonQueryCommand(dbCommand);
        }
        #endregion

        #region Private Methods
        private static object ExecuteScalarCommand(DbCommand command)
        {
            object result = null;
            try
            {
                result = command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Execute Scalar Command: {0}", ex.Message);
            }
            return result;
        }

        private static void ExecuteNonQueryCommand(DbCommand command)
        {
            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Execute Scalar Command: {0}", ex.Message);
            }
        }

        private static bool OpenStatisticsDatabase()
        {
            bool success = false;
            //lock (dbLock)
            {
                try
                {
                    _dbConnection = new SQLiteConnection("URI=file:" + GetMySensorsDatabaseName());
                    _dbConnection.Open();
                    success = true;
                    Console.WriteLine("Sucess open DB");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error open DB: {0}", ex.Message);
                }
            }
            return success;
        }

        private static void CloseDatabase()
        {
            _dbConnection.Close();
        }

        private static string GetMySensorsDatabaseName()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MySensors.db");
        }
        #endregion
    }
}
