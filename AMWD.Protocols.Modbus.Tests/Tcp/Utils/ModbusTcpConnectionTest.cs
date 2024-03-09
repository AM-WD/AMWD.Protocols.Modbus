using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.Modbus.Common.Contracts;
using AMWD.Protocols.Modbus.Tcp;
using AMWD.Protocols.Modbus.Tcp.Utils;
using Moq;

namespace AMWD.Protocols.Modbus.Tests.Tcp.Utils
{
	[TestClass]
	public class ModbusTcpConnectionTest
	{
		private string _hostname = "127.0.0.1";

		private Mock<TcpClientWrapper> _tcpClientMock;
		private Mock<NetworkStreamWrapper> _networkStreamMock;
		private Mock<SocketWrapper> _socketMock;

		private bool _clientIsAlwaysConnected;
		private Queue<bool> _clientIsConnectedQueue;

		private int _clientReceiveTimeout = 1000;
		private int _clientSendTimeout = 1000;
		private Task _clientConnectTask = Task.CompletedTask;

		private List<byte[]> _networkRequestCallbacks;

		private Queue<byte[]> _networkResponseQueue;

		[TestInitialize]
		public void Initialize()
		{
			_clientIsAlwaysConnected = true;
			_clientIsConnectedQueue = new Queue<bool>();

			_networkRequestCallbacks = [];
			_networkResponseQueue = new Queue<byte[]>();
		}

		[TestMethod]
		public void ShouldGetAndSetPropertiesOfBaseClient()
		{
			// Arrange
			_clientIsAlwaysConnected = false;
			_clientIsConnectedQueue.Enqueue(true);
			var connection = GetTcpConnection();
			connection.GetType().GetField("_isConnected", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(connection, true);

			// Act
			connection.ReadTimeout = TimeSpan.FromSeconds(123);
			connection.WriteTimeout = TimeSpan.FromSeconds(456);

			// Assert - part 1
			Assert.AreEqual("TCP", connection.Name);
			Assert.AreEqual(1, connection.ReadTimeout.TotalSeconds);
			Assert.AreEqual(1, connection.WriteTimeout.TotalSeconds);
			Assert.IsTrue(connection.IsConnected);

			// Assert - part 2
			_tcpClientMock.VerifySet(c => c.ReceiveTimeout = 123000, Times.Once);
			_tcpClientMock.VerifySet(c => c.SendTimeout = 456000, Times.Once);

			_tcpClientMock.VerifyGet(c => c.ReceiveTimeout, Times.Once);
			_tcpClientMock.VerifyGet(c => c.SendTimeout, Times.Once);
			_tcpClientMock.VerifyGet(c => c.Connected, Times.Once);

			_socketMock.VerifyNoOtherCalls();
			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[DataTestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("   ")]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ShouldThrowArumentNullExceptionForInvalidHostname(string hostname)
		{
			// Arrange
			var connection = GetTcpConnection();

			// Act
			connection.Hostname = hostname;

			// Assert - ArgumentNullException
		}

		[DataTestMethod]
		[DataRow(0)]
		[DataRow(65536)]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void ShouldThrowArumentOutOfRangeExceptionForInvalidPort(int port)
		{
			// Arrange
			var connection = GetTcpConnection();

			// Act
			connection.Port = port;

			// Assert - ArgumentOutOfRangeException
		}

		[TestMethod]
		public async Task ShouldConnectAsync()
		{
			// Arrange
			var connection = GetConnection();

			// Act
			await connection.ConnectAsync();

			// Assert
			Assert.IsTrue(connection.IsConnected);

			_tcpClientMock.Verify(c => c.Close(), Times.Once);
			_tcpClientMock.Verify(c => c.ConnectAsync(IPAddress.Loopback, 502, It.IsAny<CancellationToken>()), Times.Once);
			_tcpClientMock.VerifyGet(c => c.ReceiveTimeout, Times.Once);
			_tcpClientMock.VerifyGet(c => c.Connected, Times.Exactly(2));
			_tcpClientMock.VerifyGet(c => c.Client, Times.Exactly(3));

			_socketMock.Verify(s => s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, false), Times.Once);
			_socketMock.Verify(s => s.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 0), Times.Once);
			_socketMock.Verify(s => s.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 0), Times.Once);

