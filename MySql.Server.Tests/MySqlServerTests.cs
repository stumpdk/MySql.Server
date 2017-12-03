using MySql.Data.MySqlClient;
using System;
using System.IO;
using MySql.Server;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace MySqlServerTests
{
    [TestClass]
    public class MySqlServerTests
    {
        [TestMethod]
        public void KillProcess()
        {
            int previousProcessCount = Process.GetProcessesByName("mysqld").Length;
            
            MySqlServer database = MySqlServer.Instance;
            database.StartServer();
            database.ShutDown();

            Thread.Sleep(500);

            Assert.AreEqual(previousProcessCount, Process.GetProcessesByName("mysqld").Length, "should kill the running process");
        }

        [TestMethod]
        public void StartServerOnSpecifiedPort()
        {
            MySqlServer server = MySqlServer.Instance;
            server.StartServer(3366);
            MySqlHelper.ExecuteNonQuery(server.GetConnectionString(), "CREATE DATABASE testserver; USE testserver;");
            server.ShutDown();
        }

        [TestMethod]
        public void MultipleProcessesInARow()
        {
            var dbServer = MySqlServer.Instance;
            dbServer.StartServer();
            dbServer.ShutDown();
            dbServer.StartServer();
            dbServer.ShutDown();
        }
    }
}
