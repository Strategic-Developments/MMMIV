using System;
using Sandbox.ModAPI;
using System.IO;

namespace Meridian.Utilities
{
    public static class FileManager
    {
        public static string GetTextFileWorldStorage(string fileName)
        {
            if (!FileExists(fileName, typeof(FileManager), false))
            {
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(fileName, typeof(FileManager));
                lock (writer)
                {
                    writer.Write("");
                    writer.Flush();
                    writer.Close();
                }
            }
            TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(fileName, typeof(FileManager));

            string data;
            lock (reader)
            {
                data = reader.ReadToEnd();
                reader.Close();
            }

            return data;
        }

        public static void SaveXMLFileWorldStorage(object obj, string fileName)
        {
            TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(fileName, typeof(FileManager));
            lock (writer)
            {
                writer.Write(MyAPIGateway.Utilities.SerializeToXML(obj));
                writer.Flush();
                writer.Close();
            }
        }

        public static string GetTextFileGlobalStorage(string fileName)
        {
            if (!FileExists(fileName, typeof(FileManager), true))
            {
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(fileName);
                lock (writer)
                {
                    writer.Write("");
                    writer.Flush();
                    writer.Close();
                }
            }
            TextReader reader = MyAPIGateway.Utilities.ReadFileInGlobalStorage(fileName);

            string data;
            lock (reader)
            {
                data = reader.ReadToEnd();
                reader.Close();
            }

            return data;
        }

        public static void SaveXMLFileGlobalStorage(object obj, string fileName)
        {
            TextWriter writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(fileName);
            lock (writer)
            {
                writer.Write(MyAPIGateway.Utilities.SerializeToXML(obj));
                writer.Flush();
                writer.Close();
            }
        }

        public static void SaveTextFileGlobalStorage(string obj, string fileName)
        {
            TextWriter writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(fileName);
            lock (writer)
            {
                writer.Write(obj);
                writer.Flush();
                writer.Close();
            }
        }

        private static bool FileExists(string path, Type type, bool global = false)
        {
            try
            {
                TextReader reader = global ? MyAPIGateway.Utilities.ReadFileInGlobalStorage(path) : MyAPIGateway.Utilities.ReadFileInWorldStorage(path, type);
                lock (reader)
                {
                    reader.ReadToEnd();
                    reader.Close();
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