			_socketMock.VerifyNoOtherCalls();
			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldOnlyConnectAsyncOnce()
		{
			// Arrange
			var connection = GetConnection();

			await connection.ConnectAsync();
			ClearInvocations();

			// Act
			await connection.ConnectAsync();

			// Assert
			Assert.IsTrue(connection.IsConnected);

			_tcpClientMock.VerifyGet(c => c.Connected, Times.Once);

			_socketMock.VerifyNoOtherCalls();

			_socketMock.VerifyNoOtherCalls();
			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		[ExpectedException(typeof(ApplicationException))]
		public async Task ShouldThrowApplicationExceptionHostnameNotResolvable()
		{
			// Arrange
			_hostname = "device.internal";
			var connection = GetConnection();

			// Act
			await connection.ConnectAsync();

			// Assert - ApplicationException
		}

		[TestMethod]
		public async Task ShouldRetryConnectAsync()
		{
			// Arrange
			_clientIsAlwaysConnected = false;
			_clientIsConnectedQueue.Enqueue(false);
			_clientIsConnectedQueue.Enqueue(true);
			_clientIsConnectedQueue.Enqueue(true);
			var connection = GetConnection();

			// Act
			await connection.ConnectAsync();

			// Assert
			Assert.IsTrue(connection.IsConnected);

			_tcpClientMock.Verify(c => c.Close(), Times.Exactly(2));
			_tcpClientMock.Verify(c => c.ConnectAsync(IPAddress.Loopback, 502, It.IsAny<CancellationToken>()), Times.Exactly(2));
			_tcpClientMock.VerifyGet(c => c.ReceiveTimeout, Times.Exactly(2));
			_tcpClientMock.VerifyGet(c => c.Connected, Times.Exactly(3));
			_tcpClientMock.VerifyGet(c => c.Client, Times.Exactly(3));

			_socketMock.Verify(s => s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, false), Times.Once);
			_socketMock.Verify(s => s.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 0), Times.Once);
			_socketMock.Verify(s => s.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 0), Times.Once);

			_socketMock.VerifyNoOtherCalls();
			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		[ExpectedException(typeof(SocketException))]
		public async Task ShouldThrowSocketExceptionOnConnectAsyncForNoReconnect()
		{
			// Arrange
			_clientIsAlwaysConnected = false;
			_clientIsConnectedQueue.Enqueue(false);
			var connection = GetTcpConnection();
			connection.ReconnectTimeout = TimeSpan.Zero;

			// Act
			await connection.ConnectAsync();

			// Assert - SocketException
		}

