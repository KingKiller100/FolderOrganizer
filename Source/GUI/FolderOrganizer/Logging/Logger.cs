using System;
using System.IO;
using System.Text;

namespace FolderOrganizer.Logging
{
    class Logger
    {
        public enum Level
        {
            TRC = 0b1,        // Trace
            DBG = 0b10,       // Debug
            INF = 0b100,      // Inform
            WRN = 0b1000,     // Warn
            ERR = 0b10000,    // Error
            FTL = 0b100000,   // Fatal
            CRT = FTL,        // Critical
        }

        private static string _fPath;
        private static FileStream _logFile;
        private static Encoding _encoding = Encoding.UTF8;
        private static Level _minLevel = Level.DBG;

        #region public methods

        public static byte[] ReadBytes()
        {
            return File.ReadAllBytes(_fPath);
        }

        public static void MoveFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            filePath = Path.GetFullPath(Path.ChangeExtension(filePath, ".log"));

            if (_fPath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
                return;

            CloseFile();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            File.Move(_fPath, filePath);
            Open(filePath, _minLevel);
        }

        public static bool Open(string filename, Level minLvl, Encoding encoding)
        {
            var result = OpenFile(filename);
            ChangeEncoding(encoding);
            SetLevel(minLvl);
            return result;
        }

        public static bool Open(string filename, Level minLvl)
        {
            return Open(filename, minLvl, Encoding.UTF8);
        }

        public static void SetLevel(Level lvl) => _minLevel = lvl;

        public static void ChangeEncoding(Encoding encoding)
        {
            if (Equals(encoding, _encoding)
            || _logFile.Length <= 0)
                return;

            CloseFile();
            var data = ReadBytes();
            var convertedData = Encoding.Convert(_encoding, encoding, data);
            _encoding = encoding;
            _logFile = File.Create(_fPath);
            _logFile.Write(convertedData, 0, convertedData.Length);
            _logFile.Flush();
        }

        public static bool IsOpen()
        {
            return _logFile == null || _logFile != Stream.Null;
        }

        public static void CloseFile()
        {
            _logFile.Dispose();
            _logFile.Close();
            _logFile = null;
        }

        public static void Raw(string msg)
        {
            Output(msg);
        }

        public static void NewLine()
        {
            Raw(string.Empty);
        }

        public static void Trace(string msg)
        {
            Logify(Level.TRC, msg);
        }

        public static void Debug(string msg)
        {
            Logify(Level.DBG, msg);
        }

        public static void Info(string msg)
        {
            Logify(Level.INF, msg);
        }
        public static void Warn(string msg)
        {
            Logify(Level.WRN, msg);
        }

        public static void Error(string msg)
        {
            Logify(Level.ERR, msg);
        }

        public static void Critical(string msg)
        {
            Logify(Level.CRT, msg);
        }

        public static void Fatal(string msg)
        {
            Fatal(msg, new Exception(msg));
        }

        public static void Fatal(string msg, Exception e)
        {
            Logify(Level.FTL, $"Message: {msg}{Environment.NewLine}Exception: {e}");
            throw e;
        }

        public static void Banner(string msg, string padStr, byte padCount)
        {
            var dt = DateTime.Now;
            var padding = string.Empty;

            for (var i = 0; i < padCount; ++i)
            {
                padding += padStr;
            }

            var log = $"[{dt:d} {dt:T}] {padding}{msg}{padding}";
            Output(log);
        }

        #endregion

        #region private methods

        private static bool OpenFile(string filepath)
        {
            if (_logFile != null)
            {
                Console.WriteLine("Must first close logger before reopening");
                return true;
            }

            try
            {
                var path = Path.GetDirectoryName(filepath);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                _fPath = Path.GetFullPath(Path.ChangeExtension(filepath, ".log"));
                _logFile = File.Create(filepath);
            }
            catch (Exception e)
            {
                Console.WriteLine($@"Cannot open file: {filepath}{Environment.NewLine}[Exception] {e}");
                return false;
            }

            return IsOpen();
        }

        private static void Logify(Level lvl, string msg)
        {
            if (lvl < _minLevel)
                return;

            var dt = DateTime.Now;
            var log = $"[{dt:d} {dt:T}] [{lvl}]: {msg}";
            Output(log);
        }

        private static void Output(string log)
        {
            Console.WriteLine(log);
            var data = _encoding.GetBytes($"{log}{Environment.NewLine}");
            _logFile.Write(data, 0, data.Length);
            _logFile.Flush();
        }

        #endregion
    }
}

