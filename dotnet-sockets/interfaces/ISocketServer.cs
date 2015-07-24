using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace dotnet_sockets
{
    public interface ISocketServer
    {
		event EventHandler<ISocketClient> Connected;
        event EventHandler<ISocketClient> Disconnected;
		event EventHandler<Exception> Error;
        event EventHandler<int> Sent;
        event EventHandler<SocketDataArgs> Received;

        IEnumerable<ISocketClient> Clients { get; }

		Task<bool> Open(int port = 0);
        Task<bool> Close();
        Task<ISocketClient> Accept();
        //Task<int> Send(string data);
        //Task<int> Send(byte[] data);
        //Task<int> Flush();        
    }
}
