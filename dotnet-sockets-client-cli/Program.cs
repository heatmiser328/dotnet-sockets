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
            try
            {
                string server = args.Length > 0 ? args[0] : null;
                int port = args.Length > 1 ? Int32.Parse(args[1]) : cDefaultPort;
                string mode = args.Length > 2 ? args[2] : "sub";
                int count = args.Length > 3 ? Int32.Parse(args[3]) : cDefaultCount;
                string pubdata = args.Length > 4 ? args[4] : "{0}\r\n";

                Info("DOTNET-SOCKETS Client");

                client = new AsyncSocketClient(server, port);
                client.Connected += (sender, b) =>
                {
                    Debug("DOTNET-SOCKET Client: connected");
                };
                client.Disconnected += (sender, b) =>
                {
                    Debug("DOTNET-SOCKET Client: disconnected");
                };
                client.Error += (sender, err) =>
                {
                    Error("DOTNET-SOCKET Client", err.Value);
                };
                client.Sent += (sender, size) =>
                {
                    Debug("DOTNET-SOCKET Client: Sent [{0}] bytes of data", size);
                };
                client.ReceivedData += (sender, data) =>
                {
                    string msg = System.Text.Encoding.UTF8.GetString(data.Data, 0, data.Size);
                    Debug("DOTNET-SOCKET Client: Received [{0}] bytes of data", data.Size);
                    Debug("DOTNET-SOCKET Client: {0}", msg);
                };
                client.Log += (sender, a) =>
                {
                    Log(a.Level, a.Message, a.Exception, a.Args);
                };

                client.Open().Wait();
                if (mode == "pub")
                    Publish(client, pubdata, count);
                else
                    Subscribe(client, count);
            }
            catch (Exception ex)
            {
                Error("DOTNET-SOCKET Client", ex);
            }
            finally
            {
                Info("DOTNET-SOCKET Client closing");                
                if (client != null)
                    client.Close().Wait();
                Info("DOTNET-SOCKET Client done");                
            }            
        }


        static void Publish(ISocketClient client, string data, int count)
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
                    Debug("DOTNET-SOCKET Client: Sending {0}", i);
                    client.Send(msg).Wait();
                }
            }
            catch (Exception ex)
            {
                Error("DOTNET-SOCKET Client", ex);
            }
            finally
            {
            }
        }

        static void Subscribe(ISocketClient client, int countlog)
        {
            try
            {
                while (client.IsConnected)
                {                    
                    Debug("DOTNET-SOCKET Client: Waiting for data");
                    client.Receive().Wait();
                }
            }
            catch (Exception ex)
            {
                Error("DOTNET-SOCKET Client", ex);
            }
            finally
            {
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
