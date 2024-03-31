using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.Modbus.Common.Contracts;
using AMWD.Protocols.Modbus.Tcp;
using AMWD.Protocols.Modbus.Tcp.Utils;
using Moq;

namespace AMWD.Protocols.Modbus.Tests.Tcp
{
	[TestClass]
	public class ModbusTcpConnectionTest
	{
		private readonly string _hostname = "127.0.0.1";

		private Mock<TcpClientWrapper> _tcpClientMock;
		private Mock<NetworkStreamWrapper> _networkStreamMock;

		private bool _alwaysConnected;
		private Queue<bool> _connectedQueue;

		private readonly int _clientReceiveTimeout = 1000;
		private readonly int _clientSendTimeout = 1000;
		private readonly Task _clientConnectTask = Task.CompletedTask;

		private List<byte[]> _networkRequestCallbacks;

		private Queue<byte[]> _networkResponseQueue;

		[TestInitialize]
		public void Initialize()
		{
			_alwaysConnected = true;
			_connectedQueue = new Queue<bool>();

			_networkRequestCallbacks = [];
			_networkResponseQueue = new Queue<byte[]>();
		}

		[TestMethod]
		public void ShouldGetAndSetPropertiesOfBaseClient()
		{
			// Arrange
			var connection = GetTcpConnection();

			// Act
			connection.ReadTimeout = TimeSpan.FromSeconds(123);
			connection.WriteTimeout = TimeSpan.FromSeconds(456);

			// Assert - part 1
			Assert.AreEqual("TCP", connection.Name);
			Assert.AreEqual(1, connection.ReadTimeout.TotalSeconds);
			Assert.AreEqual(1, connection.WriteTimeout.TotalSeconds);

			Assert.AreEqual(_hostname, connection.Hostname);
			Assert.AreEqual(502, connection.Port);

			// Assert - part 2
			_tcpClientMock.VerifySet(c => c.ReceiveTimeout = 123000, Times.Once);
			_tcpClientMock.VerifySet(c => c.SendTimeout = 456000, Times.Once);

			_tcpClientMock.VerifyGet(c => c.ReceiveTimeout, Times.Once);
			_tcpClientMock.VerifyGet(c => c.SendTimeout, Times.Once);

			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[DataTestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("   ")]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ShouldThrowArgumentNullExceptionForInvalidHostname(string hostname)
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
		public void ShouldThrowArgumentOutOfRangeExceptionForInvalidPort(int port)
		{
			// Arrange
			var connection = GetTcpConnection();

			// Act
			connection.Port = port;

			// Assert - ArgumentOutOfRangeException
		}

		[TestMethod]
		public void ShouldBeAbleToDisposeMultipleTimes()
		{
			// Arrange
			var connection = GetConnection();

			// Act
			connection.Dispose();
			connection.Dispose();
		}

		[TestMethod]
		[ExpectedException(typeof(ObjectDisposedException))]
		public async Task ShouldThrowDisposedExceptionOnInvokeAsync()
		{
			// Arrange
			var connection = GetConnection();
			connection.Dispose();

			// Act
			await connection.InvokeAsync(null, null);

			// Assert - OjbectDisposedException
		}

		[DataTestMethod]
		[DataRow(null)]
		[DataRow(new byte[0])]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task ShouldThrowArgumentNullExceptionForMissingRequestOnInvokeAsync(byte[] request)
		{
			// Arrange
			var connection = GetConnection();

			// Act
			await connection.InvokeAsync(request, null);

			// Assert - ArgumentNullException
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task ShouldThrowArgumentNullExceptionForMissingValidationOnInvokeAsync()
		{
			// Arrange
			byte[] request = new byte[1];
			var connection = GetConnection();

			// Act
			await connection.InvokeAsync(request, null);

			// Assert - ArgumentNullException
		}

		[TestMethod]
		public async Task ShouldInvokeAsync()
		{
			// Arrange
			byte[] request = [1, 2, 3];
			byte[] expectedResponse = [9, 8, 7];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);
			_networkResponseQueue.Enqueue(expectedResponse);

			var connection = GetConnection();

			// Act
			var response = await connection.InvokeAsync(request, validation);

			// Assert
			Assert.IsNotNull(response);

			CollectionAssert.AreEqual(expectedResponse, response.ToArray());
			CollectionAssert.AreEqual(request, _networkRequestCallbacks.First());

			_tcpClientMock.Verify(c => c.Connected, Times.Once);
			_tcpClientMock.Verify(c => c.GetStream(), Times.Once);

			_networkStreamMock.Verify(ns => ns.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);

			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldConnectAndDisconnectOnInvokeAsync()
		{
			// Arrange
			_alwaysConnected = false;
			_connectedQueue.Enqueue(false);
			_connectedQueue.Enqueue(true);
			_connectedQueue.Enqueue(true);

			byte[] request = [1, 2, 3];
			byte[] expectedResponse = [9, 8, 7];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);
			_networkResponseQueue.Enqueue(expectedResponse);

			var connection = GetConnection();
			connection.IdleTimeout = TimeSpan.FromMilliseconds(200);

			// Act
			var response = await connection.InvokeAsync(request, validation);
			await Task.Delay(500);

			// Assert
			Assert.IsNotNull(response);

			CollectionAssert.AreEqual(expectedResponse, response.ToArray());
			CollectionAssert.AreEqual(request, _networkRequestCallbacks.First());

			_tcpClientMock.VerifyGet(c => c.ReceiveTimeout, Times.Once);

			_tcpClientMock.Verify(c => c.Connected, Times.Exactly(3));
			_tcpClientMock.Verify(c => c.Close(), Times.Exactly(2));
			_tcpClientMock.Verify(c => c.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
			_tcpClientMock.Verify(c => c.GetStream(), Times.Once);

			_networkStreamMock.Verify(ns => ns.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);

			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		[ExpectedException(typeof(EndOfStreamException))]
		public async Task ShouldThrowEndOfStreamExceptionOnInvokeAsync()
		{
			// Arrange
			byte[] request = [1, 2, 3];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);

			var connection = GetConnection();

			// Act
			var response = await connection.InvokeAsync(request, validation);

			// Assert - EndOfStreamException
		}

		[TestMethod]
		[ExpectedException(typeof(ApplicationException))]
		public async Task ShouldThrowApplicationExceptionWhenHostNotResolvableOnInvokeAsync()
		{
			// Arrange
			_alwaysConnected = false;
			_connectedQueue.Enqueue(false);

			byte[] request = [1, 2, 3];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);

			var connection = GetConnection();
			connection.GetType().GetField("_hostname", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(connection, "");

			// Act
			var response = await connection.InvokeAsync(request, validation);

			// Assert - ApplicationException
		}

		[TestMethod]
		public async Task ShouldSkipCloseOnTimeoutOnInvokeAsync()
		{
			// Arrange
			_alwaysConnected = false;
			_connectedQueue.Enqueue(false);
			_connectedQueue.Enqueue(true);
			_connectedQueue.Enqueue(false);

			byte[] request = [1, 2, 3];
			byte[] expectedResponse = [9, 8, 7];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);
			_networkResponseQueue.Enqueue(expectedResponse);

			var connection = GetConnection();
			connection.IdleTimeout = TimeSpan.FromMilliseconds(200);

			// Act
			var response = await connection.InvokeAsync(request, validation);
			await Task.Delay(500);

			// Assert
			Assert.IsNotNull(response);

			CollectionAssert.AreEqual(expectedResponse, response.ToArray());
			CollectionAssert.AreEqual(request, _networkRequestCallbacks.First());

			_tcpClientMock.VerifyGet(c => c.ReceiveTimeout, Times.Once);

			_tcpClientMock.Verify(c => c.Connected, Times.Exactly(3));
			_tcpClientMock.Verify(c => c.Close(), Times.Once);
			_tcpClientMock.Verify(c => c.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
			_tcpClientMock.Verify(c => c.GetStream(), Times.Once);

			_networkStreamMock.Verify(ns => ns.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);

			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldRetryToConnectOnInvokeAsync()
		{
			// Arrange
			_alwaysConnected = false;
			_connectedQueue.Enqueue(false);
			_connectedQueue.Enqueue(false);
			_connectedQueue.Enqueue(true);

			byte[] request = [1, 2, 3];
			byte[] expectedResponse = [9, 8, 7];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);
			_networkResponseQueue.Enqueue(expectedResponse);

			var connection = GetConnection();

			// Act
			var response = await connection.InvokeAsync(request, validation);

			// Assert
			Assert.IsNotNull(response);

			CollectionAssert.AreEqual(expectedResponse, response.ToArray());
			CollectionAssert.AreEqual(request, _networkRequestCallbacks.First());

			_tcpClientMock.VerifyGet(c => c.ReceiveTimeout, Times.Exactly(2));

			_tcpClientMock.Verify(c => c.Connected, Times.Exactly(3));
			_tcpClientMock.Verify(c => c.Close(), Times.Exactly(2));
			_tcpClientMock.Verify(c => c.ConnectAsync(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
			_tcpClientMock.Verify(c => c.GetStream(), Times.Once);

			_networkStreamMock.Verify(ns => ns.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);

			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		[ExpectedException(typeof(TaskCanceledException))]
		public async Task ShouldThrowTaskCancelledExceptionForDisposeOnInvokeAsync()
		{
			// Arrange
			byte[] request = [1, 2, 3];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);

			var connection = GetConnection();
			_networkStreamMock
				.Setup(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
				.Returns(new ValueTask(Task.Delay(100)));

			// Act
			var task = connection.InvokeAsync(request, validation);
			connection.Dispose();
			await task;

			// Assert - TaskCancelledException
		}

		[TestMethod]
		[ExpectedException(typeof(TaskCanceledException))]
		public async Task ShouldThrowTaskCancelledExceptionForCancelOnInvokeAsync()
		{
			// Arrange
			byte[] request = [1, 2, 3];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);
			using var cts = new CancellationTokenSource();

			var connection = GetConnection();
			_networkStreamMock
				.Setup(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
				.Returns(new ValueTask(Task.Delay(100)));

			// Act
			var task = connection.InvokeAsync(request, validation, cts.Token);
			cts.Cancel();
			await task;

			// Assert - TaskCancelledException
		}

		[TestMethod]
		public async Task ShouldRemoveRequestFromQueueOnInvokeAsync()
		{
			// Arrange
			byte[] request = [1, 2, 3];
			byte[] expectedResponse = [9, 8, 7];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);
			_networkResponseQueue.Enqueue(expectedResponse);
			using var cts = new CancellationTokenSource();

			var connection = GetConnection();
			_networkStreamMock
				.Setup(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
				.Callback<ReadOnlyMemory<byte>, CancellationToken>((req, _) => _networkRequestCallbacks.Add(req.ToArray()))
				.Returns(new ValueTask(Task.Delay(100)));

			// Act
			var taskToComplete = connection.InvokeAsync(request, validation);

			var taskToCancel = connection.InvokeAsync(request, validation, cts.Token);
			cts.Cancel();

			var response = await taskToComplete;

			// Assert - Part 1
			try
			{
				await taskToCancel;
				Assert.Fail();
			}
			catch (TaskCanceledException)
			{ /* expected exception */ }

			// Assert - Part 2
			Assert.AreEqual(1, _networkRequestCallbacks.Count);
			CollectionAssert.AreEqual(request, _networkRequestCallbacks.First());
			CollectionAssert.AreEqual(expectedResponse, response.ToArray());

			_tcpClientMock.Verify(c => c.Connected, Times.Once);
			_tcpClientMock.Verify(c => c.GetStream(), Times.Once);

			_networkStreamMock.Verify(ns => ns.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);

			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldRemoveRequestFromQueueOnDispose()
		{
			// Arrange
			byte[] request = [1, 2, 3];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);

			var connection = GetConnection();
			_networkStreamMock
				.Setup(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
				.Callback<ReadOnlyMemory<byte>, CancellationToken>((req, _) => _networkRequestCallbacks.Add(req.ToArray()))
				.Returns(new ValueTask(Task.Delay(100)));

			// Act
			var taskToCancel = connection.InvokeAsync(request, validation);
			var taskToDequeue = connection.InvokeAsync(request, validation);
			connection.Dispose();

			// Assert
			try
			{
				await taskToCancel;
				Assert.Fail();
			}
			catch (TaskCanceledException)
			{ /* expected exception */ }

			try
			{
				await taskToDequeue;
				Assert.Fail();
			}
			catch (ObjectDisposedException)
			{ /* expected exception */ }

			Assert.AreEqual(1, _networkRequestCallbacks.Count);
			CollectionAssert.AreEqual(request, _networkRequestCallbacks.First());

			_tcpClientMock.Verify(c => c.Connected, Times.Once);
			_tcpClientMock.Verify(c => c.GetStream(), Times.Once);
			_tcpClientMock.Verify(c => c.Dispose(), Times.Once);

			_networkStreamMock.Verify(ns => ns.FlushAsync(It.IsAny<CancellationToken>()), Times.Once);
			_networkStreamMock.Verify(ns => ns.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()), Times.Once);

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

			_tcpClientMock = new Mock<TcpClientWrapper>();
			_tcpClientMock.Setup(c => c.Connected).Returns(() => _alwaysConnected || _connectedQueue.Dequeue());
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
			_tcpClientMock.Invocations.Clear();
		}
	}
}
