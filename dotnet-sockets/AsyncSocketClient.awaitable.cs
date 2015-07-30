using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using dotnet_nats.sockets;

namespace dotnet_nats
{
    public class AsyncSocketClient
    {
        const int cBufferSize = 256;
        string _address;
        int _port;
        Socket _socket;
        ConcurrentQueue<byte[]> _tosend;        

        public AsyncSocketClient(string address, int port)
        {
            _address = address;
            _port = port;
            _tosend = new ConcurrentQueue<byte[]>();            
        }
        public AsyncSocketClient() : this(null, -1) { }

        public event EventHandler<TransportConnectionArgs> Connected;
        public event EventHandler<TransportConnectionArgs> Reconnected;
        public event EventHandler<TransportConnectionArgs> Disconnected;
        public event EventHandler<TransportErrorArgs> Error;
        public event EventHandler<TransportDataArgs> Sent;
        public event EventHandler<TransportDataArgs> Received;

        public async Task<bool> Open(string address = null, int port = -1)
        {
            try
            {
                IPEndPoint server = new IPEndPoint(IPAddress.Parse(_address), _port);
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
				args.SocketFlags = SocketFlags.None;
                args.RemoteEndPoint = server;
                var awaitable = new SocketAwaitable(args);
                awaitable.OnCompleted(() => {
                    RaiseConnected();
                    //Flush();                    
                    //Receive();
                });

                _socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);                
                await _socket.ConnectAsync(awaitable);
                return awaitable.IsCompleted;
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                throw;
            }
        }

        public async Task<bool> Close()
        {
            try
            {
                if (_socket != null && _socket.Connected)
                {    
                    await Flush();
                    _socket.Shutdown(SocketShutdown.Both);
					SocketAsyncEventArgs args = new SocketAsyncEventArgs();            
                    var awaitable = new SocketAwaitable(args);
                    awaitable.OnCompleted(() => {
                        RaiseDisconnected();
                        // cancel pending receives
                    });
                    await _socket.DisconnectAsync(awaitable);
                    return !awaitable.IsCompleted;
                }            
				return false;
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                throw;
            }
        }

        public async Task<bool> Send(string data)
        {
            try
            {
                byte[] b = Encoding.UTF8.GetBytes(data);
                return await Send(b);
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                throw;
            }
        }

        public async Task<bool> Send(byte[] data)
        {
            try
            {
                _tosend.Enqueue(data);
                return await Flush();
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                throw;
            }
        }

        public async Task<bool> Flush()
        {
            try
            {
                if (_socket == null)
                    return false;
                if (_tosend.IsEmpty)
                    return false;
                
                byte[] data;
                while (_tosend.TryDequeue(out data))
                {
                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.SetBuffer(data, 0, data.Length);
                    var awaitable = new SocketAwaitable(args);                    
                    awaitable.OnCompleted(() => {
                        RaiseSent(awaitable.GetResult());                        
                    });
                    await _socket.SendAsync(awaitable);                    
                }
                return true;
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                throw;
            }
        }

        #region Receive
        async Task<bool> Receive()
        {
            try
            {
                if (_socket == null)
                    return false;

				SocketAsyncEventArgs args = new SocketAsyncEventArgs();            
				args.SetBuffer(new byte[cBufferSize], 0, cBufferSize);
                var awaitable = new SocketAwaitable(args);
                List<byte> message = new List<byte>();
                int bytes;
                while ((bytes = await _socket.ReceiveAsync(awaitable)) > 0)
                {
                    message.AddRange(args.Buffer.Take(bytes).ToList());
                }
                RaiseReceived(message.ToArray());
                return await Receive();
            }
            catch (Exception ex)
            {
                RaiseError(ex);                
                return false;
            }
        }
        #endregion
        
        #region Events
        void RaiseConnected()
        {
            if (Connected != null)
                Connected(this, new TransportConnectionArgs(true, false));
        }
        void RaiseReconnected()
        {
            if (Reconnected != null)
                Reconnected(this, new TransportConnectionArgs(true, true));
        }
        void RaiseDisconnected()
        {
            if (Disconnected != null)
                Disconnected(this, new TransportConnectionArgs(false, false));
        }        
        void RaiseError(Exception ex)
        {
            if (Error != null)
                Error(this, new TransportErrorArgs(ex));
        }        
        void RaiseSent(int sent)
        {
            if (Sent != null)
                Sent(this, new TransportDataArgs(null, sent));
        }
        void RaiseReceived(byte[] data)
        {
            if (Received != null)
                Received(this, new TransportDataArgs(data, data.Length));
        }
        #endregion
    }
}
