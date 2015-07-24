using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using dotnet_sockets;

namespace dotnet_sockets_client_cli
{
    class Program
    {
        const int cDefaultPort = 8888;
        const int cDefaultCount = 10;
        static void Main(string[] args)
        {
            ISocketClient client = null;
            ILog log = new dotnet_sockets.log.ConsoleLog();
            try
            {
                string server = args.Length > 0 ? args[0] : null;
                int port = args.Length > 1 ? Int32.Parse(args[1]) : cDefaultPort;
                string mode = args.Length > 2 ? args[2] : "sub";
                int count = args.Length > 3 ? Int32.Parse(args[3]) : cDefaultCount;
                string pubdata = args.Length > 4 ? args[4] : "{0}\r\n";

                log.Info("DOTNET-SOCKETS Client");

                client = new AsyncSocketClient(server, port, log);
                client.Connected += (sender, b) =>
                {
                    log.Debug("DOTNET-SOCKET Client: connected");
                };
                client.Disconnected += (sender, b) =>
                {
                    log.Debug("DOTNET-SOCKET Client: disconnected");
                };
                client.Error += (sender, err) =>
                {
                    log.Error("DOTNET-SOCKET Client", err);
                };
                client.Sent += (sender, size) =>
                {
                    log.Debug("DOTNET-SOCKET Client: Sent [{0}] bytes of data", size);
                };
                client.Received += (sender, data) =>
                {
                    string msg = System.Text.Encoding.UTF8.GetString(data.Data, 0, data.Size);
                    log.Debug("DOTNET-SOCKET Client: Received [{0}] bytes of data", data.Size);
                    log.Debug("DOTNET-SOCKET Client: {0}", msg);
                };

                client.Open().Wait();
                if (mode == "pub")
                    Publish(client, pubdata, count, log);
                else
                    Subscribe(client, count, log);
            }
            catch (Exception ex)
            {
                log.Error("DOTNET-SOCKET Client", ex);
            }
            finally
            {
                log.Info("DOTNET-SOCKET Client closing");                
                if (client != null)
                    client.Close().Wait();
                log.Info("DOTNET-SOCKET Client done");                
            }            
        }


        static void Publish(ISocketClient client, string data, int count, ILog log)
        {
            try
            {
                if (System.IO.File.Exists(data))
                {
                    data = System.IO.File.ReadAllText(data);
                }
                for (int i = 0; i < count && client.IsConnected; i++)
                {
                    string msg = string.Format(data, i);
                    log.Debug("DOTNET-SOCKET Client: Sending {0}", i);
                    client.Send(msg).Wait();
                }
            }
            catch (Exception ex)
            {
                log.Error("DOTNET-SOCKET Client", ex);
            }
            finally
            {
            }
        }

        static void Subscribe(ISocketClient client, int count, ILog log)
        {
            try
            {
                while (client.IsConnected)
                {                    
                    log.Debug("DOTNET-SOCKET Client: Waiting for data");
                    client.Receive().Wait();
                }
            }
            catch (Exception ex)
            {
                log.Error("DOTNET-SOCKET Client", ex);
            }
            finally
            {
            }

        }
    }
}
