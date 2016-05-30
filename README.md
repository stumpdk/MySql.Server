# Mysql.Server
MySql standalone server for C# unit tests

## Use
Download with [NuGet](https://www.nuget.org/packages/MySql.Server/), or download the [release](https://github.com/stumpdk/Mysql.Server/releases) and include **Mysql.Server.dll** as a reference in your project.

## How does it work?
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
See [Example.cs](/Examples/Example.cs) for a complete example.
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

## API
* **MySqlServer.Instance**: Retrieves an Instance of the server API.

* **MySqlServer.StartServer()**: Starts the server.

* **MySqlServer.StartServer(int serverPort)**: Starts the server at a specified port. Nice to have if you have a real MySql server running on the test machine.

* **MySqlServer.ShutDown()**: Shuts down the server.

* **MySqlServer.GetConnectionString()**: Returns a connection string to be used when connecting to the server.

* **MySqlServer.GetConnectionString(string databasename)**: Returns a connection string to be used when connecting to the server and a specific database. This method can only be used if a database is already created.

