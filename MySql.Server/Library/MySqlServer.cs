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
        private string _runningInstancesFile;

        private int _serverPort = 3306;
        private Process _process;

        private MySqlConnection _testConnection;
        
        public int ServerPort { get { return _serverPort; } }
        public int ProcessId
        {
            get
            {
                if (!_process.HasExited)
                {
                    return _process.Id;
                }
               
                return -1;
            }
        }

        public string StdOut {
            get
            {
                return _process?.StandardOutput.ReadToEnd();
            }
        }

        public string StdErr
        {
            get
            {
                return _process?.StandardError.ReadToEnd();
            }
        }


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
            _runningInstancesFile = BaseDirHelper.GetBaseDir() + "\\running_instances";
        }

        ~MySqlServer()
        {
            if (instance != null) { 
                instance.ShutDown();
            }

            if (_process != null)
            {
                try { 
                    _process.Kill();
                }
                catch(Exception e)
                {
                    System.Console.WriteLine("Could not kill process while disposing");
                }

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
            return string.Format("Server=127.0.0.1;Port={0};UserId=root;Pooling=false", _serverPort.ToString());
        }


        /// <summary>
        /// Get a connection string for the server and a specified database
        /// </summary>
        /// <param name="databaseName">The name of the database</param>
        /// <param name="userName">The name of the user to connect as</param>
        /// <returns>A connection string for the server and database</returns>
        public string GetConnectionString(string databaseName)
        {
            return (new MySqlConnectionStringBuilder(GetConnectionString()) { Database = databaseName }).ToString();
        }

        private string[] Directories
        {
            get
            {
                return new [] { _mysqlDirectory, Path.Combine(_mysqlDirectory, "bin"), Path.Combine(_mysqlDirectory, "share"), _dataRootDirectory, _dataDirectory };
            }
        }
        /// <summary>
        /// Create directories necessary for MySQL to run
        /// </summary>
        private void createDirs()
        {
            foreach (string dir in Directories) {
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
        private void removeDirs(int retries)
        {
            foreach (string dir in Directories)
            {
                DirectoryInfo checkDir = new DirectoryInfo(dir);

                if (checkDir.Exists)
                {
                    int i = 0;
                    while(i < retries)
                    {
                        try { 
                            checkDir.Delete(true);
                            i = retries;
                        }
                        catch (Exception e)
                        {
                            System.Console.WriteLine("Could not delete directory: " + checkDir.FullName + e.Message);
                            i++;
                            Thread.Sleep(50);
                        }
                    }
                }                        
            }

            try { 
                File.Delete(this._runningInstancesFile);
            }
            catch(Exception e)
            {
                System.Console.WriteLine("Could not delete runningInstancesFile");
            }
        }

        /// <summary>
        /// Extracts the files necessary for running MySQL as a process
        /// </summary>
        private void extractMySqlFiles()
        {
            try { 
                if (!new FileInfo(_mysqlDirectory + "\\bin\\mysqld.exe").Exists) {
                    //Extracting the two MySql files needed for the standalone server
                    File.WriteAllBytes(_mysqlDirectory + "\\bin\\mysqld.exe", Properties.Resources.mysqld);
                    File.WriteAllBytes(_mysqlDirectory + "\\bin\\mysql_install_db.exe", Properties.Resources.mysql_install_db);
                    File.WriteAllBytes(_mysqlDirectory + "\\share.zip", Properties.Resources.share);
                    System.IO.Compression.ZipFile.ExtractToDirectory(_mysqlDirectory + "\\share.zip", _mysqlDirectory);
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

            //Cleaning up any precedented processes
            this.KillPreviousProcesses();

            createDirs();
            extractMySqlFiles();

            using (var process = new Process())
            {

                var installarguments = new[]
                {
                    string.Format("--datadir=\"{0}\"",_dataDirectory),
                    string.Format("--port={0}", _serverPort.ToString()),
                    "--default-user",
                   // "--skip-networking",
                };

                process.StartInfo.FileName = string.Format("\"{0}\\bin\\mysql_install_db.exe\"", _mysqlDirectory);
                process.StartInfo.Arguments = string.Join(" ", installarguments);
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                System.Console.WriteLine("Running " + process.StartInfo.FileName + " " + String.Join(" ", installarguments));

                process.Start();
                process.WaitForExit();
            }

            this._process = new Process();

            var arguments = new[]
            {
                "--standalone",
                "--console",
                string.Format("--basedir=\"{0}\"",_mysqlDirectory),
                string.Format("--datadir=\"{0}\"",_dataDirectory),
                "--enable-named-pipe",
                string.Format("--port={0}", _serverPort.ToString()),
               // "--skip-networking",
                "--innodb_fast_shutdown=2",
                "--innodb_doublewrite=OFF",
                "--innodb_log_file_size=1048576",
            };

            _process.StartInfo.FileName = string.Format("\"{0}\\bin\\mysqld.exe\"", _mysqlDirectory);
            _process.StartInfo.Arguments = string.Join(" ", arguments);
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;

            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.RedirectStandardOutput = true;

            System.Console.WriteLine("Running " + _process.StartInfo.FileName + " " + String.Join(" ", arguments));

            try { 
                _process.Start();
                File.WriteAllText(_runningInstancesFile, _process.Id.ToString());
            }
            catch(Exception e){
                throw new Exception("Could not start server process: " + e.Message);
            }

            try
            {
                this.waitForStartup();
            } catch
            {
                var stderr = _process.StandardError.ReadToEnd();
                var stdout = _process.StandardOutput.ReadToEnd();
                throw new Exception(string.Format(
                    "An error happened while waiting for the server to start. StdOut: {0}{1} StdErr: {2}", 
                    stdout, 
                    Environment.NewLine + Environment.NewLine, 
                    stderr));

                throw;
            }
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
                    throw new Exception("Server could not be started." + lastException.Message);

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

        public void KillPreviousProcesses()
        {
            if (!File.Exists(_runningInstancesFile))
                return;

            string[] runningInstancesIds = File.ReadAllLines(_runningInstancesFile);

            for(int i = 0; i < runningInstancesIds.Length; i++)
            {
                try
                {
                    Process p = Process.GetProcessById(Int32.Parse(runningInstancesIds[i]));
                    p.Kill();
                }
                catch(Exception e)
                {
                    System.Console.WriteLine("Could not kill process: " + e.Message);
                }
            }

            try { 
                File.Delete(_runningInstancesFile);
            }
            catch(Exception e)
            {
                System.Console.WriteLine("Could not delete running instances file");
            }

            this.removeDirs(10);
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
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Could not close database server process: " + e.Message);
                throw;
            }

            removeDirs(10);
        }
    }
}
