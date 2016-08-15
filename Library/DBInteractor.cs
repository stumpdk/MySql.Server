using MySql.Data.MySqlClient;
using System;
namespace MySql.Server
{ 
    /// <summary>
    /// DBInteractor handles interactions with the running server.
    /// That is: Opening and closing connections and executing queries.
    /// </summary>
    public class DBInteractor : IDisposable
    {
        private MySqlConnection _myConnection;
        public MySqlConnection Connection
        {
            get { return _myConnection; }
        }

        public DBInteractor(string connectionString)
        {
            Connect(connectionString);
        }

        private void Connect(string connectionString)
        {
            if (this._myConnection == null)
            {
                this._myConnection = new MySqlConnection(connectionString);

            }

            if (this._myConnection.State != System.Data.ConnectionState.Open)
            {
                this._myConnection.Open();
            }
        }

        public void Disconnect()
        {
            if (this._myConnection.State != System.Data.ConnectionState.Closed)
                this._myConnection.Close();
        }

        public void SelectDatabase(string databaseName)
        {
            ExecuteNonQuery(String.Format("CREATE DATABASE {0};", databaseName));
        }

        public void CreateDatabase(string databaseName)
        {
            ExecuteNonQuery(String.Format("USE {0};", databaseName));
        }

        public void ExecuteNonQuery(string query)
        {
            try
            {
                MySqlCommand command = new MySqlCommand(query, this._myConnection);
                command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Could not execute non query: "  + e.Message);
                throw;
            }
        }

        public MySqlDataReader ExecuteReader(string query)
        {
            try {
                MySqlCommand command = new MySqlCommand(query, this._myConnection);
                return command.ExecuteReader();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose()
        {
            Dispose(true);
            //           GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                this.Disconnect();
                this._myConnection.Dispose();
            }

            disposed = true;
        }
    }
}