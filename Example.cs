using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using MySql.Server;

namespace Example
{
    [TestClass]
    public class Example
    {
        //Starting the MySql server. Here it is done in the AssemblyInitialize method for performance purposes.
        //It could also be restarted in every test using [TestInitialize] attribute
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            MySqlServer dbServer = MySqlServer.Instance;
            dbServer.StartServer();

            //Let us create a table
            dbServer.ExecuteNonQuery("CREATE TABLE testTable (`id` INT NOT NULL, `value` CHAR(150) NULL,  PRIMARY KEY (`id`)) ENGINE = MEMORY;");

            //Insert data. You could of course insert data from a *.sql file
            dbServer.ExecuteNonQuery("INSERT INTO testTable (`value`) VALUES ('some value')");
        }

        //The server is shutdown as the test ends
        [AssemblyCleanup]
        public static void Cleanup()
        {
            MySqlServer dbServer = MySqlServer.Instance;

            dbServer.ShutDown();
        }

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
    }
}
