using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotnet_sockets
{
    public class SocketDataArgs : EventArgs
    {
        public SocketDataArgs(ISocketClient client, byte[] data, int size) { Client = client; Data = data; Size = size; }
        public ISocketClient Client { get; private set; }
        public byte[] Data { get; private set; }
        public int Size { get; private set; }
    }
}
