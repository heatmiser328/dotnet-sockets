using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace dotnet_sockets
{
    public class AsyncSocketClient : ISocketClient
    {
        string _address;
        int _port;
        ILog _log;
        Socket _socket;
        ConcurrentQueue<byte[]> _tosend = new ConcurrentQueue<byte[]>();

        public AsyncSocketClient(Socket socket, ILog log)
        {
            _socket = socket;
            _log = log;            
        }
        public AsyncSocketClient(Socket socket) : this(socket, new log.ConsoleLog()) {}
        public AsyncSocketClient(string address, int port, ILog log)
        {
            _address = address;
            _port = port;
            _log = log;            
        }
        public AsyncSocketClient(string address, int port) : this(address, port, new log.ConsoleLog()) {}
        public AsyncSocketClient() : this(null, -1) {}

        public event EventHandler<bool> Connected;
        public event EventHandler<bool> Reconnected;
        public event EventHandler<bool> Disconnected;
        public event EventHandler<Exception> Error;
        public event EventHandler<int> Sent;
        public event EventHandler<SocketDataArgs> Received;

        public bool IsConnected { get {return _socket != null ? _socket.Connected : false; }}

        public Task<bool> Open(string address = null, int port = -1)
        {
            try
            {
                if (_socket != null) return Task<bool>.FromResult(true);
                if (address != null) _address = address;
                if (port > 0) _port = port;
                _address = _address ?? Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();

                IPEndPoint server = new IPEndPoint(IPAddress.Parse(_address), _port);
                _socket = new Socket(server.AddressFamily, SocketType.Stream, ProtocolType.Tcp);                                
				var tcs = new TaskCompletionSource<bool>(_socket); 
                _socket.BeginConnect(server, (ar) => {
		            try
		            {
						var t = (TaskCompletionSource<bool>)ar.AsyncState; 
						var s = (Socket)t.Task.AsyncState; 
						try {
                            s.EndConnect(ar);                            
							t.TrySetResult(ar.IsCompleted); 
			                RaiseConnected();
						} 
						catch (Exception exc) { 
							RaiseError(exc);
							t.TrySetException(exc); 
						} 
		            }
		            catch (Exception ex)
		            {
		                RaiseError(ex);
                        tcs.TrySetException(ex);
		            }
				}, tcs);
				
				return tcs.Task;
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                throw;
            }
        }

        public Task<bool> Close()
        {
            try
            {
                if (_socket != null && _socket.Connected)
                {    
                    return Flush().ContinueWith((antecedent) => {
                        _socket.Shutdown(SocketShutdown.Both);
                        var tcs = new TaskCompletionSource<bool>(_socket);
                        _socket.BeginDisconnect(false, (ar) =>
                        {
                            try
                            {
                                var t = (TaskCompletionSource<bool>)ar.AsyncState;
                                var s = (Socket)t.Task.AsyncState;
                                try
                                {
                                    s.EndDisconnect(ar);
                                    t.TrySetResult(false);
                                    RaiseDisconnected();
                                    // cancel pending receives
                                }
                                catch (Exception exc)
                                {
                                    RaiseError(exc);
                                    t.TrySetException(exc);
                                }
                            }
                            catch (Exception ex)
                            {
                                RaiseError(ex);
                                tcs.TrySetException(ex);
                            }
                        }, tcs);

                        return tcs.Task;
                    }).Result;
                }            
				return Task<bool>.FromResult(false);
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                throw;
            }
        }

        public Task<int> Send(string data)
        {
            try
            {
                byte[] b = Encoding.UTF8.GetBytes(data);
                return Send(b);
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                throw;
            }
        }

        public Task<int> Send(byte[] data)
        {
            try
            {
                _tosend.Enqueue(data);
                return Flush();
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                throw;
            }
        }

        public Task<int> Flush()
        {
            try
            {
                if (_socket == null)
                    return Task<int>.FromResult(0);
                if (_tosend.IsEmpty)
                    return Task<int>.FromResult(0);
                
            	var tcs = new TaskCompletionSource<int>(_socket);
                List<byte> send = new List<byte>();
                byte[] data;
                while (_tosend.TryDequeue(out data))
                {
                    send.AddRange(data);                    
                }
                if (send.Count > 0)
                {
                    data = send.ToArray();
                    _socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) => {
			            try
			            {
							var t = (TaskCompletionSource<int>)ar.AsyncState; 
							var s = (Socket)t.Task.AsyncState; 
							try {
                                int sent = s.EndSend(ar);
								t.TrySetResult(sent); 
				                RaiseSent(sent);
							} 
							catch (Exception exc) { 
								RaiseError(exc);
								t.TrySetException(exc); 
							} 
			            }
			            catch (Exception ex)
			            {
			                RaiseError(ex);
			            }
					}, tcs);					
                }
				else
				{
					tcs.TrySetResult(0);					
				}
                return tcs.Task;				
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                throw;
            }
        }

        public Task<byte[]> Receive()
		{
            return DoReceive();
		}
		
        #region Receive
        class ReceiveState
        {
            public ReceiveState(Socket s) { socket = s; Task = new TaskCompletionSource<byte[]>(socket);}
            public Socket socket;
            public TaskCompletionSource<byte[]> Task;
            public const int BufferSize = 1024 * 1024;
            public byte[] buffer = new byte[BufferSize];
            public List<byte> message = new List<byte>();
        }

        Task<byte[]> DoReceive(ReceiveState state = null)
        {
            try
            {
                if (_socket == null)
                    return Task<byte[]>.FromResult(new byte[]{});

                if (state == null)
                    state = new ReceiveState(_socket);
                _socket.BeginReceive(state.buffer, 0, ReceiveState.BufferSize, 0, (ar) => {
		            try
		            {
		                ReceiveState st = (ReceiveState)ar.AsyncState;
                        var t = st.Task;
                        var s = (Socket)t.Task.AsyncState;
                        try
                        {
                            int received = st.socket.EndReceive(ar);
                            if (received > 0)
                            {
                                _log.Debug("AsyncSocketClient: Received {0}", received);
                                state.message.AddRange(st.buffer.Take(received).ToList());                                
                            }
                            if (received < ReceiveState.BufferSize)
                            {
                                var data = st.message.ToArray();
                                t.TrySetResult(data);
                                if (data.Length > 0)
                                {
                                    _log.Debug("AsyncSocketClient: Received message {0}", data.Length);
                                    RaiseReceived(data, data.Length);
                                }
                            }                                
                            else
                            {
                                DoReceive(state).Wait(); // read the remaining 
                            }                                                                    
                        }
                        catch (Exception exc)
                        {
                            RaiseError(exc);
                            t.TrySetException(exc);
                        } 
		            }
		            catch (Exception ex)
		            {
		                RaiseError(ex);
		            }
				}, state);
                return state.Task.Task;
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                return Task<byte[]>.FromResult(new byte[] { });
            }
        }
        #endregion
        
        #region Events
        void RaiseConnected()
        {
            if (Connected != null)
                Connected(this, true);
        }
        void RaiseReconnected()
        {
            if (Reconnected != null)
                Reconnected(this, true);
        }
        void RaiseDisconnected()
        {
            if (Disconnected != null)
                Disconnected(this, false);
        }        
        void RaiseError(Exception ex)
        {
            if (Error != null)
                Error(this, ex);
        }
        void RaiseSent(int length)
        {
            if (Sent != null)
                Sent(this, length);
        }
        void RaiseReceived(byte[] data, int length)
        {
            if (Received != null)
                Received(this, new SocketDataArgs(this, data, length));
        }
        #endregion
    }
}
