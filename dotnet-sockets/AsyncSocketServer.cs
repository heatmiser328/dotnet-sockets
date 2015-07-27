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
    // http://codereview.stackexchange.com/questions/31143/performant-c-socket-server
    public class AsyncSocketServer : ISocketServer
    {
        int _port;        
        Socket _socket;
		const int cBackLog = 100;
        IList<ISocketClient> _clients = new List<ISocketClient>();                

        public AsyncSocketServer(int port)
        {
            _port = port;            
        }        
        public AsyncSocketServer() : this(-1) {}

        public event EventHandler<EventArgs<ISocketClient>> Connected;
        public event EventHandler<EventArgs<ISocketClient>> Disconnected;
        public event EventHandler<EventArgs<Exception>> Error;
        public event EventHandler<EventArgs<int>> Sent;
        public event EventHandler<SocketDataArgs> ReceivedData;
        public event EventHandler<LogEventArgs> Log;

        public IEnumerable<ISocketClient> Clients { get { return _clients.ToArray(); } }

        public Task<bool> Open(int port = -1)
        {
            try
            {
                if (_socket != null) return Task<bool>.FromResult(true);
                if (port > 0) _port = port;
                
				//IPAddress hostIP = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
				IPEndPoint ep = new IPEndPoint(IPAddress.Any, _port);
                RaiseDebug("AsyncSocketServer: endpoint = {0}", ep);
                _socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);                                
				_socket.Bind(ep); 				
				_socket.Listen(cBackLog);                
                return Task<bool>.FromResult(true);
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
                    // close clients?
                    _clients.Clear();
                    _socket.Shutdown(SocketShutdown.Both);
                    var tcs = new TaskCompletionSource<bool>(_socket);
                    _socket.BeginDisconnect(true, (ar) =>
                    {
                        try
                        {
                            var t = (TaskCompletionSource<bool>)ar.AsyncState;
                            var s = (Socket)t.Task.AsyncState;
                            try
                            {
                                s.EndDisconnect(ar);                                
                                t.TrySetResult(false);                                                                        
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
                    }, tcs);

                    return tcs.Task;
                }            
				return Task<bool>.FromResult(false);
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                throw;
            }
        }

        public Task<ISocketClient> Accept()
        {
            try
            {
                if (_socket == null)
                    return Task<ISocketClient>.FromResult<ISocketClient>(null);

                var tcs = new TaskCompletionSource<ISocketClient>(_socket);
                _socket.BeginAccept((ar) =>
                {
                    try
                    {
                        var t = (TaskCompletionSource<ISocketClient>)ar.AsyncState;
                        var s = (Socket)t.Task.AsyncState;
                        try
                        {
                            ISocketClient client = new AsyncSocketClient(s.EndAccept(ar));
                            HandleClient(client);                            
                            _clients.Add(client);                            
                            RaiseConnected(client);
                            t.TrySetResult(client);
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
            }
            catch (Exception ex)
            {
                RaiseError(ex);
            }
            return Task<ISocketClient>.FromResult<ISocketClient>(null);
        }

		/*
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
		*/
		
        void HandleClient(ISocketClient client)
        {        
            client.Disconnected += (sender, b) =>
            {
                _clients.Remove(client);
                RaiseDisconnected(client);
            };
            client.Error += (sender, err) =>
            {
                RaiseError(err.Value);
            };
            client.ReceivedData += (sender, args) =>
            {
                RaiseReceivedData(client, args.Data, args.Size);
            };
            client.Sent += (sender, sent) =>
            {
                RaiseSent(sent.Value);
            };

            Task.Factory.StartNew(() => {
                while (client.IsConnected)
                {
                    client.Receive().Wait();
                }
            });
        }

        #region Events
        void RaiseConnected(ISocketClient client)
        {
            if (Connected != null)
                Connected(this, new EventArgs<ISocketClient>(client));
        }
        void RaiseDisconnected(ISocketClient client)
        {
            if (Disconnected != null)
                Disconnected(this, new EventArgs<ISocketClient>(client));
        }        
        void RaiseError(Exception ex)
        {
            if (Error != null)
                Error(this, new EventArgs<Exception>(ex));
        }
        void RaiseSent(int length)
        {
            if (Sent != null)
                Sent(this, new EventArgs<int>(length));
        }
        void RaiseReceivedData(ISocketClient client, byte[] data, int length)
        {
            if (ReceivedData != null)
                ReceivedData(this, new SocketDataArgs(client, data, length));
        }

        void RaiseTrace(string msg, params object[] args)
        {
            RaiseLog("TRACE", msg, null, args);
        }
        void RaiseDebug(string msg, params object[] args)
        {
            RaiseLog("DEBUG", msg, null, args);
        }
        void RaiseInfo(string msg, params object[] args)
        {
            RaiseLog("INFO", msg, null, args);
        }
        void RaiseWarn(string msg, params object[] args)
        {
            RaiseLog("WARN", msg, null, args);
        }
        void RaiseError(string msg, Exception ex, params object[] args)
        {
            RaiseLog("ERROR", msg, ex, args);
        }
        void RaiseFatal(string msg, params object[] args)
        {
            RaiseLog("FATAL", msg, null, args);
        }
        void RaiseLog(string level, string msg, Exception ex = null, params object[] args)
        {
            if (Log != null)
                Log(this, new LogEventArgs(level, msg, ex, args));
        }

        #endregion
    }
}
