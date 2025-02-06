using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.Modbus.Tcp;
using AMWD.Protocols.Modbus.Tcp.Utils;
using Moq;

namespace AMWD.Protocols.Modbus.Tests.Tcp
{
	[TestClass]
	public class ModbusTcpProxyTest
	{
		private bool _connectClient;

		private Mock<ModbusClientBase> _clientMock;
		private Mock<TcpListenerWrapper> _tcpListenerMock;
		private Mock<SocketWrapper> _socketMock;
		private Mock<IPEndPointWrapper> _ipEndPointMock;
		private Mock<TcpClientWrapper> _tcpClientMock;
		private Mock<NetworkStreamWrapper> _networkStreamMock;

		private bool _socketBound;

		private Queue<byte[]> _requestBytesQueue;
		private List<byte[]> _responseBytesCallbacks;

		#region Read functions

		private List<(byte UnitId, ushort StartAddress, ushort Count)> _clientReadCallbacks;
		private List<(byte UnitId, ModbusDeviceIdentificationCategory Category, ModbusDeviceIdentificationObject ObjectId)> _clientReadDeviceCallbacks;
		private List<Coil> _clientReadCoilsResponse;
		private List<DiscreteInput> _clientReadDiscreteInputsResponse;
		private List<HoldingRegister> _clientReadHoldingRegistersResponse;
		private List<InputRegister> _clientReadInputRegistersResponse;
		private DeviceIdentification _clientDeviceIdentificationResponse;

		#endregion Read functions

		#region Write functions

		private List<(byte UnitId, Coil Coil)> _writeSingleCoilCallbacks;
		private List<(byte UnitId, List<Coil> Coils)> _writeMultipleCoilsCallbacks;
		private List<(byte UnitId, HoldingRegister HoldingRegister)> _writeSingleRegisterCallbacks;
		private List<(byte UnitId, List<HoldingRegister> HoldingRegisters)> _writeMultipleRegistersCallbacks;

		private bool _clientWriteResponse;

		#endregion Write functions

		[TestInitialize]
		public void Initialize()
		{
			_connectClient = true;

			_socketBound = false;

			_requestBytesQueue = new Queue<byte[]>();
			_responseBytesCallbacks = [];

			#region Read functions

			_clientReadCallbacks = [];
			_clientReadDeviceCallbacks = [];
			_clientReadCoilsResponse = [];
			_clientReadDiscreteInputsResponse = [];
			_clientReadHoldingRegistersResponse = [];
			_clientReadInputRegistersResponse = [];
			_clientDeviceIdentificationResponse = new DeviceIdentification
			{
				VendorName = nameof(DeviceIdentification.VendorName),
				ProductCode = nameof(DeviceIdentification.ProductCode),
				MajorMinorRevision = nameof(DeviceIdentification.MajorMinorRevision),
			};
			_clientDeviceIdentificationResponse.ExtendedObjects.Add(131, [11, 22, 33]);

			#endregion Read functions

			#region Write functions

			_writeSingleCoilCallbacks = [];
			_writeMultipleCoilsCallbacks = [];
			_writeSingleRegisterCallbacks = [];
			_writeMultipleRegistersCallbacks = [];

			_clientWriteResponse = true;

			#endregion Write functions
		}

		#region General

