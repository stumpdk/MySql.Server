using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace MySql.Server
{
    /// <summary>
    /// A singleton class controlling test database initializing and cleanup
    /// </summary>
    public class MySqlServer
    {
        private string _mysqlDirectory;
        private string _dataDirectory;
        private string _dataRootDirectory;

        private int _serverPort = 3306;

        private Process _process;

        private MySqlConnection _testConnection;

        //The Instance is running the private constructor. This way, the class is implemented as a singleton
        private static MySqlServer instance;
        public static MySqlServer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MySqlServer();
                }

                return instance;
            }
        }

        /// <summary>
        /// The MySQL server is started in the constructor
        /// </summary>
        private MySqlServer()
        {
            _mysqlDirectory = BaseDirHelper.GetBaseDir() + "\\tempServer";
            _dataRootDirectory = _mysqlDirectory + "\\data";
            _dataDirectory = string.Format("{0}\\{1}", _dataRootDirectory, Guid.NewGuid());
        }

        ~MySqlServer()
        {
            if (instance != null) { 
                instance.ShutDown();
            }

            if (_process != null)
            {
                _process.Kill();
                _process.Dispose();
                _process = null;
            }

            instance = null;
        }

        /// <summary>
        /// Get a connection string for the server (no database selected)
        /// </summary>
        /// <returns>A connection string for the server</returns>
        public string GetConnectionString()
        {
            return string.Format("Server=127.0.0.1;Port={0};Protocol=pipe;", _serverPort.ToString());
        }

        /// <summary>
        /// Get a connection string for the server and a specified database
        /// </summary>
        /// <param name="databaseName">The name of the database</param>
        /// <returns>A connection string for the server and database</returns>
        public string GetConnectionString(string databaseName)
        {
            return string.Format("Server=127.0.0.1;Port={0};Protocol=pipe;Database={1};", _serverPort.ToString(), databaseName);
        }

        /// <summary>
        /// Create directories necessary for MySQL to run
        /// </summary>
        private void createDirs()
        {
            string[] dirs = { _mysqlDirectory, _dataRootDirectory, _dataDirectory };

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
                    System.Console.WriteLine("Could not create or delete directory: " + checkDir.FullName);
                }
            }
        }

        /// <summary>
        /// Removes all directories related to the MySQL process
        /// </summary>
        private void removeDirs()
        {
            string[] dirs = { this._mysqlDirectory, this._dataRootDirectory, this._dataDirectory };

            foreach (string dir in dirs)
            {
                DirectoryInfo checkDir = new DirectoryInfo(dir);
                try
                {
                    if (checkDir.Exists)
                        checkDir.Delete(true);
                }
                catch (Exception)
                {
                    System.Console.WriteLine("Could not delete directory: ", checkDir.FullName);
                }
            }
        }

        /// <summary>
        /// Extracts the files necessary for running MySQL as a process
        /// </summary>
        private void extractMySqlFiles()
        {
            try { 
                if (!new FileInfo(_mysqlDirectory + "\\mysqld.exe").Exists) {
                    //Extracting the two MySql files needed for the standalone server
                    File.WriteAllBytes(_mysqlDirectory + "\\mysqld.exe", Properties.Resources.mysqld);
                    File.WriteAllBytes(_mysqlDirectory + "\\errmsg.sys", Properties.Resources.errmsg);
                }
            }
            catch
            {
                throw;    
            }
        }
    
        /// <summary>
        /// Starts the server and creates all files and folders necessary
        /// </summary>
        public void StartServer()
        {
            //The process is still running, don't create a new
            if (_process != null && !_process.HasExited)
                return;

            createDirs();
            extractMySqlFiles();

            this._process = new Process();

            var arguments = new[]
            {
                "--standalone",
                "--console",
                string.Format("--basedir=\"{0}\"",_mysqlDirectory),
                string.Format("--lc-messages-dir=\"{0}\"",_mysqlDirectory),
                string.Format("--datadir=\"{0}\"",_dataDirectory),
                "--skip-grant-tables",
                "--enable-named-pipe",
                string.Format("--port={0}", _serverPort.ToString()),
               // "--skip-networking",
                "--innodb_fast_shutdown=2",
                "--innodb_doublewrite=OFF",
                "--innodb_log_file_size=1048576",
                "--innodb_data_file_path=ibdata1:10M;ibdata2:10M:autoextend"
            };

            _process.StartInfo.FileName = string.Format("\"{0}\\mysqld.exe\"", _mysqlDirectory);
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

        /// <summary>
        /// Start the server on a specified port number
        /// </summary>
        /// <param name="serverPort">The port on which the server should listen</param>
        public void StartServer(int serverPort)
        {
            _serverPort = serverPort;
            StartServer();
        }

        /// <summary>
        /// Checks if the server is started. The most reliable way is simply to check if we can connect to it
        /// </summary>
        ///
        private void waitForStartup()
        {
            int totalWaitTime = 0;
            int sleepTime = 100;

            Exception lastException = new Exception();

            if(_testConnection == null)
            {
                _testConnection = new MySqlConnection(GetConnectionString());
            }
            
            while (!_testConnection.State.Equals(System.Data.ConnectionState.Open))
            {
                if (totalWaitTime > 10000)
                    throw new Exception("Server could not be started.", lastException);

                totalWaitTime = totalWaitTime + sleepTime;

                try {
                    _testConnection.Open();
                }
                catch(Exception e)
                {
                    _testConnection.Close();
                    lastException = e;
                    Thread.Sleep(sleepTime);
                }
            }
            
            System.Console.WriteLine("Database connection established after " + totalWaitTime.ToString() + " miliseconds");
            _testConnection.ClearAllPoolsAsync();
            _testConnection.Close();
            _testConnection.Dispose();
            _testConnection = null;
        }

        /// <summary>
        /// Shuts down the server and removes all files related to it
        /// </summary>
        public void ShutDown()
        {
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill();
                    _process.WaitForExit();
                    _process = null;
                }

                //System.Console.WriteLine("Process killed");
                removeDirs();
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Could not close database server process: " + e.Message);
                throw;
            }
        }
    }
}
