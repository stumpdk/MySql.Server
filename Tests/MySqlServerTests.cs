using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using MySql.Server;
using System.Diagnostics;

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
            
            Assert.AreEqual(previousProcessCount, Process.GetProcessesByName("mysqld").Length, "should kill the running process");
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
