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
		event EventHandler<EventArgs<ISocketClient>> Connected;
        event EventHandler<EventArgs<ISocketClient>> Disconnected;
		event EventHandler<EventArgs<Exception>> Error;
        event EventHandler<EventArgs<int>> Sent;
        event EventHandler<SocketDataArgs> ReceivedData;

        IEnumerable<ISocketClient> Clients { get; }

		Task<bool> Open(int port = 0);
        Task<bool> Close();
        Task<ISocketClient> Accept();
        //Task<int> Send(string data);
        //Task<int> Send(byte[] data);
        //Task<int> Flush();        
    }
}
