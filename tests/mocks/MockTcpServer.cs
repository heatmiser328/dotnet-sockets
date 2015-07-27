using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using dotnet_sockets;

namespace mocks
{
    internal class MockTcpServer
    {
        public const int cPort = 8877;        
        int _port = cPort;
        TcpListener _listener;

        public MockTcpServer(int port = cPort)
        {            
            _port = port;
            _listener = new TcpListener(IPAddress.Any, _port);
            Reset();
        }

        public int Connections { get; private set; }
        public IList<byte[]> Received { get; private set; }

        public void Reset()
        {            
            //_log.Debug("MockTcpServer.Reset");
            Connections = 0;
            Received = new List<byte[]>();
        }

        public void Start()
        {
            //_log.Debug("MockTcpServer.Start: Start listener");
            _listener.Start();
            AcceptConnection();            
        }

        public void Stop()
        {
            //_log.Debug("MockTcpServer.Stop: Stop listener");
            _listener.Stop();
        }

        private void AcceptConnection()
        {
            try
            {
                //_log.Debug("MockTcpServer.Connect: Wait for socket connection");
                _listener.BeginAcceptSocket((ar) => {
                    TcpListener l = (TcpListener)ar.AsyncState;
                    if (l.Server.Connected)
                    {
                        Socket s = l.EndAcceptSocket(ar);
                        //_log.Debug("MockTcpServer.Connect: Socket connected");
                        //Receive(new ReceiveState(s));
                        //AcceptConnection();
                    }
                }, _listener);
            }
            catch{}
        }

        private class ReceiveState
        {
            public ReceiveState(Socket s) { socket = s; }
            public Socket socket;
            public const int BufferSize = 256;
            public byte[] buffer = new byte[BufferSize];
            public List<byte> message = new List<byte>();
        }
        private void Receive(ReceiveState st)
        {
            try
            {
                //_log.Debug("MockTcpServer.Receive: Wait for data from Socket connection");
                st.socket.BeginReceive(st.buffer, 0, ReceiveState.BufferSize, SocketFlags.None, (ar) => {
                    ReceiveState state = (ReceiveState)ar.AsyncState;
                    int received = state.socket.EndReceive(ar);
                    //_log.Debug(string.Format("MockTcpServer.Receive: Read {0} from Socket connection", received));
                    if (received > 0)
                    {
                        state.message.AddRange(state.buffer.Take(received).ToList());
                        Receive(state); // read the remaining
                    }
                    else
                    {
                        //_log.Debug("MockTcpServer.Receive: Read message from Socket connection");
                        Received.Add(state.message.ToArray());
                        Receive(new ReceiveState(state.socket));
                    }
                }, st);
            }
            catch { }
        }
    }
}
