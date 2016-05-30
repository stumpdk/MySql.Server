using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using MySql.Server;
using System.Diagnostics;

namespace Example
{
    [TestClass]
    public class Example
    {
        private static readonly string _testDatabaseName = "testserver";
        
        /// <summary>
        /// Example of a simple test: Start a server, create a database and add data to it
        /// </summary>
        [TestMethod]
        public void ExampleTest()
        {
            //Setting up and starting the server
            //This can also be done in a AssemblyInitialize method to speed up tests
            MySqlServer dbServer = MySqlServer.Instance;
            dbServer.StartServer();

            //Create a database and select it
            MySqlHelper.ExecuteNonQuery(dbServer.GetConnectionString(), string.Format("CREATE DATABASE {0};USE {0};", _testDatabaseName));

            //Create a table
            MySqlHelper.ExecuteNonQuery(dbServer.GetConnectionString(_testDatabaseName), "CREATE TABLE testTable (`id` INT NOT NULL, `value` CHAR(150) NULL,  PRIMARY KEY (`id`)) ENGINE = MEMORY;");

            //Insert data (large chunks of data can of course be loaded from a file)
            MySqlHelper.ExecuteNonQuery(dbServer.GetConnectionString(_testDatabaseName), "INSERT INTO testTable (`id`,`value`) VALUES (1, 'some value')");
            MySqlHelper.ExecuteNonQuery(dbServer.GetConnectionString(_testDatabaseName), "INSERT INTO testTable (`id`, `value`) VALUES (2, 'test value')");

            //Load data
            using (MySqlDataReader reader = MySqlHelper.ExecuteReader(dbServer.GetConnectionString(_testDatabaseName), "select * from testTable WHERE id = 2"))
            {
                reader.Read();

                Assert.AreEqual("test value", reader.GetString("value"), "Inserted and read string should match");
            }

            //Shutdown server
            dbServer.ShutDown(); 
        }
    }
}
