# Mysql.Server
MySql standalone server for C# unit tests

## Use
Download with NuGet, or download the [release](https://github.com/stumpdk/Mysql.Server/releases) and include **MySqlStandAloneServer.dll** as a reference in your project.

## How does it work?
MySqlStandAloneServer is simply running a minimal instance of MySql (currently version 5.6.26). Necessary data and log files are created at run time (and are cleaned up afterwards).

The software makes it possible to create and run unit tests on a real MySql server without spending time on server setup.

## Example

### Create server, table and data.
See [Example.cs](https://github.com/stumpdk/MySqlStandAloneServer/blob/master/Example.cs) for a complete example.
```c#
        //Starting the MySql server. Here it is done in the AssemblyInitialize method for performance purposes.
        //It could also be restarted in every test using [TestInitialize] attribute
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            MySqlServer dbServer = MySqlServer.Instance;
            dbServer.StartServer();

            //Let us create a table
            dbServer.ExecuteNonQuery("CREATE TABLE testTable (`id` INT NOT NULL, `value` CHAR(150) NULL,  PRIMARY KEY (`id`)) ENGINE = MEMORY;");

            //Insert data
            dbServer.ExecuteNonQuery("INSERT INTO testTable (`value`) VALUES ('some value')");
        }
```

### Make a test
```c#
        //Concrete test. Writes data and reads it again.
        [TestMethod]
        public void TestMethod()
        {
            MySqlServer database = MySqlServer.Instance;

            database.ExecuteNonQuery("insert into testTable (`id`, `value`) VALUES (2, 'test value')");

            using (MySqlDataReader reader = database.ExecuteReader("select * from testTable WHERE id = 2"))
            {
                reader.Read();

                Assert.AreEqual("test value", reader.GetString("value"), "Inserted and read string should match");
            }
        }
```

### Shut down server
```c#       
        //The server is shutdown as the test ends
        [AssemblyCleanup]
        public static void Cleanup()
        {
            MySqlServer dbServer = MySqlServer.Instance;
    
            dbServer.ShutDown();
        }
```

## API
* **MySqlServer.Instance**: Retrieves an Instance of the server API.

* **MySqlServer.StartServer()**: Starts the server and creates a database ("testdatabase"). Optimally this is run once during a test run. But it can be run as many times as needed (to get independent tests).

* **MySqlServer.ShutDown()**: Shuts down the server.

* **MySqlServer.ExecuteNonQuery(string query)**: Executes a query with no return on default database ("testdatabase").

* **MySqlServer.ExecuteReader(string query)**: Executes query and returns a MySqlDataReader object.

* **MySqlServer.Connection**: Returns a connection object to the default database ("testdatabase"). This can be used to make more specific operations on the server with a MySqlCommand object (like prepared statements or ExecuteReaderAsync).

