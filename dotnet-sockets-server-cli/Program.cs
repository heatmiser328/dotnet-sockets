using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using dotnet_sockets;

namespace dotnet_sockets_server_cli
{
    class Program
    {
        const int cDefaultPort = 8888;
        static void Main(string[] args)
        {            
            try
            {
                int port = args.Length > 0 ? Int32.Parse(args[0]) : cDefaultPort;                
                Info("DOTNET-SOCKETS Server");
                Run(port);
            }
            catch (Exception ex)
            {
                Error("DOTNET-SOCKET Server", ex);
            }
            finally
            {
                Info("DOTNET-SOCKET Server done");
            }
        }

        static void Run(int port)
        {
            ISocketServer server = null;
            try
            {
                server = new AsyncSocketServer(port);
                server.Connected += (sender, client) => {
                    Debug("DOTNET-SOCKET Server: client connected");
                };
                server.Disconnected += (sender, client) => {
                    Debug("DOTNET-SOCKET Server: client disconnected");
                };
                server.Error += (sender, err) => {
                    Error("DOTNET-SOCKET Server", err.Value);
                };
                server.ReceivedData += (sender, args) => {
                    string msg = System.Text.Encoding.UTF8.GetString(args.Data, 0, args.Size);
                    Debug("DOTNET-SOCKET Server: Received [{0}] bytes of data", args.Size);                    
                    // echo to other clients
                    string[] tokens = msg.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string token in tokens)
                    {
                        Debug("DOTNET-SOCKET Server: {0}", token);
                        foreach (ISocketClient sc in server.Clients)
                        {
                            if (sc !=  args.Client)
                            {
                                sc.Send(token);
                            }
                        }
                    }                        
                };
                server.Log += (sender, a) =>
                {
                    Log(a.Level, a.Message, a.Exception, a.Args);
                };

                server.Open().Wait();
                while (true)
                {
                    Debug("DOTNET-SOCKET Server: wait for connection");
                    server.Accept().Wait();                    
                }                
            }
            catch (Exception ex)
            {
                Error("DOTNET-SOCKET Server", ex);
            }
            finally
            {
                if (server != null)
                    server.Close().Wait();
            }
        }

        static void Debug(string msg, params object[] args)
        {
            Log("DEBUG", msg, null, args);
        }
        static void Info(string msg, params object[] args)
        {
            Log("INFO", msg, null, args);
        }
        static void Error(string msg, Exception ex, params object[] args)
        {
            Log("ERROR", msg, ex, args);
        }
        static void Log(string level, string msg, Exception ex = null, params object[] args)
        {
            Console.Out.WriteLine(String.Format("{0} : {1} : {2}", DateTime.Now, level, msg), args);
        }
    }
}