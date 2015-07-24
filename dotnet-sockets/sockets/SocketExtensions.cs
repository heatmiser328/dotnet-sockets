using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace dotnet_sockets
{
	//http://blogs.msdn.com/b/pfxteam/archive/2011/12/15/10248293.aspx
	public static class SocketExtensions 
	{ 
	    public static SocketAwaitable ConnectAsync(this Socket socket, 
			SocketAwaitable awaitable) 
	    { 
	        awaitable.Reset(); 
	        if (!socket.ConnectAsync(awaitable.m_eventArgs)) 
	            awaitable.m_wasCompleted = true; 
	        return awaitable; 
	    }
	
	    public static SocketAwaitable DisconnectAsync(this Socket socket, 
			SocketAwaitable awaitable) 
	    { 
	        awaitable.Reset(); 
	        if (!socket.DisconnectAsync(awaitable.m_eventArgs)) 
	            awaitable.m_wasCompleted = true; 
	        return awaitable; 
	    }
	
	    public static SocketAwaitable ReceiveAsync(this Socket socket, 
	        SocketAwaitable awaitable) 
	    { 
	        awaitable.Reset(); 
	        if (!socket.ReceiveAsync(awaitable.m_eventArgs)) 
	            awaitable.m_wasCompleted = true; 
	        return awaitable; 
	    }

	    public static SocketAwaitable SendAsync(this Socket socket, 
	        SocketAwaitable awaitable) 
	    { 
	        awaitable.Reset(); 
	        if (!socket.SendAsync(awaitable.m_eventArgs)) 
	            awaitable.m_wasCompleted = true; 
	        return awaitable; 
	    }
	}
}
