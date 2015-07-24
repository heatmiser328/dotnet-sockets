using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;
using Shouldly;

using mocks;

using dotnet_sockets;

namespace transports
{
    public class AsyncSocketClientTest : IDisposable
    {        
        MockTcpServer _server;
        AsyncSocketClient _client;
        ILog _log;

        public AsyncSocketClientTest(ITestOutputHelper output)
        {
            _log = new MockLog(output);
            _server = new MockTcpServer(_log);
            _log.Debug("AsyncSocketClientTest: Start mock server");
            _server.Start();
            _client = new AsyncSocketClient("127.0.0.1", MockTcpServer.cPort);            
        }
        public void Dispose()
        {
            _client.Close();
            _server.Stop();
        }
				

        [Fact]
        public async Task Open()
        {            
            var connections = 0;
            Exception exception = null;
            _client.Error += new EventHandler<TransportErrorArgs>((sender, args) => {
                exception = args.Exception;
            });
            _client.Connected += new EventHandler<TransportConnectionArgs>((sender, args) => {
                connections++;
                args.Connected.ShouldBe(true);
                args.Reconnected.ShouldBe(false);
                          
            });
            var connected = await _client.Open();
            connected.ShouldBe(true);
            connections.ShouldBe(1);            
            exception.ShouldBe(null);
        }
		
        [Fact]
        public void Open_Fail()
        {            
            var connections = 0;
            Exception exception = null;
            _client.Error += new EventHandler<TransportErrorArgs>((sender, args) => {
                exception = args.Exception;
            });
            _client.Connected += new EventHandler<TransportConnectionArgs>((sender, args) => {
                connections++;
                args.Connected.ShouldBe(true);
                args.Reconnected.ShouldBe(false);
                          
            });
            //var connected = ;
            Should.Throw<System.Net.Sockets.SocketException>(() => {
                return _client.Open(null, MockTcpServer.cPort + 1);                
            });
            //connected.ShouldBe(false);
            connections.ShouldBe(0);            
            exception.ShouldNotBe(null);
        }
		        
        //[Fact]
        public async Task Close()
        {            
            var connections = 0;
            Exception exception = null;
            _client.Error += new EventHandler<TransportErrorArgs>((sender, args) => {
                exception = args.Exception;
            });
            _client.Connected += new EventHandler<TransportConnectionArgs>((sender, args) => {
                connections++;
                args.Connected.ShouldBe(true);
                args.Reconnected.ShouldBe(false);                          
            });
            _client.Disconnected += new EventHandler<TransportConnectionArgs>((sender, args) => {
                connections--;
                args.Connected.ShouldBe(false);
                args.Reconnected.ShouldBe(false);                          
            });
			
            var connected = await _client.Open();
			//System.Threading.Thread.Sleep(100);
            connected.ShouldBe(true);
            connections.ShouldBe(1);            
            exception.ShouldBe(null);
            /*
            connected = await _client.Close();
            connected.ShouldBe(true);
            connections.ShouldBe(0);
            exception.ShouldBe(null);
            */
        }
		
		
        //void Send(string data);
        //void Send(byte[] data);
        //void Flush();
    }
}
