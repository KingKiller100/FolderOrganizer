using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FolderOrganizer.Application;

namespace FolderOrganizer.Logging
{
    class Logger
    {
        public enum Level
        {
            DBG = 0b1,       // Debug
            INF = 0b10,      // Inform
            WRN = 0b100,     // Warn
            ERR = 0b1000,    // Error
            FTL = 0b10000,   // Fatal
            BNR = 0B100000,  // Banner
            CRT = FTL,       // Critical
        }

        private static FileStream _logFile;
        private static Encoding _encoding;
        private static Level _minLevel;

        public static bool Open(string filename, Level minLvl)
        {
            return Open(filename, minLvl, Encoding.UTF8);
        }

        public static bool Open(string filename, Level minLvl, Encoding encoding)
        {
            var path = Path.Combine(AppFolders.LogsDir, filename);

            if (_logFile != null)
            {
                var msg = @"Must first close logger before reopening";
                Console.WriteLine(msg);
                return true;
            }

            try
            {
                _logFile = File.Open(path, FileMode.Append);
                _encoding = encoding;
                _minLevel = minLvl;
            }
            catch (Exception e)
            {
                var err = $@"[Exception] {e}";
                Console.WriteLine(err);
                return false;
            }

            return true;
        }

        public void SetLevel(Level lvl) => _minLevel = lvl;

        public static bool IsOpen()
        {
            return _logFile == null || _logFile != Stream.Null;
        }

        public static void Close()
        {
            _logFile.Dispose();
            _logFile.Close();
            _logFile = null;
        }

        public static void Raw(string msg)
        {
            Output($"{msg}");
        }

        public static void Dbg(string msg)
        {
            Logify(Level.DBG, msg);
        }

        public static void Inf(string msg)
        {
            Logify(Level.INF, msg);
        }
        public static void Wrn(string msg)
        {
            Logify(Level.WRN, msg);
        }

        public static void Err(string msg)
        {
            Logify(Level.ERR, msg);
        }

        public static void Crt(string msg)
        {
            Logify(Level.CRT, msg);
        }

        public static void Ftl(string msg)
        {
            Ftl(msg, new Exception(msg));
        }

        public static void Ftl(string msg, Exception e)
        {
            Logify(Level.FTL, $"Message: {msg}{Environment.NewLine}Exception: {e}");
            throw e;
        }


        public static void Bnr(string msg, string padStr, byte padCount)
        {
            var dt = DateTime.Now;
            var padding = string.Empty;

            for (var i = 0; i < padCount; ++i)
            {
                padding += padStr;
            }

            var log = $"[{dt:d} {dt:T}]: {padding} {msg} {padding}";
            Output(log);
        }

        private static void Logify(Level lvl, string msg)
        {
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
    }
}
