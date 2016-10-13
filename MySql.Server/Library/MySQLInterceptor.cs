using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySql.Server
{
    public class MySQLInterceptorStats : BaseCommandInterceptor
    {
        private static int _nonQueries = 0;
        private static int _readerQueries = 0;
        private static int _scalarQueries = 0;

        public override void Init(MySqlConnection connection)
        {
            base.Init(connection);
        }

        public override bool ExecuteNonQuery(string sql, ref int returnValue)
        {
            _nonQueries++;
            return false;
        }

        public override bool ExecuteReader(string sql, System.Data.CommandBehavior behavior, ref MySqlDataReader returnValue)
        {
            _readerQueries++;
            return base.ExecuteReader(sql, behavior, ref returnValue);
        }

        public override bool ExecuteScalar(string sql, ref object returnValue)
        {
            _scalarQueries++;
            return base.ExecuteScalar(sql, ref returnValue);
        }

        public static string GetStats()
        {
            return string.Format("Connection stats: {0} non queries, {1} reader queries, {2} scalar queries", _nonQueries.ToString(), _readerQueries.ToString(), _scalarQueries.ToString());
        }
    }
}
