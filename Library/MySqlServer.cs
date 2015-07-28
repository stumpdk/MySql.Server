using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace MySql.Server
{
    /**
     * A singleton class controlling test database initializing and cleanup
     */ 
    public class MySqlServer : IDisposable
    {
        //The Instance is running the private constructor. This way, the class is implemented as a singleton
        private static MySqlServer instance;
        public static MySqlServer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MySqlServer(new DBTestConnectionStringFactory());
                }

                return instance;
            }
        }


        private string _mysqlDirectory;
        private string _dataDirectory;
        private string _dataRootDirectory;

        private IDBConnectionStringFactory _conStrFac;

        private MySqlConnection _myConnection;
        public MySqlConnection Connection { 
            get {
                if (this._myConnection == null)
                {
                    this.OpenConnection(this._conStrFac.Database());
                }
                return this._myConnection; 
            }  
        }

        private Process _process;

        private MySqlServer(IDBConnectionStringFactory conStrFac)
        {
            this._mysqlDirectory = BaseDirHelper.GetBaseDir() + "\\tempServer";
            this._dataRootDirectory = this._mysqlDirectory + "\\data";
            this._dataDirectory = this._dataRootDirectory + "\\" + Guid.NewGuid() + "";

            this.killProcesses();

            this.createDirs();

            this.extractMySqlFiles();

            this._conStrFac = conStrFac;
        }

    /*    private void removeDirs()
        {
            //Removing any previous data directories
            new DirectoryInfo(this._dataRootDirectory).GetDirectories().ToList().ForEach(delegate(DirectoryInfo dir)
            {
                try
                {
                    dir.Delete(true);
                }
                catch (Exception)
                {
                    System.Console.WriteLine("Could not delete data directory" + dir.FullName);
                }
            });
        }
        */
        private void createDirs()
        {
            string[] dirs = { this._mysqlDirectory, this._dataRootDirectory, this._dataDirectory };

            foreach (string dir in dirs) {
                DirectoryInfo checkDir = new DirectoryInfo(dir);
                try
                {
                    if (checkDir.Exists)
                        checkDir.Delete(true);

                    checkDir.Create();
                }
                catch(Exception)
                {
                    System.Console.WriteLine("Could not create or delete directory: ", checkDir);
                }
            }
        }

        private void extractMySqlFiles()
        {
            try { 
                if (!new FileInfo(this._mysqlDirectory + "\\mysqld.exe").Exists) {
                    //Extracting the two MySql files needed for the standalone server
                    File.WriteAllBytes(this._mysqlDirectory + "\\mysqld.exe", Properties.Resources.mysqld);
                    File.WriteAllBytes(this._mysqlDirectory + "\\errmsg.sys", Properties.Resources.errmsg);
                }
            }
            catch
            {
                throw;    
            }
        }

        private void killProcesses()
        {
            //Killing all processes with the name mysqld.exe
            foreach (var process in Process.GetProcessesByName("mysqld"))
            {
                try
                {
                    process.Kill();
                }
                catch (Exception)
                {
                    System.Console.WriteLine("Tried to kill already existing mysqld process without success");
                }
            }
        }

        public void StartServer()
        {
            _process = new Process();

            var arguments = new[]
            {
                "--standalone",
                "--console",
                "--basedir=" + "\"" + this._mysqlDirectory + "\"",
                "--lc-messages-dir=" + "\"" + this._mysqlDirectory + "\"",
                "--datadir=" + "\"" + this._dataDirectory + "\"",
                "--skip-grant-tables",
                "--enable-named-pipe",
                "--skip-networking",
                "--innodb_fast_shutdown=2",
                "--innodb_doublewrite=OFF",
                "--innodb_log_file_size=1048576",
                "--innodb_data_file_path=ibdata1:10M;ibdata2:10M:autoextend"
            };

            _process.StartInfo.FileName = "\"" + this._mysqlDirectory + "\\mysqld.exe" + "\"";
            _process.StartInfo.Arguments = string.Join(" ", arguments);
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;

            System.Console.WriteLine("Running " + _process.StartInfo.FileName + " " + String.Join(" ", arguments));

            try { 
                _process.Start();
            }
            catch(Exception e){
                throw new Exception("Could not start server process: " + e.Message);
            }

            this.waitForStartup();
        }

        /**
         * Checks if the server is started. The most reliable way is simply to check
         * if we can connect to it
         **/
        private void waitForStartup()
        {
            bool connected = false;
            int waitTime = 0;

            while (!connected)
            {
                if (waitTime > 10000)
                    throw new Exception("Server could not be started");

                waitTime = waitTime + 500;

                try
                {
                    this.OpenConnection(this._conStrFac.Server());
                    connected = true;

                    this.ExecuteNonQuery("CREATE DATABASE testserver;USE testserver;", false);

                    System.Console.WriteLine("Database connection established after " + waitTime.ToString() + " miliseconds");
                }
                catch (Exception)
                {
                    Thread.Sleep(500);
                    connected = false;
                }
            }
        }

        private void ExecuteNonQuery(string query, bool useDatabase)
        {
            string connectionString = useDatabase ? this._conStrFac.Database() : this._conStrFac.Server();
            this.OpenConnection(connectionString);
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
            finally{
                this.CloseConnection();
            }
        }

        public void ExecuteNonQuery(string query)
        {
            this.ExecuteNonQuery(query, true);
        }

        public MySqlDataReader ExecuteReader(string query)
        {
            this.OpenConnection(this._conStrFac.Database());

            try {
                MySqlCommand command = new MySqlCommand(query, this._myConnection);
                return command.ExecuteReader();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void OpenConnection(string connectionString)
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

        public void CloseConnection()
        {
            if(this._myConnection.State != System.Data.ConnectionState.Closed)
                this._myConnection.Close();
        }

        public void ShutDown()
        {
            try
            {
                this.CloseConnection();
                if (!this._process.HasExited)
                {
                    this._process.Kill();
                }
                //System.Console.WriteLine("Process killed");
                this._process.Dispose();
                this._process = null;
                this.killProcesses();
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Could not close database server process: " + e.Message);
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
                this.CloseConnection();
                this._myConnection.Dispose();
            }

            disposed = true;
        }
    }
}
