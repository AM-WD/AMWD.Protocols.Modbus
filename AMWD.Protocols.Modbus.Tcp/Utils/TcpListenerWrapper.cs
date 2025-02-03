using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AMWD.Protocols.Modbus.Tcp.Utils
{
	/// <inheritdoc cref="TcpListener" />
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal class TcpListenerWrapper(IPAddress localaddr, int port) : IDisposable
	{
		#region Fields

		private readonly TcpListener _tcpListener = new(localaddr, port);

		#endregion Fields

		#region Constructor

		#endregion Constructor

		#region Properties

		/// <inheritdoc cref="TcpListener.LocalEndpoint"/>
		public virtual IPEndPointWrapper LocalIPEndPoint
			=> new(_tcpListener.LocalEndpoint);

		public virtual SocketWrapper Socket
			=> new(_tcpListener.Server);

		#endregion Properties

		#region Methods

		/// <summary>
		/// Accepts a pending connection request as a cancellable asynchronous operation.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This operation will not block. The returned <see cref="Task{TResult}"/> object will complete after the TCP connection has been accepted.
		/// </para>
		/// <para>
		/// Use the <see cref="TcpClientWrapper.GetStream"/> method to obtain the underlying <see cref="NetworkStreamWrapper"/> of the returned <see cref="TcpClientWrapper"/> in the <see cref="Task{TResult}"/>.
		/// The <see cref="NetworkStreamWrapper"/> will provide you with methods for sending and receiving with the remote host.
		/// When you are through with the <see cref="TcpClientWrapper"/>, be sure to call its <see cref="TcpClientWrapper.Close"/> method.
		/// </para>
		/// </remarks>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation</param>
		/// <returns>
		/// The task object representing the asynchronous operation.
		/// The <see cref="Task{TResult}.Result"/> property on the task object returns a <see cref="TcpClientWrapper"/> used to send and receive data.
		/// </returns>
		/// <exception cref="InvalidOperationException">The listener has not been started with a call to <see cref="Start"/>.</exception>
		/// <exception cref="SocketException">
		/// Use the <see cref="SocketException.ErrorCode"/> property to obtain the specific error code.
		/// When you have obtained this code, you can refer to the
		/// <see href="https://learn.microsoft.com/en-us/windows/desktop/winsock/windows-sockets-error-codes-2">Windows Sockets version 2 API error code</see>
		/// documentation for a detailed description of the error.
		/// </exception>
		/// <exception cref="OperationCanceledException">The cancellation token was canceled. This exception is stored into the returned task.</exception>
		public virtual async Task<TcpClientWrapper> AcceptTcpClientAsync(CancellationToken cancellationToken = default)
		{
#if NET8_0_OR_GREATER
			var tcpClient = await _tcpListener.AcceptTcpClientAsync(cancellationToken);
#else
			var tcpClient = await _tcpListener.AcceptTcpClientAsync();
#endif
			return new TcpClientWrapper(tcpClient);
		}

		public virtual void Start()
			=> _tcpListener.Start();

		public virtual void Stop()
			=> _tcpListener.Stop();

		public virtual void Dispose()
		{
#if NET8_0_OR_GREATER
			_tcpListener.Dispose();
#endif
		}

		#endregion Methods
	}
}
