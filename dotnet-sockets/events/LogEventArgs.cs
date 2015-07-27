using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotnet_sockets
{
    public class LogEventArgs
    {
        public LogEventArgs(string level, string msg, Exception ex = null, params object[] args)
        {
            Level = level;
            Message = msg;
            Exception = ex;
            Args = args;
        }
        public string Level { get; private set; }
        public string Message { get; private set; }
        public Exception Exception { get; private set; }
        public object[] Args { get; private set; }
    }
}
