using System;
using System.Collections.Generic;
using System.IO;

namespace MySql.Server
{
    /// <summary>
    /// A helper class used to find the base directory of the application
    /// </summary>
    internal class BaseDirHelper
    {
        static string baseDir;
        public static string GetBaseDir()
        {
            if (baseDir == null)
            {
                baseDir = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.Parent.FullName.ToString();
            }

            return baseDir;
        }
    }
}
