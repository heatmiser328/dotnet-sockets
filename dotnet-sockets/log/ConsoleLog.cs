using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotnet_sockets.log
{
    public class ConsoleLog : ILog
    {
        public ConsoleLog(string level)
        {
            Level = level;
        }
        public ConsoleLog() : this("INFO"){}

        #region ILog Members
        public string Level { get; set; }

        public void Trace(string msg, params object[] args)
        {
            Write("TRACE", msg, args);
        }

        public void Debug(string msg, params object[] args)
        {
            Write("DEBUG", msg, args);
        }

        public void Info(string msg, params object[] args)
        {
            Write("INFO", msg, args);
        }

        public void Warn(string msg, params object[] args)
        {
            Write("WARN", msg, args);
        }

        public void Error(string msg, params object[] args)
        {
            Write("ERROR", msg, args);
        }

        public void Error(string msg, Exception ex, params object[] args)
        {
            string em = ExceptionMessage(ex);
            if (!string.IsNullOrEmpty(em))
                msg += System.Environment.NewLine + em;
            Write("ERROR", msg, args);
        }

        public void Fatal(string msg, params object[] args)
        {
            Write("FATAL", msg, args);
        }

        #endregion

        bool DisplayLevel(string level)
        {
            return true;
        }

        string ExceptionMessage(Exception ex)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            Exception e = ex;
            while (e != null)
            {
                sb.AppendLine(e.Message);
                e = e.InnerException;
            }
            return sb.ToString();
        }

        void Write(string level, string msg, params object[] args)
        {
            if (DisplayLevel(level))            
                Console.Out.WriteLine("{0}: {1,-5}: {2}", DateTime.Now, level, args != null ? string.Format(msg, args) : msg);                
        }
    }
}
