using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotnet_sockets
{
    public interface ISocketClient
    {
		event EventHandler<EventArgs<bool>> Connected;
		event EventHandler<EventArgs<bool>> Reconnected;
        event EventHandler<EventArgs<bool>> Disconnected;
		event EventHandler<EventArgs<Exception>> Error;
		event EventHandler<EventArgs<int>> Sent;
        event EventHandler<SocketDataArgs> ReceivedData;
        event EventHandler<LogEventArgs> Log;

        bool IsConnected { get; }

		Task<bool> Open(string address = null, int port = 0);
        Task<bool> Close();
        Task<int> Send(string data);
        Task<int> Send(byte[] data);
        Task<int> Flush();
        Task<byte[]> Receive();
    }
}
