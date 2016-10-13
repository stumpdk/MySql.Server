# Mysql.Server [![Build Status](https://travis-ci.org/stumpdk/MySql.Server.svg?branch=master)](https://travis-ci.org/stumpdk/MySql.Server)

MySql standalone server for C# unit tests

## Use
Download with [NuGet](https://www.nuget.org/packages/MySql.Server/), or download the [release](https://github.com/stumpdk/Mysql.Server/releases) and include **Mysql.Server.dll** as a reference in your project.

## How it works
Mysql.Server is simply running a minimal instance of MySql (currently version 5.6.26). Necessary data and log files are created at run time (and are cleaned up afterwards).

Mysql.Server makes it possible to create and run unit tests on a real MySql server without spending time on server setup.

## Examples

### Create server, table and data.
See [Example.cs](/Examples/Example.cs) for a complete example.
```c#
        //Get an instance
        MySqlServer dbServer = MySqlServer.Instance;
        
        //Start the server
        dbServer.StartServer();
        
        //Create a database and use it
        MySqlHelper.ExecuteNonQuery(dbServer.GetConnectionString(), "CREATE DATABASE testserver; USE testserver;");
        
        //Insert data
        MySqlHelper.ExecuteNonQuery(dbServer.GetConnectionString(), "INSERT INTO testTable (`id`, `value`) VALUES (2, 'test value')"); 
        
        //Shut down server
        dbServer.ShutDown();
```

### A test
```c#
        //Concrete test. Writes data and reads it again.
        [TestMethod]
        public void TestMethod()
        {
            MySqlServer database = MySqlServer.Instance;

            MySqlHelper.ExecuteNonQuery(database.GetConnectionString(), "INSERT INTO testTable (`id`, `value`) VALUES (2, 'test value')");

            using (MySqlDataReader reader = MySqlHelper.ExecuteReader(database.GetConnectionString(), "SELECT * FROM testTable WHERE id = 2"))
            {
                reader.Read();

                Assert.AreEqual("test value", reader.GetString("value"), "Inserted and read string should match");
            }
        }
```

### Complete example

```
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
```

## API
* **MySqlServer.Instance**: Retrieves an Instance of the server API.

* **MySqlServer.StartServer()**: Starts the server.

* **MySqlServer.StartServer(int serverPort)**: Starts the server at a specified port. Nice to have if you have a real MySql server running on the test machine.

* **MySqlServer.ShutDown()**: Shuts down the server.

* **MySqlServer.GetConnectionString()**: Returns a connection string to be used when connecting to the server.

* **MySqlServer.GetConnectionString(string databasename)**: Returns a connection string to be used when connecting to the server and a specific database. This method can only be used if a database is already created.

* **MySqlServer.ProcessId**: Returns the process id of the server. Returns -1 if the process has exited.

* **MySqlServer.ServerPort**: Returns the server port of the instance.

