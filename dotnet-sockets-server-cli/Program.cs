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
            ILog log = new dotnet_sockets.log.ConsoleLog();
            try
            {
                int port = args.Length > 0 ? Int32.Parse(args[0]) : cDefaultPort;                
                log.Info("DOTNET-SOCKETS Server");
                Run(port, log);
            }
            catch (Exception ex)
            {
                log.Error("DOTNET-SOCKET Server", ex);
            }
            finally
            {
                log.Info("DOTNET-SOCKET Server done");
            }
        }

        static void Run(int port, ILog log)
        {
            ISocketServer server = null;
            try
            {
                server = new AsyncSocketServer(port, log);
                server.Connected += (sender, client) => {
                    log.Debug("DOTNET-SOCKET Server: client connected");
                };
                server.Disconnected += (sender, client) => {
                    log.Debug("DOTNET-SOCKET Server: client disconnected");
                };
                server.Error += (sender, err) => {
                    log.Error("DOTNET-SOCKET Server", err);
                };
                server.Received += (sender, args) => {
                    string msg = System.Text.Encoding.UTF8.GetString(args.Data, 0, args.Size);
                    log.Debug("DOTNET-SOCKET Server: Received [{0}] bytes of data", args.Size);                    
                    // echo to other clients
                    string[] tokens = msg.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string token in tokens)
                    {
                        log.Debug("DOTNET-SOCKET Server: {0}", token);
                        foreach (ISocketClient sc in server.Clients)
                        {
                            if (sc !=  args.Client)
                            {
                                sc.Send(token);
                            }
                        }
                    }                        
                };
                server.Open().Wait();
                while (true)
                {
                    log.Debug("DOTNET-SOCKET Server: wait for connection");
                    server.Accept().Wait();                    
                }                
            }
            catch (Exception ex)
            {
                log.Error("DOTNET-SOCKET Server", ex);
            }
            finally
            {
                if (server != null)
                    server.Close().Wait();
            }
        }
    }
}