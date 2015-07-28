using System;

namespace MySql.Server
{
    /*
     *  Static class used to serve connection strings 
     */ 
    internal class DBTestConnectionStringFactory : IDBConnectionStringFactory
    {
        /*
         * Returns a connection string of the server
         */ 
        public string Server(){
           // return "Server=localhost;Protocol=pipe;";
            return "Server=" + "127.0.0.1" + ";Protocol=pipe;";
        }

        /*
         * Returns a connection string of the default database (the test server)
         */ 
        public string Database()
        {
            return "Server=" + "127.0.0.1" + ";Database=testserver;Protocol=pipe;"; 
        }

        /**
         * Returns a connection string of a specific database
         */ 
        public string Database(string databaseName)
        {
            return "Server=" + "127.0.0.1" + ";Database=" + databaseName + ";Protocol=pipe;";
        }
    }
}