		[TestMethod]
		public void ShouldCreateInstance()
		{
			// Arrange
			_connectClient = false;

			// Act
			using (var proxy = GetProxy())
			{
				// Assert
				Assert.IsNotNull(proxy);
			}

			// Assert
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.Dispose(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldDispose()
		{
			// Arrange
			_connectClient = false;

			// Act
			using (var proxy = GetProxy())
			{
				proxy.Dispose();
			}

			// Assert
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.Dispose(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldStartAndStop()
		{
			// Arrange
			_connectClient = false;
			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await proxy.StopAsync();

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Exactly(2));
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldSetSocketToDualModeOnIpV6()
		{
			// Arrange
			_connectClient = false;
			using var proxy = GetProxy(IPAddress.IPv6Loopback);

			// Act
			await proxy.StartAsync();
			await proxy.StopAsync();

			// Assert
			_tcpListenerMock.VerifyGet(m => m.Socket, Times.Once);
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);
			_socketMock.VerifySet(m => m.DualMode = true, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Exactly(2));
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldThrowArgumentNullExceptionOnCreateInstanceForClient()
		{
			// Arrange

			// Act + Assert
			Assert.ThrowsException<ArgumentNullException>(() => new ModbusTcpProxy(null, IPAddress.Loopback));
		}

		[TestMethod]
		public void ShouldGetAllProperties()
		{
			// Arrange
			_connectClient = false;
			using var proxy = GetProxy();

			// Act
			Assert.AreEqual(IPAddress.Loopback, proxy.ListenAddress);
			Assert.AreEqual(502, proxy.ListenPort);
			Assert.IsFalse(proxy.IsRunning);
			Assert.AreEqual(100, proxy.ReadWriteTimeout.TotalSeconds);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.Socket, Times.Once);
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Exactly(2));
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Port, Times.Once);
			_socketMock.VerifyGet(m => m.IsBound, Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldSetAllProperties()
		{
			// Arrange
			_connectClient = false;
			using var proxy = GetProxy();

			// Act
			proxy.ListenAddress = IPAddress.Any;
			proxy.ListenPort = 55033;
			proxy.ReadWriteTimeout = TimeSpan.FromSeconds(3);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Exactly(2));
			_ipEndPointMock.VerifySet(m => m.Address = IPAddress.Any, Times.Once);
			_ipEndPointMock.VerifySet(m => m.Port = 55033, Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldThrowArgumentOutOfRangeExceptionForInvalidTimeout()
		{
			// Arrange
			_connectClient = false;
			using var proxy = GetProxy();

			// Act + Assert
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => proxy.ReadWriteTimeout = TimeSpan.FromSeconds(-3));
		}

		[TestMethod]
		public async Task ShouldIgnoreExceptionInWaitForClient()
		{
			// Arrange
			using var proxy = GetProxy();
			_tcpListenerMock
				.Setup(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()))
				.Returns<CancellationToken>(async (ct) =>
				{
					await Task.Run(() => SpinWait.SpinUntil(() => _connectClient || ct.IsCancellationRequested));
					_connectClient = false;
					throw new Exception();
				});

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldIgnoreExceptionInHandleClientAsync()
		{
			// Arrange
			using var proxy = GetProxy();
			_tcpClientMock.Setup(m => m.GetStream()).Throws(new Exception());

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);
			_tcpClientMock.Verify(m => m.Dispose(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnIllegalFunction()
		{
			// Arrange
			byte[] request = [1, 14, 0, 5, 0, 4];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([1, 142, 1]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		#endregion General

		#region Read functions

		#region Read Coils (Fn 1)

		[TestMethod]
		public async Task ShouldReadCoils()
		{
			// Arrange
			byte[] request = [2, 1, 0, 5, 0, 4];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			_clientReadCoilsResponse = [
				new Coil { Address = 5, HighByte = 0xFF },
				new Coil { Address = 6, HighByte = 0x00 },
				new Coil { Address = 7, HighByte = 0x00 },
				new Coil { Address = 8, HighByte = 0xFF },
			];
			byte[] expectedResponse = CreateMessage([2, 1, 1, 9]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.ReadCoilsAsync(2, 5, 4, It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, startAddress, count) = _clientReadCallbacks.First();
			Assert.AreEqual(2, unitId);
			Assert.AreEqual(5, startAddress);
			Assert.AreEqual(4, count);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnReadCoils()
		{
			// Arrange
			byte[] request = [2, 1, 0, 5, 4];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnReadCoils()
		{
			// Arrange
			byte[] request = [2, 1, 0, 5, 0, 4];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([2, 129, 4]);

			using var proxy = GetProxy();

			_clientMock
				.Setup(m => m.ReadCoilsAsync(It.IsAny<byte>(), It.IsAny<ushort>(), It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
				.Callback<byte, ushort, ushort, CancellationToken>((unitId, address, count, _) => _clientReadCallbacks.Add((unitId, address, count)))
				.ThrowsAsync(new Exception("Error ;-)"));

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.ReadCoilsAsync(2, 5, 4, It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, startAddress, count) = _clientReadCallbacks.First();
			Assert.AreEqual(2, unitId);
			Assert.AreEqual(5, startAddress);
			Assert.AreEqual(4, count);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		#endregion Read Coils (Fn 1)

		#region Read Discrete Inputs (Fn 2)

		[TestMethod]
		public async Task ShouldReadDiscreteInputs()
		{
			// Arrange
			byte[] request = [2, 2, 0, 5, 0, 4];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			_clientReadDiscreteInputsResponse = [
				new DiscreteInput { Address = 5, HighByte = 0x00 },
				new DiscreteInput { Address = 6, HighByte = 0xFF },
				new DiscreteInput { Address = 7, HighByte = 0x00 },
				new DiscreteInput { Address = 8, HighByte = 0xFF },
			];
			byte[] expectedResponse = CreateMessage([2, 2, 1, 10]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.ReadDiscreteInputsAsync(2, 5, 4, It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, startAddress, count) = _clientReadCallbacks.First();
			Assert.AreEqual(2, unitId);
			Assert.AreEqual(5, startAddress);
			Assert.AreEqual(4, count);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnReadDiscreteInputs()
		{
			// Arrange
			byte[] request = [2, 2, 0, 5, 4];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnReadDiscreteInputs()
		{
			// Arrange
			byte[] request = [2, 2, 0, 5, 0, 4];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([2, 130, 4]);

			using var proxy = GetProxy();

			_clientMock
				.Setup(m => m.ReadDiscreteInputsAsync(It.IsAny<byte>(), It.IsAny<ushort>(), It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
				.Callback<byte, ushort, ushort, CancellationToken>((unitId, address, count, _) => _clientReadCallbacks.Add((unitId, address, count)))
				.ThrowsAsync(new Exception("Error ;-)"));

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.ReadDiscreteInputsAsync(2, 5, 4, It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, startAddress, count) = _clientReadCallbacks.First();
			Assert.AreEqual(2, unitId);
			Assert.AreEqual(5, startAddress);
			Assert.AreEqual(4, count);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		#endregion Read Discrete Inputs (Fn 2)

		#region Read Holding Registers (Fn 3)

		[TestMethod]
		public async Task ShouldReadHoldingRegisters()
		{
			// Arrange
			byte[] request = [42, 3, 0, 15, 0, 2];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			_clientReadHoldingRegistersResponse = [
				new HoldingRegister { Address = 15, LowByte = 12, HighByte = 34 },
				new HoldingRegister { Address = 16, LowByte = 56, HighByte = 78 },
			];
			byte[] expectedResponse = CreateMessage([42, 3, 4, 34, 12, 78, 56]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.ReadHoldingRegistersAsync(42, 15, 2, It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, startAddress, count) = _clientReadCallbacks.First();
			Assert.AreEqual(42, unitId);
			Assert.AreEqual(15, startAddress);
			Assert.AreEqual(2, count);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnReadHoldingRegisters()
		{
			// Arrange
			byte[] request = [42, 3, 0, 15, 2];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnReadHoldingRegisters()
		{
			// Arrange
			byte[] request = [42, 3, 0, 15, 0, 2];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([42, 131, 4]);

			using var proxy = GetProxy();

			_clientMock
				.Setup(m => m.ReadHoldingRegistersAsync(It.IsAny<byte>(), It.IsAny<ushort>(), It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
				.Callback<byte, ushort, ushort, CancellationToken>((unitId, address, count, _) => _clientReadCallbacks.Add((unitId, address, count)))
				.ThrowsAsync(new Exception("Error ;-)"));

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.ReadHoldingRegistersAsync(42, 15, 2, It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, startAddress, count) = _clientReadCallbacks.First();
			Assert.AreEqual(42, unitId);
			Assert.AreEqual(15, startAddress);
			Assert.AreEqual(2, count);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		#endregion Read Holding Registers (Fn 3)

		#region Read Input Registers (Fn 4)

		[TestMethod]
		public async Task ShouldReadInputRegisters()
		{
			// Arrange
			byte[] request = [24, 4, 0, 10, 0, 2];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			_clientReadInputRegistersResponse = [
				new InputRegister { Address = 10, LowByte = 34, HighByte = 12 },
				new InputRegister { Address = 11, LowByte = 78, HighByte = 56 },
			];
			byte[] expectedResponse = CreateMessage([24, 4, 4, 12, 34, 56, 78]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.ReadInputRegistersAsync(24, 10, 2, It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, startAddress, count) = _clientReadCallbacks.First();
			Assert.AreEqual(24, unitId);
			Assert.AreEqual(10, startAddress);
			Assert.AreEqual(2, count);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnReadInputRegisters()
		{
			// Arrange
			byte[] request = [24, 4, 0, 10, 2];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnReadInputRegisters()
		{
			// Arrange
			byte[] request = [24, 4, 0, 10, 0, 2];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([24, 132, 4]);

			using var proxy = GetProxy();

			_clientMock
				.Setup(m => m.ReadInputRegistersAsync(It.IsAny<byte>(), It.IsAny<ushort>(), It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
				.Callback<byte, ushort, ushort, CancellationToken>((unitId, address, count, _) => _clientReadCallbacks.Add((unitId, address, count)))
				.ThrowsAsync(new Exception("Error ;-)"));

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.ReadInputRegistersAsync(24, 10, 2, It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, startAddress, count) = _clientReadCallbacks.First();
			Assert.AreEqual(24, unitId);
			Assert.AreEqual(10, startAddress);
			Assert.AreEqual(2, count);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		#endregion Read Input Registers (Fn 4)

		#region Read Encapsulated Interface (Fn 43)

		[TestMethod]
		public async Task ShouldReadDeviceIdentification()
		{
			// Arrange
			byte[] request = [1, 43, 14, 1, 0];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([
				1, 43, 14, 1,
				1, 0, 0, 3,
				0, 10, 86, 101, 110, 100, 111, 114, 78, 97, 109, 101,
				1, 11, 80, 114, 111, 100, 117, 99, 116, 67, 111, 100, 101,
				2, 18, 77, 97, 106, 111, 114, 77, 105, 110, 111, 114, 82, 101, 118, 105, 115, 105, 111, 110]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.ReadDeviceIdentificationAsync(1, ModbusDeviceIdentificationCategory.Basic, ModbusDeviceIdentificationObject.VendorName, It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, category, objectId) = _clientReadDeviceCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(ModbusDeviceIdentificationCategory.Basic, category);
			Assert.AreEqual(ModbusDeviceIdentificationObject.VendorName, objectId);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
			SnapshotAssert.AreEqual(_clientDeviceIdentificationResponse.ToString());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnReadDeviceIdentification()
		{
			// Arrange
			byte[] request = [1, 43, 14, 1];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnIllegalFunctionForWrongTypeOnReadDeviceIdentification()
		{
			// Arrange
			byte[] request = [1, 43, 13, 1, 0];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([1, 171, 1]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnIllegalDataAddressForWrongTypeOnReadDeviceIdentification()
		{
			// Arrange
			byte[] request = [1, 43, 14, 4, 10];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([1, 171, 2]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnIllegalDataValueForWrongTypeOnReadDeviceIdentification()
		{
			// Arrange
			byte[] request = [1, 43, 14, 0, 0];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([1, 171, 3]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert

			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReadDeviceIdentificationWithIndividualAccessAllowed()
		{
			// Arrange
			byte[] request = [1, 43, 14, 1, 0];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			_clientDeviceIdentificationResponse.IsIndividualAccessAllowed = true;
			byte[] expectedResponse = CreateMessage([
				1, 43, 14, 1,
				129, 0, 0, 3,
				0, 10, 86, 101, 110, 100, 111, 114, 78, 97, 109, 101,
				1, 11, 80, 114, 111, 100, 117, 99, 116, 67, 111, 100, 101,
				2, 18, 77, 97, 106, 111, 114, 77, 105, 110, 111, 114, 82, 101, 118, 105, 115, 105, 111, 110]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.ReadDeviceIdentificationAsync(1, ModbusDeviceIdentificationCategory.Basic, ModbusDeviceIdentificationObject.VendorName, It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, category, objectId) = _clientReadDeviceCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(ModbusDeviceIdentificationCategory.Basic, category);
			Assert.AreEqual(ModbusDeviceIdentificationObject.VendorName, objectId);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReadDeviceIdentificationRegular()
		{
			// Arrange
			byte[] request = [1, 43, 14, 2, 0];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([
				1, 43, 14, 2,
				2, 0, 0, 7,
				0, 10, 86, 101, 110, 100, 111, 114, 78, 97, 109, 101,
				1, 11, 80, 114, 111, 100, 117, 99, 116, 67, 111, 100, 101,
				2, 18, 77, 97, 106, 111, 114, 77, 105, 110, 111, 114, 82, 101, 118, 105, 115, 105, 111, 110,
				3, 0, 4, 0, 5, 0, 6, 0]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.ReadDeviceIdentificationAsync(1, ModbusDeviceIdentificationCategory.Regular, ModbusDeviceIdentificationObject.VendorName, It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, category, objectId) = _clientReadDeviceCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(ModbusDeviceIdentificationCategory.Regular, category);
			Assert.AreEqual(ModbusDeviceIdentificationObject.VendorName, objectId);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReadDeviceIdentificationExtended()
		{
			// Arrange
			byte[] request = [1, 43, 14, 3, 0];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([
				1, 43, 14, 3,
				3, 255, 223, 102,
				0, 10, 86, 101, 110, 100, 111, 114, 78, 97, 109, 101,
				1, 11, 80, 114, 111, 100, 117, 99, 116, 67, 111, 100, 101,
				2, 18, 77, 97, 106, 111, 114, 77, 105, 110, 111, 114, 82, 101, 118, 105, 115, 105, 111, 110,
				3, 0, 4, 0, 5, 0, 6, 0,
				128, 0, 129, 0, 130, 0,
				131, 3, 11, 22, 33,
				132, 0, 133, 0, 134, 0, 135, 0, 136, 0, 137, 0, 138, 0, 139, 0, 140, 0, 141, 0, 142, 0, 143, 0, 144, 0,
				145, 0, 146, 0, 147, 0, 148, 0, 149, 0, 150, 0, 151, 0, 152, 0, 153, 0, 154, 0, 155, 0, 156, 0, 157, 0,
				158, 0, 159, 0, 160, 0, 161, 0, 162, 0, 163, 0, 164, 0, 165, 0, 166, 0, 167, 0, 168, 0, 169, 0, 170, 0,
				171, 0, 172, 0, 173, 0, 174, 0, 175, 0, 176, 0, 177, 0, 178, 0, 179, 0, 180, 0, 181, 0, 182, 0, 183, 0,
				184, 0, 185, 0, 186, 0, 187, 0, 188, 0, 189, 0, 190, 0, 191, 0, 192, 0, 193, 0, 194, 0, 195, 0, 196, 0,
				197, 0, 198, 0, 199, 0, 200, 0, 201, 0, 202, 0, 203, 0, 204, 0, 205, 0, 206, 0, 207, 0, 208, 0, 209, 0,
				210, 0, 211, 0, 212, 0, 213, 0, 214, 0, 215, 0, 216, 0, 217, 0, 218, 0, 219, 0, 220, 0, 221, 0, 222, 0]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.ReadDeviceIdentificationAsync(1, ModbusDeviceIdentificationCategory.Extended, ModbusDeviceIdentificationObject.VendorName, It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, category, objectId) = _clientReadDeviceCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(ModbusDeviceIdentificationCategory.Extended, category);
			Assert.AreEqual(ModbusDeviceIdentificationObject.VendorName, objectId);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReadDeviceIdentificationIndividual()
		{
			// Arrange
			byte[] request = [1, 43, 14, 4, 0];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			_clientDeviceIdentificationResponse.IsIndividualAccessAllowed = true;
			byte[] expectedResponse = CreateMessage([
				1, 43, 14, 4,
				132, 0, 0, 1,
				0, 10, 86, 101, 110, 100, 111, 114, 78, 97, 109, 101]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.ReadDeviceIdentificationAsync(1, ModbusDeviceIdentificationCategory.Individual, ModbusDeviceIdentificationObject.VendorName, It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, category, objectId) = _clientReadDeviceCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(ModbusDeviceIdentificationCategory.Individual, category);
			Assert.AreEqual(ModbusDeviceIdentificationObject.VendorName, objectId);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureForWrongTypeOnReadDeviceIdentification()
		{
			// Arrange
			byte[] request = [1, 43, 14, 1, 0];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([1, 171, 4]);

			using var proxy = GetProxy();

			_clientMock.Setup(m => m.ReadDeviceIdentificationAsync(It.IsAny<byte>(), It.IsAny<ModbusDeviceIdentificationCategory>(), It.IsAny<ModbusDeviceIdentificationObject>(), It.IsAny<CancellationToken>()))
				.Callback<byte, ModbusDeviceIdentificationCategory, ModbusDeviceIdentificationObject, CancellationToken>((unitId, category, objectId, _) => _clientReadDeviceCallbacks.Add((unitId, category, objectId)))
				.ThrowsAsync(new ModbusException());

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.ReadDeviceIdentificationAsync(1, ModbusDeviceIdentificationCategory.Basic, ModbusDeviceIdentificationObject.VendorName, It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, category, objectId) = _clientReadDeviceCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(ModbusDeviceIdentificationCategory.Basic, category);
			Assert.AreEqual(ModbusDeviceIdentificationObject.VendorName, objectId);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		#endregion Read Encapsulated Interface (Fn 43)

		#endregion Read functions

		#region Write functions

		#region Write Single Coil (Fn 5)

		[TestMethod]
		public async Task ShouldWriteSingleCoil()
		{
			// Arrange
			byte[] request = [3, 5, 0, 7, 255, 0];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([3, 5, 0, 7, 255, 0]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.WriteSingleCoilAsync(3, It.IsAny<Coil>(), It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, coil) = _writeSingleCoilCallbacks.First();
			Assert.AreEqual(3, unitId);
			Assert.AreEqual(7, coil.Address);
			Assert.IsTrue(coil.Value);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnWriteSingleCoil()
		{
			// Arrange
			byte[] request = [3, 5, 0, 7, 255];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnIllegalDataValueOnWriteSingleCoil()
		{
			// Arrange
			byte[] request = [3, 5, 0, 7, 250, 0];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([3, 133, 3]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnWriteSingleCoilForNotSuccessful()
		{
			// Arrange
			_clientWriteResponse = false;
			byte[] request = [3, 5, 0, 7, 255, 0];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([3, 133, 4]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.WriteSingleCoilAsync(3, It.IsAny<Coil>(), It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, coil) = _writeSingleCoilCallbacks.First();
			Assert.AreEqual(3, unitId);
			Assert.AreEqual(7, coil.Address);
			Assert.IsTrue(coil.Value);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnWriteSingleCoilForException()
		{
			// Arrange
			byte[] request = [3, 5, 0, 7, 255, 0];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([3, 133, 4]);

			using var proxy = GetProxy();

			_clientMock
				.Setup(m => m.WriteSingleCoilAsync(It.IsAny<byte>(), It.IsAny<Coil>(), It.IsAny<CancellationToken>()))
				.Callback<byte, Coil, CancellationToken>((unitId, coil, _) => _writeSingleCoilCallbacks.Add((unitId, coil)))
				.ThrowsAsync(new ModbusException());

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.WriteSingleCoilAsync(3, It.IsAny<Coil>(), It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, coil) = _writeSingleCoilCallbacks.First();
			Assert.AreEqual(3, unitId);
			Assert.AreEqual(7, coil.Address);
			Assert.IsTrue(coil.Value);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		#endregion Write Single Coil (Fn 5)

		#region Write Single Register (Fn 6)

		[TestMethod]
		public async Task ShouldWriteSingleRegister()
		{
			// Arrange
			byte[] request = [4, 6, 0, 1, 0, 3];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([4, 6, 0, 1, 0, 3]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.WriteSingleHoldingRegisterAsync(4, It.IsAny<HoldingRegister>(), It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, register) = _writeSingleRegisterCallbacks.First();
			Assert.AreEqual(4, unitId);
			Assert.AreEqual(1, register.Address);
			Assert.AreEqual(3, register.Value);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnWriteSingleRegister()
		{
			// Arrange
			byte[] request = [4, 6, 0, 1, 3];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnWriteSingleRegisterForNotSuccessful()
		{
			// Arrange
			_clientWriteResponse = false;
			byte[] request = [4, 6, 0, 1, 0, 3];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([4, 134, 4]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.WriteSingleHoldingRegisterAsync(4, It.IsAny<HoldingRegister>(), It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, register) = _writeSingleRegisterCallbacks.First();
			Assert.AreEqual(4, unitId);
			Assert.AreEqual(1, register.Address);
			Assert.AreEqual(3, register.Value);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnWriteSingleRegisterForException()
		{
			// Arrange
			byte[] request = [4, 6, 0, 1, 0, 3];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([4, 134, 4]);

			using var proxy = GetProxy();

			_clientMock
				.Setup(m => m.WriteSingleHoldingRegisterAsync(It.IsAny<byte>(), It.IsAny<HoldingRegister>(), It.IsAny<CancellationToken>()))
				.Callback<byte, HoldingRegister, CancellationToken>((unitId, register, _) => _writeSingleRegisterCallbacks.Add((unitId, register)))
				.ThrowsAsync(new ModbusException());

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.WriteSingleHoldingRegisterAsync(4, It.IsAny<HoldingRegister>(), It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, register) = _writeSingleRegisterCallbacks.First();
			Assert.AreEqual(4, unitId);
			Assert.AreEqual(1, register.Address);
			Assert.AreEqual(3, register.Value);

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		#endregion Write Single Register (Fn 6)

		#region Write Multiple Coils (Fn 15)

		[TestMethod]
		public async Task ShouldWriteMultipleCoils()
		{
			// Arrange
			byte[] request = [1, 15, 0, 13, 0, 10, 2, 205, 1];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([1, 15, 0, 13, 0, 10]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.WriteMultipleCoilsAsync(1, It.IsAny<IReadOnlyList<Coil>>(), It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());

			var (unitId, coils) = _writeMultipleCoilsCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(10, coils.Count);

			for (byte i = 13; i < 23; i++)
				Assert.IsNotNull(coils.Where(c => c.Address == i).FirstOrDefault());

			CollectionAssert.AreEqual(new bool[] { true, false, true, true, false, false, true, true, true, false }, coils.Select(c => c.Value).ToArray());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnWriteMultipleCoils()
		{
			// Arrange
			byte[] request = [1, 15, 0, 13, 0, 10];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnIllegalDataValueOnWriteMultipleCoils()
		{
			// Arrange
			byte[] request = [1, 15, 0, 13, 0, 10, 2, 205];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([1, 143, 3]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnWriteMultipleCoilsForNotSuccessful()
		{
			// Arrange
			_clientWriteResponse = false;
			byte[] request = [1, 15, 0, 13, 0, 10, 2, 205, 1];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([1, 143, 4]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.WriteMultipleCoilsAsync(1, It.IsAny<IReadOnlyList<Coil>>(), It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());

			var (unitId, coils) = _writeMultipleCoilsCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(10, coils.Count);

			for (byte i = 13; i < 23; i++)
				Assert.IsNotNull(coils.Where(c => c.Address == i).FirstOrDefault());

			CollectionAssert.AreEqual(new bool[] { true, false, true, true, false, false, true, true, true, false }, coils.Select(c => c.Value).ToArray());
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnWriteMultipleCoilsForException()
		{
			// Arrange
			byte[] request = [1, 15, 0, 13, 0, 10, 2, 205, 1];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([1, 143, 4]);

			using var proxy = GetProxy();

			_clientMock
				.Setup(m => m.WriteMultipleCoilsAsync(It.IsAny<byte>(), It.IsAny<IReadOnlyList<Coil>>(), It.IsAny<CancellationToken>()))
				.Callback<byte, IReadOnlyList<Coil>, CancellationToken>((unitId, coils, _) => _writeMultipleCoilsCallbacks.Add((unitId, coils.ToList())))
				.ThrowsAsync(new ModbusException());

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.WriteMultipleCoilsAsync(1, It.IsAny<IReadOnlyList<Coil>>(), It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());

			var (unitId, coils) = _writeMultipleCoilsCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(10, coils.Count);

			for (byte i = 13; i < 23; i++)
				Assert.IsNotNull(coils.Where(c => c.Address == i).FirstOrDefault());

			CollectionAssert.AreEqual(new bool[] { true, false, true, true, false, false, true, true, true, false }, coils.Select(c => c.Value).ToArray());
		}

		#endregion Write Multiple Coils (Fn 15)

		#region Write Multiple Coils (Fn 16)

		[TestMethod]
		public async Task ShouldWriteMultipleRegisters()
		{
			// Arrange
			byte[] request = [1, 16, 0, 1, 0, 2, 4, 0, 10, 1, 2];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([1, 16, 0, 1, 0, 2]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.WriteMultipleHoldingRegistersAsync(1, It.IsAny<IReadOnlyList<HoldingRegister>>(), It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());

			var (unitId, registers) = _writeMultipleRegistersCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(2, registers.Count);

			for (byte i = 1; i < 3; i++)
				Assert.IsNotNull(registers.Where(c => c.Address == i).FirstOrDefault());

			CollectionAssert.AreEqual(new ushort[] { 10, 258 }, registers.Select(c => c.Value).ToArray());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnWriteMultipleRegisters()
		{
			// Arrange
			byte[] request = [1, 16, 0, 1, 0, 2];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnIllegalDataValueOnWriteMultipleRegisters()
		{
			// Arrange
			byte[] request = [1, 16, 0, 1, 0, 2, 4, 0, 10, 1];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([1, 144, 3]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnWriteMultipleRegistersForNotSuccessful()
		{
			// Arrange
			_clientWriteResponse = false;
			byte[] request = [1, 16, 0, 1, 0, 2, 4, 0, 10, 1, 2];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([1, 144, 4]);

			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.WriteMultipleHoldingRegistersAsync(1, It.IsAny<IReadOnlyList<HoldingRegister>>(), It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());

			var (unitId, registers) = _writeMultipleRegistersCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(2, registers.Count);

			for (byte i = 1; i < 3; i++)
				Assert.IsNotNull(registers.Where(c => c.Address == i).FirstOrDefault());

			CollectionAssert.AreEqual(new ushort[] { 10, 258 }, registers.Select(c => c.Value).ToArray());
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnWriteMultipleRegistersForException()
		{
			// Arrange
			byte[] request = [1, 16, 0, 1, 0, 2, 4, 0, 10, 1, 2];
			_requestBytesQueue.Enqueue(CreateHeader(request));
			_requestBytesQueue.Enqueue(request);
			byte[] expectedResponse = CreateMessage([1, 144, 4]);

			using var proxy = GetProxy();

			_clientMock
				.Setup(m => m.WriteMultipleHoldingRegistersAsync(It.IsAny<byte>(), It.IsAny<IReadOnlyList<HoldingRegister>>(), It.IsAny<CancellationToken>()))
				.Callback<byte, IReadOnlyList<HoldingRegister>, CancellationToken>((unitId, coils, _) => _writeMultipleRegistersCallbacks.Add((unitId, coils.ToList())))
				.ThrowsAsync(new ModbusException());

			// Act
			await proxy.StartAsync();
			await Task.Delay(100);

			// Assert
			_tcpListenerMock.VerifyGet(m => m.LocalIPEndPoint, Times.Once);
			_ipEndPointMock.VerifyGet(m => m.Address, Times.Once);

			_tcpListenerMock.Verify(m => m.Start(), Times.Once);
			_tcpListenerMock.Verify(m => m.Stop(), Times.Once);
			_tcpListenerMock.Verify(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));

			_tcpClientMock.Verify(m => m.GetStream(), Times.Once);

			_clientMock.Verify(m => m.WriteMultipleHoldingRegistersAsync(1, It.IsAny<IReadOnlyList<HoldingRegister>>(), It.IsAny<CancellationToken>()), Times.Once);

			_networkStreamMock.Verify(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
			_networkStreamMock.Verify(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse, _responseBytesCallbacks.First());

			var (unitId, registers) = _writeMultipleRegistersCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(2, registers.Count);

			for (byte i = 1; i < 3; i++)
				Assert.IsNotNull(registers.Where(c => c.Address == i).FirstOrDefault());

			CollectionAssert.AreEqual(new ushort[] { 10, 258 }, registers.Select(c => c.Value).ToArray());
		}

		#endregion Write Multiple Coils (Fn 16)

		#endregion Write functions

		private void VerifyNoOtherCalls()
		{
			_clientMock.VerifyNoOtherCalls();
			_tcpListenerMock.VerifyNoOtherCalls();
			_ipEndPointMock.VerifyNoOtherCalls();
			_socketMock.VerifyNoOtherCalls();
			_tcpClientMock.VerifyNoOtherCalls();
			_networkStreamMock.VerifyNoOtherCalls();
		}

		private static byte[] CreateHeader(IReadOnlyList<byte> request)
		{
			ushort length = (ushort)request.Count;
			byte[] bytes = BitConverter.GetBytes(length);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);

			return [0, 1, 0, 0, .. bytes];
		}

		private static byte[] CreateMessage(IReadOnlyList<byte> request)
		{
			return [.. CreateHeader(request), .. request];
		}

		private ModbusTcpProxy GetProxy(IPAddress localAddress = null)
		{
			localAddress ??= IPAddress.Loopback;
			int localPort = 502;

			var connection = new Mock<IModbusConnection>();

			_clientMock = new Mock<ModbusClientBase>(connection.Object);
			_tcpListenerMock = new Mock<TcpListenerWrapper>(localAddress, localPort);
			_ipEndPointMock = new Mock<IPEndPointWrapper>(null);
			_socketMock = new Mock<SocketWrapper>(null);
			_tcpClientMock = new Mock<TcpClientWrapper>(AddressFamily.InterNetwork);
			_networkStreamMock = new Mock<NetworkStreamWrapper>(null);

			#region General

			_tcpListenerMock
				.Setup(m => m.Socket)
				.Returns(() => _socketMock.Object);
			_tcpListenerMock
				.Setup(m => m.LocalIPEndPoint)
				.Returns(() => _ipEndPointMock.Object);
			_tcpListenerMock
				.Setup(m => m.AcceptTcpClientAsync(It.IsAny<CancellationToken>()))
				.Returns<CancellationToken>(async (ct) =>
				{
					await Task.Run(() => SpinWait.SpinUntil(() => _connectClient || ct.IsCancellationRequested));
					ct.ThrowIfCancellationRequested();
					_connectClient = false;

					return _tcpClientMock.Object;
				});

			_ipEndPointMock.SetupProperty(m => m.Address, localAddress);
			_ipEndPointMock.SetupProperty(m => m.Port, localPort);

			_socketMock.SetupProperty(m => m.DualMode, false);
			_socketMock.SetupGet(m => m.IsBound).Returns(() => _socketBound);

			_tcpClientMock
				.Setup(m => m.GetStream())
				.Returns(() => _networkStreamMock.Object);

			_networkStreamMock
				.Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
				.Returns<byte[], int, int, CancellationToken>(async (buffer, offset, count, ct) =>
				{
					await Task.Run(() => SpinWait.SpinUntil(() => _requestBytesQueue.Count > 0 || ct.IsCancellationRequested));
					ct.ThrowIfCancellationRequested();

					byte[] bytes = _requestBytesQueue.Dequeue();
					int minLength = Math.Min(bytes.Length, count);

					Array.Copy(bytes, 0, buffer, offset, minLength);
					return minLength;
				});
			_networkStreamMock
				.Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
				.Callback<byte[], int, int, CancellationToken>((buffer, _, __, ___) => _responseBytesCallbacks.Add(buffer))
				.Returns(Task.CompletedTask);

			#endregion General

			#region Read functions

			_clientMock
				.Setup(m => m.ReadCoilsAsync(It.IsAny<byte>(), It.IsAny<ushort>(), It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
				.Callback<byte, ushort, ushort, CancellationToken>((unitId, address, count, _) => _clientReadCallbacks.Add((unitId, address, count)))
				.ReturnsAsync(() => _clientReadCoilsResponse);
			_clientMock
				.Setup(m => m.ReadDiscreteInputsAsync(It.IsAny<byte>(), It.IsAny<ushort>(), It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
				.Callback<byte, ushort, ushort, CancellationToken>((unitId, address, count, _) => _clientReadCallbacks.Add((unitId, address, count)))
				.ReturnsAsync(() => _clientReadDiscreteInputsResponse);
			_clientMock
				.Setup(m => m.ReadHoldingRegistersAsync(It.IsAny<byte>(), It.IsAny<ushort>(), It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
				.Callback<byte, ushort, ushort, CancellationToken>((unitId, address, count, _) => _clientReadCallbacks.Add((unitId, address, count)))
				.ReturnsAsync(() => _clientReadHoldingRegistersResponse);
			_clientMock
				.Setup(m => m.ReadInputRegistersAsync(It.IsAny<byte>(), It.IsAny<ushort>(), It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
				.Callback<byte, ushort, ushort, CancellationToken>((unitId, address, count, _) => _clientReadCallbacks.Add((unitId, address, count)))
				.ReturnsAsync(() => _clientReadInputRegistersResponse);
			_clientMock
				.Setup(m => m.ReadDeviceIdentificationAsync(It.IsAny<byte>(), It.IsAny<ModbusDeviceIdentificationCategory>(), It.IsAny<ModbusDeviceIdentificationObject>(), It.IsAny<CancellationToken>()))
				.Callback<byte, ModbusDeviceIdentificationCategory, ModbusDeviceIdentificationObject, CancellationToken>((unitId, category, objectId, _) => _clientReadDeviceCallbacks.Add((unitId, category, objectId)))
				.ReturnsAsync(() => _clientDeviceIdentificationResponse);

			#endregion Read functions

			#region Write functions

			_clientMock
				.Setup(m => m.WriteSingleCoilAsync(It.IsAny<byte>(), It.IsAny<Coil>(), It.IsAny<CancellationToken>()))
				.Callback<byte, Coil, CancellationToken>((unitId, coil, _) => _writeSingleCoilCallbacks.Add((unitId, coil)))
				.ReturnsAsync(() => _clientWriteResponse);
			_clientMock
				.Setup(m => m.WriteSingleHoldingRegisterAsync(It.IsAny<byte>(), It.IsAny<HoldingRegister>(), It.IsAny<CancellationToken>()))
				.Callback<byte, HoldingRegister, CancellationToken>((unitId, register, _) => _writeSingleRegisterCallbacks.Add((unitId, register)))
				.ReturnsAsync(() => _clientWriteResponse);
			_clientMock
				.Setup(m => m.WriteMultipleCoilsAsync(It.IsAny<byte>(), It.IsAny<IReadOnlyList<Coil>>(), It.IsAny<CancellationToken>()))
				.Callback<byte, IReadOnlyList<Coil>, CancellationToken>((unitId, coils, _) => _writeMultipleCoilsCallbacks.Add((unitId, coils.ToList())))
				.ReturnsAsync(() => _clientWriteResponse);
			_clientMock
				.Setup(m => m.WriteMultipleHoldingRegistersAsync(It.IsAny<byte>(), It.IsAny<IReadOnlyList<HoldingRegister>>(), It.IsAny<CancellationToken>()))
				.Callback<byte, IReadOnlyList<HoldingRegister>, CancellationToken>((unitId, registers, _) => _writeMultipleRegistersCallbacks.Add((unitId, registers.ToList())))
				.ReturnsAsync(() => _clientWriteResponse);

			#endregion Write functions

			var proxy = new ModbusTcpProxy(_clientMock.Object, localAddress);
			var tcpListenerField = proxy.GetType().GetField("_tcpListener", BindingFlags.NonPublic | BindingFlags.Instance);

			((IDisposable)tcpListenerField.GetValue(proxy)).Dispose();
			tcpListenerField.SetValue(proxy, _tcpListenerMock.Object);

			return proxy;
		}
	}
}
