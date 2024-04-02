using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.Modbus.Serial;
using AMWD.Protocols.Modbus.Serial.Enums;
using AMWD.Protocols.Modbus.Serial.Utils;
using Moq;

namespace AMWD.Protocols.Modbus.Tests.Serial
{
	[TestClass]
	public class ModbusSerialConnectionTest
	{
		private Mock<SerialPortWrapper> _serialPortMock;

		private bool _alwaysOpen;
		private Queue<bool> _isOpenQueue;

		private readonly int _serialPortReadTimeout = 1000;
		private readonly int _serialPortWriteTimeout = 1000;

		private List<byte[]> _serialLineRequestCallbacks;

		private Queue<byte[]> _serialLineResponseQueue;

		[TestInitialize]
		public void Initialize()
		{
			_alwaysOpen = true;
			_isOpenQueue = new Queue<bool>();

			_serialLineRequestCallbacks = [];
			_serialLineResponseQueue = new Queue<byte[]>();
		}

		[TestMethod]
		public void ShouldGetAndSetPropertiesOfBaseClient()
		{
			// Arrange
			var connection = GetSerialConnection();

			// Act
			connection.PortName = "SerialPort";
			connection.BaudRate = BaudRate.Baud2400;
			connection.DataBits = 5;
			connection.Handshake = Handshake.XOnXOff;
			connection.Parity = Parity.None;
			connection.ReadTimeout = TimeSpan.FromSeconds(123);
			connection.RtsEnable = true;
			connection.StopBits = StopBits.OnePointFive;
			connection.WriteTimeout = TimeSpan.FromSeconds(456);

			// Assert - part 1
			_serialPortMock.VerifySet(p => p.PortName = "SerialPort", Times.Once);
			_serialPortMock.VerifySet(p => p.BaudRate = 2400, Times.Once);
			_serialPortMock.VerifySet(p => p.DataBits = 5, Times.Once);
			_serialPortMock.VerifySet(p => p.Handshake = Handshake.XOnXOff, Times.Once);
			_serialPortMock.VerifySet(p => p.Parity = Parity.None, Times.Once);
			_serialPortMock.VerifySet(p => p.ReadTimeout = 123000, Times.Once);
			_serialPortMock.VerifySet(p => p.RtsEnable = true, Times.Once);
			_serialPortMock.VerifySet(p => p.StopBits = StopBits.OnePointFive, Times.Once);
			_serialPortMock.VerifySet(p => p.WriteTimeout = 456000, Times.Once);

			_serialPortMock.VerifyNoOtherCalls();

			// Assert - part 2
			Assert.AreEqual("Serial", connection.Name);
			Assert.IsNull(connection.PortName);
			Assert.AreEqual(0, (int)connection.BaudRate);
			Assert.AreEqual(0, connection.DataBits);
			Assert.AreEqual(0, (int)connection.Handshake);
			Assert.AreEqual(0, (int)connection.Parity);
			Assert.AreEqual(1, connection.ReadTimeout.TotalSeconds);
			Assert.IsFalse(connection.RtsEnable);
			Assert.AreEqual(0, (int)connection.StopBits);
			Assert.AreEqual(1, connection.WriteTimeout.TotalSeconds);
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
			_serialLineResponseQueue.Enqueue(expectedResponse);

			var connection = GetConnection();

			// Act
			var response = await connection.InvokeAsync(request, validation);

			// Assert
			Assert.IsNotNull(response);

			CollectionAssert.AreEqual(expectedResponse, response.ToArray());
			CollectionAssert.AreEqual(request, _serialLineRequestCallbacks.First());

			_serialPortMock.Verify(c => c.IsOpen, Times.Once);

			_serialPortMock.Verify(ns => ns.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
			_serialPortMock.Verify(ns => ns.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			_serialPortMock.VerifyNoOtherCalls();
		}

		[DataTestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public async Task ShouldOpenAndCloseOnInvokeAsync(bool modifyDriver)
		{
			// Arrange
			_alwaysOpen = false;
			_isOpenQueue.Enqueue(false);
			_isOpenQueue.Enqueue(true);
			_isOpenQueue.Enqueue(true);

			byte[] request = [1, 2, 3];
			byte[] expectedResponse = [9, 8, 7];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);
			_serialLineResponseQueue.Enqueue(expectedResponse);

			var connection = GetSerialConnection();
			connection.IdleTimeout = TimeSpan.FromMilliseconds(200);
			connection.DriverEnabledRS485 = modifyDriver;

			// Act
			var response = await connection.InvokeAsync(request, validation);
			await Task.Delay(500);

			// Assert
			Assert.IsNotNull(response);

			CollectionAssert.AreEqual(expectedResponse, response.ToArray());
			CollectionAssert.AreEqual(request, _serialLineRequestCallbacks.First());

			_serialPortMock.VerifyGet(c => c.ReadTimeout, Times.Once);

			_serialPortMock.Verify(c => c.IsOpen, Times.Exactly(3));
			_serialPortMock.Verify(c => c.Close(), Times.Exactly(2));
			_serialPortMock.Verify(c => c.ResetRS485DriverStateFlags(), Times.Exactly(2));
			_serialPortMock.Verify(c => c.Open(), Times.Once);

			if (modifyDriver)
			{
				_serialPortMock.Verify(c => c.GetRS485DriverStateFlags(), Times.Once);
				_serialPortMock.Verify(c => c.ChangeRS485DriverStateFlags(It.IsAny<RS485Flags>()), Times.Once);
			}

			_serialPortMock.Verify(ns => ns.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
			_serialPortMock.Verify(ns => ns.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			_serialPortMock.VerifyNoOtherCalls();
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
		public async Task ShouldSkipCloseOnTimeoutOnInvokeAsync()
		{
			// Arrange
			_alwaysOpen = false;
			_isOpenQueue.Enqueue(false);
			_isOpenQueue.Enqueue(true);
			_isOpenQueue.Enqueue(false);

			byte[] request = [1, 2, 3];
			byte[] expectedResponse = [9, 8, 7];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);
			_serialLineResponseQueue.Enqueue(expectedResponse);

			var connection = GetConnection();
			connection.IdleTimeout = TimeSpan.FromMilliseconds(200);

			// Act
			var response = await connection.InvokeAsync(request, validation);
			await Task.Delay(500);

			// Assert
			Assert.IsNotNull(response);

			CollectionAssert.AreEqual(expectedResponse, response.ToArray());
			CollectionAssert.AreEqual(request, _serialLineRequestCallbacks.First());

			_serialPortMock.VerifyGet(c => c.ReadTimeout, Times.Once);

			_serialPortMock.Verify(c => c.IsOpen, Times.Exactly(3));
			_serialPortMock.Verify(c => c.Close(), Times.Once);
			_serialPortMock.Verify(c => c.ResetRS485DriverStateFlags(), Times.Once);
			_serialPortMock.Verify(c => c.Open(), Times.Once);

			_serialPortMock.Verify(ns => ns.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
			_serialPortMock.Verify(ns => ns.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			_serialPortMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldRetryToConnectOnInvokeAsync()
		{
			// Arrange
			_alwaysOpen = false;
			_isOpenQueue.Enqueue(false);
			_isOpenQueue.Enqueue(false);
			_isOpenQueue.Enqueue(true);

			byte[] request = [1, 2, 3];
			byte[] expectedResponse = [9, 8, 7];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);
			_serialLineResponseQueue.Enqueue(expectedResponse);

			var connection = GetConnection();

			// Act
			var response = await connection.InvokeAsync(request, validation);

			// Assert
			Assert.IsNotNull(response);

			CollectionAssert.AreEqual(expectedResponse, response.ToArray());
			CollectionAssert.AreEqual(request, _serialLineRequestCallbacks.First());

			_serialPortMock.VerifyGet(c => c.ReadTimeout, Times.Exactly(2));

			_serialPortMock.Verify(c => c.IsOpen, Times.Exactly(3));
			_serialPortMock.Verify(c => c.Close(), Times.Exactly(2));
			_serialPortMock.Verify(c => c.ResetRS485DriverStateFlags(), Times.Exactly(2));
			_serialPortMock.Verify(c => c.Open(), Times.Exactly(2));

			_serialPortMock.Verify(ns => ns.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
			_serialPortMock.Verify(ns => ns.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			_serialPortMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		[ExpectedException(typeof(TaskCanceledException))]
		public async Task ShouldThrowTaskCancelledExceptionForDisposeOnInvokeAsync()
		{
			// Arrange
			byte[] request = [1, 2, 3];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);

			var connection = GetConnection();
			_serialPortMock
				.Setup(ns => ns.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
				.Returns(Task.Delay(100));

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
			_serialPortMock
				.Setup(ns => ns.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
				.Returns(Task.Delay(100));

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
			_serialLineResponseQueue.Enqueue(expectedResponse);
			using var cts = new CancellationTokenSource();

			var connection = GetConnection();
			_serialPortMock
				.Setup(ns => ns.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
				.Callback<byte[], CancellationToken>((req, _) => _serialLineRequestCallbacks.Add(req.ToArray()))
				.Returns(Task.Delay(100));

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
			Assert.AreEqual(1, _serialLineRequestCallbacks.Count);
			CollectionAssert.AreEqual(request, _serialLineRequestCallbacks.First());
			CollectionAssert.AreEqual(expectedResponse, response.ToArray());

			_serialPortMock.Verify(c => c.IsOpen, Times.Once);

			_serialPortMock.Verify(ns => ns.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
			_serialPortMock.Verify(ns => ns.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			_serialPortMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldRemoveRequestFromQueueOnDispose()
		{
			// Arrange
			byte[] request = [1, 2, 3];
			var validation = new Func<IReadOnlyList<byte>, bool>(_ => true);

			var connection = GetConnection();
			_serialPortMock
				.Setup(ns => ns.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
				.Callback<byte[], CancellationToken>((req, _) => _serialLineRequestCallbacks.Add(req.ToArray()))
				.Returns(Task.Delay(100));

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

			Assert.AreEqual(1, _serialLineRequestCallbacks.Count);
			CollectionAssert.AreEqual(request, _serialLineRequestCallbacks.First());

			_serialPortMock.Verify(c => c.IsOpen, Times.Once);
			_serialPortMock.Verify(c => c.Dispose(), Times.Once);

			_serialPortMock.Verify(ns => ns.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);

			_serialPortMock.VerifyNoOtherCalls();
		}

		private IModbusConnection GetConnection()
			=> GetSerialConnection();

		private ModbusSerialConnection GetSerialConnection()
		{
			_serialPortMock = new Mock<SerialPortWrapper>();
			_serialPortMock.Setup(p => p.IsOpen).Returns(() => _alwaysOpen || _isOpenQueue.Dequeue());
			_serialPortMock.Setup(p => p.ReadTimeout).Returns(() => _serialPortReadTimeout);
			_serialPortMock.Setup(p => p.WriteTimeout).Returns(() => _serialPortWriteTimeout);

			_serialPortMock
				.Setup(p => p.WriteAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
				.Callback<byte[], CancellationToken>((req, _) => _serialLineRequestCallbacks.Add(req))
				.Returns(Task.CompletedTask);
			_serialPortMock
				.Setup(p => p.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
				.Returns<byte[], int, int, CancellationToken>((buffer, offset, count, _) =>
				{
					if (_serialLineResponseQueue.TryDequeue(out byte[] bytes))
					{
						int len = bytes.Length < count ? bytes.Length : count;
						Array.Copy(bytes, 0, buffer, offset, len);
						return Task.FromResult(len);
					}

					return Task.FromResult(0);
				});

			var connection = new ModbusSerialConnection();

			// Replace real connection with mock
			var connectionField = connection.GetType().GetField("_serialPort", BindingFlags.NonPublic | BindingFlags.Instance);
			(connectionField.GetValue(connection) as SerialPortWrapper)?.Dispose();
			connectionField.SetValue(connection, _serialPortMock.Object);

			// Set unit test mode
			connection.GetType().GetField("_isUnitTest", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(connection, true);

			return connection;
		}
	}
}