		[TestMethod]
		public async Task ShouldDisconnectAsync()
		{
			// Arrange
			var connection = GetConnection();

			await connection.ConnectAsync();
			ClearInvocations();

			// Act
			await connection.DisconnectAsync();

			// Assert
			Assert.IsFalse(connection.IsConnected);

			_tcpClientMock.Verify(c => c.Close(), Times.Once);
			_tcpClientMock.VerifyNoOtherCalls();

			_socketMock.VerifyNoOtherCalls();
			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldOnlyDisconnectAsyncOnce()
		{
			// Arrange
			var connection = GetConnection();

			await connection.ConnectAsync();
			await connection.DisconnectAsync();
			ClearInvocations();

			// Act
			await connection.DisconnectAsync();

			// Assert
			Assert.IsFalse(connection.IsConnected);

			_socketMock.VerifyNoOtherCalls();
			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldCallDisconnectOnDispose()
		{
			// Arrange
			var connection = GetConnection();

			await connection.ConnectAsync();
			ClearInvocations();

			// Act
			connection.Dispose();

			// Assert
			_tcpClientMock.Verify(c => c.Close(), Times.Once);
			_tcpClientMock.Verify(c => c.Dispose(), Times.Once);
			_tcpClientMock.VerifyNoOtherCalls();

			_socketMock.VerifyNoOtherCalls();
			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldAllowMultipleDispose()
		{
			// Arrange
			var connection = GetConnection();

			// Act
			connection.Dispose();
			connection.Dispose();

			// Assert
			_tcpClientMock.Verify(c => c.Close(), Times.Once);
			_tcpClientMock.Verify(c => c.Dispose(), Times.Once);
			_tcpClientMock.VerifyNoOtherCalls();

			_socketMock.VerifyNoOtherCalls();
			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		[ExpectedException(typeof(ApplicationException))]
		public async Task ShouldThrowApplicationExceptionOnInvokeAsyncWhileNotConnected()
		{
			// Arrange
			var connection = GetConnection();

			// Act
			await connection.InvokeAsync(null, null);

			// Assert - ApplicationException
		}

		[DataTestMethod]
		[DataRow(null)]
		[DataRow(new byte[0])]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task ShouldThrowArgumentNullExceptionOnInvokeAsyncForRequest(byte[] request)
		{
			// Arrange
			var connection = GetConnection();
			await connection.ConnectAsync();

			// Act
			await connection.InvokeAsync(request, null);

			// Assert - ArgumentNullException
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task ShouldThrowArgumentNullExceptionOnInvokeAsyncForMissingValidation()
		{
			// Arrange
			byte[] request = new byte[1];

			var connection = GetConnection();
			await connection.ConnectAsync();

			// Act
			await connection.InvokeAsync(request, null);

			// Assert - ArgumentNullException
		}

		[TestMethod]
		public async Task ShouldInvokeAsync()
		{
			// Arrange
			_networkResponseQueue.Enqueue([9, 8, 7]);
			byte[] request = [1, 2, 3];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);

			var connection = GetConnection();
			await connection.ConnectAsync();
			ClearInvocations();

			// Act
			var response = await connection.InvokeAsync(request, validation);

			// Assert
			Assert.AreEqual(1, _networkRequestCallbacks.Count);

			CollectionAssert.AreEqual(new byte[] { 9, 8, 7 }, response.ToArray());
			CollectionAssert.AreEqual(request, _networkRequestCallbacks[0]);

			_tcpClientMock.Verify(c => c.Connected, Times.Once);
			_tcpClientMock.Verify(c => c.GetStream(), Times.Once);

			_networkStreamMock.Verify(ns => ns.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);

			_socketMock.VerifyNoOtherCalls();
			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		[ExpectedException(typeof(EndOfStreamException))]
		public async Task ShouldThrowEndOfStreamOnInvokeAsync()
		{
			// Arrange
			byte[] request = [1, 2, 3];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);

			var connection = GetConnection();
			await connection.ConnectAsync();
			ClearInvocations();

			// Act
			_ = await connection.InvokeAsync(request, validation);

			// Assert - EndOfStreamException
		}

		[TestMethod]
		[ExpectedException(typeof(TaskCanceledException))]
		public async Task ShouldCancelOnInvokeAsyncOnDisconnect()
		{
			// Arrange
			byte[] request = [1, 2, 3];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);

			var connection = GetConnection();
			_networkStreamMock
				.Setup(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
				.Returns(new ValueTask(Task.Delay(100)));

			await connection.ConnectAsync();
			ClearInvocations();

			// Act
			var task = connection.InvokeAsync(request, validation);
			await connection.DisconnectAsync();
			await task;

			// Assert - TaskCanceledException
		}

		[TestMethod]
		[ExpectedException(typeof(TaskCanceledException))]
		public async Task ShouldCancelOnInvokeAsyncOnAbort()
		{
			// Arrange
			byte[] request = [1, 2, 3];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);
			var cts = new CancellationTokenSource();

			var connection = GetConnection();
			_networkStreamMock
				.Setup(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
				.Returns(new ValueTask(Task.Delay(100)));

			await connection.ConnectAsync();
			ClearInvocations();

			// Act
			var task = connection.InvokeAsync(request, validation, cts.Token);
			cts.Cancel();
			await task;

			// Assert - TaskCanceledException
		}

		[DataTestMethod]
		[DataRow(typeof(IOException))]
		[DataRow(typeof(SocketException))]
		[DataRow(typeof(TimeoutException))]
		[DataRow(typeof(InvalidOperationException))]
		public async Task ShouldReconnectOnInvokeAsyncForExceptionType(Type exceptionType)
		{
			// Arrange
			_networkResponseQueue.Enqueue([9, 8, 7]);
			byte[] request = [1, 2, 3];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);

			var connection = GetConnection();
			await connection.ConnectAsync();
			ClearInvocations();

			_networkStreamMock
				.Setup(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
				.Callback<ReadOnlyMemory<byte>, CancellationToken>((req, _) => _networkRequestCallbacks.Add(req.ToArray()))
				.ThrowsAsync((Exception)Activator.CreateInstance(exceptionType));

			// Act
			try
			{
				await connection.InvokeAsync(request, validation);
			}
			catch (Exception ex)
			{
				// Assert - part 1
				Assert.IsInstanceOfType(ex, exceptionType);
			}

			// Assert - part 2
			Assert.AreEqual(1, _networkRequestCallbacks.Count);
			CollectionAssert.AreEqual(request, _networkRequestCallbacks[0]);

			_tcpClientMock.Verify(c => c.Close(), Times.Once);
			_tcpClientMock.Verify(c => c.ConnectAsync(IPAddress.Loopback, 502, It.IsAny<CancellationToken>()), Times.Once);
			_tcpClientMock.VerifyGet(c => c.ReceiveTimeout, Times.Once);
			_tcpClientMock.VerifyGet(c => c.Connected, Times.Exactly(2));
			_tcpClientMock.VerifyGet(c => c.Client, Times.Exactly(3));
			_tcpClientMock.Verify(c => c.GetStream(), Times.Once);

			_networkStreamMock.Verify(ns => ns.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);

			_socketMock.Verify(s => s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, false), Times.Once);
			_socketMock.Verify(s => s.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 0), Times.Once);
			_socketMock.Verify(s => s.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 0), Times.Once);

			_socketMock.VerifyNoOtherCalls();
			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnWithUnknownExceptionOnInvokeAsync()
		{
			// Arrange
			byte[] request = [1, 2, 3];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);

			var connection = GetConnection();
			await connection.ConnectAsync();
			ClearInvocations();

			_networkStreamMock
				.Setup(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
				.Callback<ReadOnlyMemory<byte>, CancellationToken>((req, _) => _networkRequestCallbacks.Add(req.ToArray()))
				.ThrowsAsync(new NotImplementedException());

			// Act
			try
			{
				await connection.InvokeAsync(request, validation);
			}
			catch (Exception ex)
			{
				// Assert - part 1
				Assert.IsInstanceOfType(ex, typeof(NotImplementedException));
			}

			// Assert - part 2
			Assert.AreEqual(1, _networkRequestCallbacks.Count);
			CollectionAssert.AreEqual(request, _networkRequestCallbacks[0]);

			_tcpClientMock.Verify(c => c.Connected, Times.Once);
			_tcpClientMock.Verify(c => c.GetStream(), Times.Once);

			_networkStreamMock.Verify(ns => ns.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);

			_socketMock.VerifyNoOtherCalls();
			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldRemoveRequestFromQueueOnInvokeAsync()
		{
			// Arrange
			_networkResponseQueue.Enqueue([9, 8, 7]);
			byte[] request = [1, 2, 3];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);

			var connection = GetConnection();
			await connection.ConnectAsync();
			ClearInvocations();

			_networkStreamMock
				.Setup(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
				.Callback<ReadOnlyMemory<byte>, CancellationToken>((req, _) => _networkRequestCallbacks.Add(req.ToArray()))
				.Returns(new ValueTask(Task.Delay(100)));

			var cts = new CancellationTokenSource();

			// Act
			var taskToComplete = connection.InvokeAsync(request, validation);

			var taskToCancel = connection.InvokeAsync(request, validation, cts.Token);
			cts.Cancel();

			var response = await taskToComplete;

			// Assert
			try
			{
				await taskToCancel;
				Assert.Fail();
			}
			catch (TaskCanceledException)
			{ /* expected exception */ }

			Assert.AreEqual(1, _networkRequestCallbacks.Count);

			CollectionAssert.AreEqual(new byte[] { 9, 8, 7 }, response.ToArray());
			CollectionAssert.AreEqual(request, _networkRequestCallbacks[0]);

			_tcpClientMock.Verify(c => c.Connected, Times.Exactly(2));
			_tcpClientMock.Verify(c => c.GetStream(), Times.Once);

			_networkStreamMock.Verify(ns => ns.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);

			_socketMock.VerifyNoOtherCalls();
			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldCancelQueuedRequestOnDisconnect()
		{
			// Arrange
			_networkResponseQueue.Enqueue([9, 8, 7]);
			byte[] request = [1, 2, 3];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);

			var connection = GetConnection();
			await connection.ConnectAsync();
			ClearInvocations();

			_networkStreamMock
				.Setup(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
				.Callback<ReadOnlyMemory<byte>, CancellationToken>((req, _) => _networkRequestCallbacks.Add(req.ToArray()))
				.Returns(new ValueTask(Task.Delay(100)));

			var cts = new CancellationTokenSource();

			// Act
			var taskToCancel = connection.InvokeAsync(request, validation);
			var taskToDequeue = connection.InvokeAsync(request, validation);
			await connection.DisconnectAsync();

			// Assert
			try
			{
				await taskToCancel;
				Assert.Fail();
			}
			catch (TaskCanceledException ex)
			{
				/* expected exception */
				Assert.AreNotEqual(CancellationToken.None, ex.CancellationToken);
			}

			try
			{
				await taskToDequeue;
				Assert.Fail();
			}
			catch (TaskCanceledException ex)
			{
				/* expected exception */
				Assert.AreEqual(CancellationToken.None, ex.CancellationToken);
			}

			Assert.AreEqual(1, _networkRequestCallbacks.Count);
			CollectionAssert.AreEqual(request, _networkRequestCallbacks[0]);

			_tcpClientMock.Verify(c => c.Connected, Times.Exactly(2));
			_tcpClientMock.Verify(c => c.GetStream(), Times.Once);
			_tcpClientMock.Verify(c => c.Close(), Times.Once);

			_networkStreamMock.Verify(ns => ns.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);

			_socketMock.VerifyNoOtherCalls();
			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		private IModbusConnection GetConnection()
			=> GetTcpConnection();

		private ModbusTcpConnection GetTcpConnection()
		{
			_networkStreamMock = new Mock<NetworkStreamWrapper>();
			_networkStreamMock
				.Setup(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
				.Callback<ReadOnlyMemory<byte>, CancellationToken>((req, _) => _networkRequestCallbacks.Add(req.ToArray()))
				.Returns(ValueTask.CompletedTask);
			_networkStreamMock
				.Setup(ns => ns.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
				.Returns<Memory<byte>, CancellationToken>((buffer, _) =>
				{
					if (_networkResponseQueue.TryDequeue(out byte[] bytes))
					{
						bytes.CopyTo(buffer);
						return ValueTask.FromResult(bytes.Length);
					}

					return ValueTask.FromResult(0);
				});

			_socketMock = new Mock<SocketWrapper>();

			_tcpClientMock = new Mock<TcpClientWrapper>();
			_tcpClientMock.Setup(c => c.Client).Returns(() => _socketMock.Object);
			_tcpClientMock.Setup(c => c.Connected).Returns(() => _clientIsAlwaysConnected || _clientIsConnectedQueue.Dequeue());
			_tcpClientMock.Setup(c => c.ReceiveTimeout).Returns(() => _clientReceiveTimeout);
			_tcpClientMock.Setup(c => c.SendTimeout).Returns(() => _clientSendTimeout);

			_tcpClientMock
				.Setup(c => c.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
				.Returns(() => _clientConnectTask);

			_tcpClientMock
				.Setup(c => c.GetStream())
				.Returns(() => _networkStreamMock.Object);

			var connection = new ModbusTcpConnection
			{
				Hostname = _hostname,
				Port = 502
			};

			// Replace real TCP client with mock
			var clientField = connection.GetType().GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance);
			(clientField.GetValue(connection) as TcpClientWrapper)?.Dispose();
			clientField.SetValue(connection, _tcpClientMock.Object);

			return connection;
		}

		private void ClearInvocations()
		{
			_networkStreamMock.Invocations.Clear();
			_socketMock.Invocations.Clear();
			_tcpClientMock.Invocations.Clear();
		}
	}
}
