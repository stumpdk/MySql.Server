using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Server
{
    internal interface IDBConnectionStringFactory
    {
        string Server();
        string Database();
        string Database(string databaseName);
    }
}
