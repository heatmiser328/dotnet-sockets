using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mocks
{
    internal class MockLog : dotnet_sockets.ILog
    {
        readonly Xunit.Abstractions.ITestOutputHelper output;
        public MockLog(Xunit.Abstractions.ITestOutputHelper output)
        {
            this.output = output;
        }

        public void Trace(string msg)
        {
            output.WriteLine(msg);
        }

        public void Trace(string msg, params object[] args)
        {
            output.WriteLine(msg, args);
        }

        public void Debug(string msg)
        {
            output.WriteLine(msg);
        }

        public void Debug(string msg, params object[] args)
        {
            output.WriteLine(msg, args);
        }

        public void Info(string msg)
        {
            output.WriteLine(msg);
        }

        public void Info(string msg, params object[] args)
        {
            output.WriteLine(msg, args);
        }

        public void Warn(string msg)
        {
            output.WriteLine(msg);
        }

        public void Warn(string msg, params object[] args)
        {
            output.WriteLine(msg, args);
        }

        public void Error(string msg)
        {
            output.WriteLine(msg);
        }

        public void Error(string msg, params object[] args)
        {
            output.WriteLine(msg, args);
        }

        public void Fatal(string msg)
        {
            output.WriteLine(msg);
        }

        public void Fatal(string msg, params object[] args)
        {
            output.WriteLine(msg, args);
        }

        #region ILog Members

        public string Level
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void Error(string msg, Exception ex)
        {
            throw new NotImplementedException();
        }

        public void Error(string msg, Exception ex, params object[] args)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
