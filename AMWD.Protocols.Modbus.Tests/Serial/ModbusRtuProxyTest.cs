using System.Collections.Generic;
using System.IO.Ports;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.Modbus.Common.Protocols;
using AMWD.Protocols.Modbus.Serial;
using AMWD.Protocols.Modbus.Serial.Utils;
using Moq;

namespace AMWD.Protocols.Modbus.Tests.Serial
{
	[TestClass]
	public class ModbusRtuProxyTest
	{
		private Mock<ModbusClientBase> _clientMock;
		private Mock<SerialPortWrapper> _serialPortMock;

		private SerialDataReceivedEventArgs _dataReceivedEventArgs;
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
			_dataReceivedEventArgs = Helper.CreateInstance<SerialDataReceivedEventArgs>(SerialData.Chars);
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

			// Act
			using (var proxy = GetProxy())
			{
				// Assert
				Assert.IsNotNull(proxy);
			}

			// Assert
			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Dispose(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldDispose()
		{
			// Arrange

			// Act
			using (var proxy = GetProxy())
			{
				proxy.Dispose();
			}

			// Assert
			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Dispose(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldStartAndStop()
		{
			// Arrange
			using var proxy = GetProxy();

			// Act
			await proxy.StartAsync();
			await proxy.StopAsync();

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Exactly(2));

			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Close(), Times.Exactly(2));

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Exactly(2));

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldThrowArgumentNullExceptionOnCreateInstanceForClient()
		{
			// Arrange

			// Act + Assert
			Assert.ThrowsException<ArgumentNullException>(() => new ModbusRtuProxy(null, "some-port"));
		}

		[DataTestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("  ")]
		public void ShouldThrowArgumentNullExceptionOnCreateInstanceForPortName(string portName)
		{
			// Arrange
			var connection = new Mock<IModbusConnection>();
			var clientMock = new Mock<ModbusClientBase>(connection.Object);

			// Act + Assert
			Assert.ThrowsException<ArgumentNullException>(() => new ModbusRtuProxy(clientMock.Object, portName));
		}

		[DataTestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("  ")]
		public async Task ShouldThrowArgumentNullExceptionOnMissingPortName(string portName)
		{
			// Arrange
			using var proxy = GetProxy();
			_serialPortMock.Setup(m => m.PortName).Returns(portName);

			// Act + Assert
			await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => proxy.StartAsync());
		}

		[TestMethod]
		public void ShouldGetAllProperties()
		{
			// Arrange
			using var proxy = GetProxy();

			// Act
			Assert.AreEqual("some-port", proxy.PortName);
			Assert.AreEqual(BaudRate.Baud19200, proxy.BaudRate);
			Assert.AreEqual(8, proxy.DataBits);
			Assert.AreEqual(Handshake.None, proxy.Handshake);
			Assert.AreEqual(Parity.Even, proxy.Parity);
			Assert.IsFalse(proxy.RtsEnable);
			Assert.AreEqual(StopBits.One, proxy.StopBits);
			Assert.IsTrue(proxy.IsOpen);
			Assert.AreEqual(1000, proxy.ReadTimeout.TotalMilliseconds);
			Assert.AreEqual(1000, proxy.WriteTimeout.TotalMilliseconds);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BaudRate, Times.Once);
			_serialPortMock.VerifyGet(m => m.DataBits, Times.Once);
			_serialPortMock.VerifyGet(m => m.Handshake, Times.Once);
			_serialPortMock.VerifyGet(m => m.Parity, Times.Once);
			_serialPortMock.VerifyGet(m => m.RtsEnable, Times.Once);
			_serialPortMock.VerifyGet(m => m.StopBits, Times.Once);
			_serialPortMock.VerifyGet(m => m.IsOpen, Times.Once);
			_serialPortMock.VerifyGet(m => m.ReadTimeout, Times.Once);
			_serialPortMock.VerifyGet(m => m.WriteTimeout, Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldSetAllProperties()
		{
			// Arrange
			using var proxy = GetProxy();

			// Act
			proxy.PortName = "other-port";
			proxy.BaudRate = BaudRate.Baud115200;
			proxy.DataBits = 7;
			proxy.Handshake = Handshake.RequestToSend;
			proxy.Parity = Parity.Odd;
			proxy.RtsEnable = true;
			proxy.StopBits = StopBits.OnePointFive;
			proxy.ReadTimeout = TimeSpan.FromSeconds(5);
			proxy.WriteTimeout = TimeSpan.FromSeconds(10);

			// Assert
			_serialPortMock.VerifySet(m => m.PortName = "other-port", Times.Once);
			_serialPortMock.VerifySet(m => m.BaudRate = 115200, Times.Once);
			_serialPortMock.VerifySet(m => m.DataBits = 7, Times.Once);
			_serialPortMock.VerifySet(m => m.Handshake = Handshake.RequestToSend, Times.Once);
			_serialPortMock.VerifySet(m => m.Parity = Parity.Odd, Times.Once);
			_serialPortMock.VerifySet(m => m.RtsEnable = true, Times.Once);
			_serialPortMock.VerifySet(m => m.StopBits = StopBits.OnePointFive, Times.Once);
			_serialPortMock.VerifySet(m => m.ReadTimeout = 5000, Times.Once);
			_serialPortMock.VerifySet(m => m.WriteTimeout = 10000, Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldIgnoreExceptionInDataReceived()
		{
			// Arrange
			// Not adding request data to the queue will cause an exception while reading
			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldSkipOnCrcError()
		{
			// Arrange
			byte[] request = [2, 1, 0, 5, 0, 4, 0, 0];
			_requestBytesQueue.Enqueue(request);

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnIllegalFunction()
		{
			// Arrange
			byte[] request = [1, 14, 0, 5, 0, 4];
			_requestBytesQueue.Enqueue(request);
			_requestBytesQueue.Enqueue(RtuProtocol.CRC16(request));
			byte[] expectedResponse = [1, 142, 1];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Exactly(2));

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Exactly(2));
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		#endregion General

		#region Read functions

		#region Read Coils (Fn 1)

		[TestMethod]
		public async Task ShouldReadCoils()
		{
			// Arrange
			byte[] request = [2, 1, 0, 5, 0, 4];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			_clientReadCoilsResponse = [
				new Coil { Address = 5, HighByte = 0xFF },
				new Coil { Address = 6, HighByte = 0x00 },
				new Coil { Address = 7, HighByte = 0x00 },
				new Coil { Address = 8, HighByte = 0xFF },
			];
			byte[] expectedResponse = [2, 1, 1, 9];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 6), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.ReadCoilsAsync(2, 5, 4, It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, startAddress, count) = _clientReadCallbacks.First();
			Assert.AreEqual(2, unitId);
			Assert.AreEqual(5, startAddress);
			Assert.AreEqual(4, count);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnReadCoils()
		{
			// Arrange
			byte[] request = [2, 1, 0, 5];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnReadCoils()
		{
			// Arrange
			byte[] request = [2, 1, 0, 5, 0, 4];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [2, 129, 4];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			_clientMock
				.Setup(m => m.ReadCoilsAsync(It.IsAny<byte>(), It.IsAny<ushort>(), It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
				.Callback<byte, ushort, ushort, CancellationToken>((unitId, address, count, _) => _clientReadCallbacks.Add((unitId, address, count)))
				.ThrowsAsync(new Exception("Error ;-)"));

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.ReadCoilsAsync(2, 5, 4, It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		#endregion Read Coils (Fn 1)

		#region Read Discrete Inputs (Fn 2)

		[TestMethod]
		public async Task ShouldReadDiscreteInputs()
		{
			// Arrange
			byte[] request = [22, 2, 0, 5, 0, 4];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			_clientReadDiscreteInputsResponse = [
				new DiscreteInput { Address = 5, HighByte = 0x00 },
				new DiscreteInput { Address = 6, HighByte = 0xFF },
				new DiscreteInput { Address = 7, HighByte = 0x00 },
				new DiscreteInput { Address = 8, HighByte = 0xFF },
			];
			byte[] expectedResponse = [22, 2, 1, 10];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 6), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.ReadDiscreteInputsAsync(22, 5, 4, It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, startAddress, count) = _clientReadCallbacks.First();
			Assert.AreEqual(22, unitId);
			Assert.AreEqual(5, startAddress);
			Assert.AreEqual(4, count);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnReadDiscreteInputs()
		{
			// Arrange
			byte[] request = [2, 2, 0, 5];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnDiscreteInputs()
		{
			// Arrange
			byte[] request = [2, 2, 0, 5, 0, 4];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [2, 130, 4];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			_clientMock
				.Setup(m => m.ReadDiscreteInputsAsync(It.IsAny<byte>(), It.IsAny<ushort>(), It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
				.Callback<byte, ushort, ushort, CancellationToken>((unitId, address, count, _) => _clientReadCallbacks.Add((unitId, address, count)))
				.ThrowsAsync(new Exception("Error ;-)"));

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.ReadDiscreteInputsAsync(2, 5, 4, It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		#endregion Read Discrete Inputs (Fn 2)

		#region Read Holding Registers (Fn 3)

		[TestMethod]
		public async Task ShouldReadHoldingRegisters()
		{
			// Arrange
			byte[] request = [42, 3, 0, 15, 0, 2];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			_clientReadHoldingRegistersResponse = [
				new HoldingRegister { Address = 15, LowByte = 12, HighByte = 34 },
				new HoldingRegister { Address = 16, LowByte = 56, HighByte = 78 },
			];
			byte[] expectedResponse = [42, 3, 4, 34, 12, 78, 56];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 9), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.ReadHoldingRegistersAsync(42, 15, 2, It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, startAddress, count) = _clientReadCallbacks.First();
			Assert.AreEqual(42, unitId);
			Assert.AreEqual(15, startAddress);
			Assert.AreEqual(2, count);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnReadHoldingRegisters()
		{
			// Arrange
			byte[] request = [2, 3, 0, 5];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnReadHoldingRegisters()
		{
			// Arrange
			byte[] request = [2, 3, 0, 5, 0, 4];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [2, 131, 4];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			_clientMock
				.Setup(m => m.ReadHoldingRegistersAsync(It.IsAny<byte>(), It.IsAny<ushort>(), It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
				.Callback<byte, ushort, ushort, CancellationToken>((unitId, address, count, _) => _clientReadCallbacks.Add((unitId, address, count)))
				.ThrowsAsync(new Exception("Error ;-)"));

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.ReadHoldingRegistersAsync(2, 5, 4, It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		#endregion Read Holding Registers (Fn 3)

		#region Read Input Registers (Fn 4)

		[TestMethod]
		public async Task ShouldReadInputRegisters()
		{
			// Arrange
			byte[] request = [42, 4, 0, 15, 0, 2];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			_clientReadInputRegistersResponse = [
				new InputRegister { Address = 15, LowByte = 34, HighByte = 12 },
				new InputRegister { Address = 16, LowByte = 78, HighByte = 56 },
			];
			byte[] expectedResponse = [42, 4, 4, 12, 34, 56, 78];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 9), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.ReadInputRegistersAsync(42, 15, 2, It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, startAddress, count) = _clientReadCallbacks.First();
			Assert.AreEqual(42, unitId);
			Assert.AreEqual(15, startAddress);
			Assert.AreEqual(2, count);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnReadInputRegisters()
		{
			// Arrange
			byte[] request = [2, 4, 0, 5];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnReadInputRegisters()
		{
			// Arrange
			byte[] request = [2, 4, 0, 5, 0, 4];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [2, 132, 4];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			_clientMock
				.Setup(m => m.ReadInputRegistersAsync(It.IsAny<byte>(), It.IsAny<ushort>(), It.IsAny<ushort>(), It.IsAny<CancellationToken>()))
				.Callback<byte, ushort, ushort, CancellationToken>((unitId, address, count, _) => _clientReadCallbacks.Add((unitId, address, count)))
				.ThrowsAsync(new Exception("Error ;-)"));

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.ReadInputRegistersAsync(2, 5, 4, It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		#endregion Read Input Registers (Fn 4)

		#region Read Encapsulated Interface (Fn 43)

		[TestMethod]
		public async Task ShouldReadDeviceIdentification()
		{
			// Arrange
			byte[] request = [1, 43, 14, 1, 0];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [
				1, 43, 14, 1,
				1, 0, 0, 3,
				0, 10, 86, 101, 110, 100, 111, 114, 78, 97, 109, 101,
				1, 11, 80, 114, 111, 100, 117, 99, 116, 67, 111, 100, 101,
				2, 18, 77, 97, 106, 111, 114, 77, 105, 110, 111, 114, 82, 101, 118, 105, 115, 105, 111, 110];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 55), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.ReadDeviceIdentificationAsync(1, ModbusDeviceIdentificationCategory.Basic, ModbusDeviceIdentificationObject.VendorName, It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, category, objectId) = _clientReadDeviceCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(ModbusDeviceIdentificationCategory.Basic, category);
			Assert.AreEqual(ModbusDeviceIdentificationObject.VendorName, objectId);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
			SnapshotAssert.AreEqual(_clientDeviceIdentificationResponse.ToString());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnReadDeviceIdentification()
		{
			// Arrange
			byte[] request = [1, 43, 14, 1];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnIllegalFunctionForWrongTypeOnReadDeviceIdentification()
		{
			// Arrange
			byte[] request = [1, 43, 13, 1, 0];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [1, 171, 1];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnIllegalDataAddressForWrongTypeOnReadDeviceIdentification()
		{
			// Arrange
			byte[] request = [1, 43, 14, 4, 10];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [1, 171, 2];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnIllegalDataValueForWrongTypeOnReadDeviceIdentification()
		{
			// Arrange
			byte[] request = [1, 43, 14, 0, 0];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [1, 171, 3];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReadDeviceIdentificationWithIndividualAccessAllowed()
		{
			// Arrange
			byte[] request = [1, 43, 14, 1, 0];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			_clientDeviceIdentificationResponse.IsIndividualAccessAllowed = true;
			byte[] expectedResponse = [
				1, 43, 14, 1,
				129, 0, 0, 3,
				0, 10, 86, 101, 110, 100, 111, 114, 78, 97, 109, 101,
				1, 11, 80, 114, 111, 100, 117, 99, 116, 67, 111, 100, 101,
				2, 18, 77, 97, 106, 111, 114, 77, 105, 110, 111, 114, 82, 101, 118, 105, 115, 105, 111, 110];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 55), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.ReadDeviceIdentificationAsync(1, ModbusDeviceIdentificationCategory.Basic, ModbusDeviceIdentificationObject.VendorName, It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, category, objectId) = _clientReadDeviceCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(ModbusDeviceIdentificationCategory.Basic, category);
			Assert.AreEqual(ModbusDeviceIdentificationObject.VendorName, objectId);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReadDeviceIdentificationRegular()
		{
			// Arrange
			byte[] request = [1, 43, 14, 2, 0];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [
				1, 43, 14, 2,
				2, 0, 0, 7,
				0, 10, 86, 101, 110, 100, 111, 114, 78, 97, 109, 101,
				1, 11, 80, 114, 111, 100, 117, 99, 116, 67, 111, 100, 101,
				2, 18, 77, 97, 106, 111, 114, 77, 105, 110, 111, 114, 82, 101, 118, 105, 115, 105, 111, 110,
				3, 0, 4, 0, 5, 0, 6, 0,];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 63), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.ReadDeviceIdentificationAsync(1, ModbusDeviceIdentificationCategory.Regular, ModbusDeviceIdentificationObject.VendorName, It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, category, objectId) = _clientReadDeviceCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(ModbusDeviceIdentificationCategory.Regular, category);
			Assert.AreEqual(ModbusDeviceIdentificationObject.VendorName, objectId);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReadDeviceIdentificationExtended()
		{
			// Arrange
			byte[] request = [1, 43, 14, 3, 0];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [
				1, 43, 14, 3,
				3, 255, 223, 102,
				0, 10, 86, 101, 110, 100, 111, 114, 78, 97, 109, 101,
				1, 11, 80, 114, 111, 100, 117, 99, 116, 67, 111, 100, 101,
				2, 18, 77, 97, 106, 111, 114, 77, 105, 110, 111, 114, 82, 101, 118, 105, 115, 105, 111, 110,
				3, 0, 4, 0, 5, 0, 6, 0,
				128, 0, 129, 0, 130, 0,
				131, 3, 11, 22, 33,
				132, 0, 133, 0, 134, 0, 135, 0, 136, 0, 137, 0, 138, 0, 139, 0, 140, 0, 141, 0, 142, 0, 143, 0,
				144, 0, 145, 0, 146, 0, 147, 0, 148, 0, 149, 0, 150, 0, 151, 0, 152, 0, 153, 0, 154, 0, 155, 0,
				156, 0, 157, 0, 158, 0, 159, 0, 160, 0, 161, 0, 162, 0, 163, 0, 164, 0, 165, 0, 166, 0, 167, 0,
				168, 0, 169, 0, 170, 0, 171, 0, 172, 0, 173, 0, 174, 0, 175, 0, 176, 0, 177, 0, 178, 0, 179, 0,
				180, 0, 181, 0, 182, 0, 183, 0, 184, 0, 185, 0, 186, 0, 187, 0, 188, 0, 189, 0, 190, 0, 191, 0,
				192, 0, 193, 0, 194, 0, 195, 0, 196, 0, 197, 0, 198, 0, 199, 0, 200, 0, 201, 0, 202, 0, 203, 0,
				204, 0, 205, 0, 206, 0, 207, 0, 208, 0, 209, 0, 210, 0, 211, 0, 212, 0, 213, 0, 214, 0, 215, 0,
				216, 0, 217, 0, 218, 0, 219, 0, 220, 0, 221, 0, 222, 0];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 256), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.ReadDeviceIdentificationAsync(1, ModbusDeviceIdentificationCategory.Extended, ModbusDeviceIdentificationObject.VendorName, It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, category, objectId) = _clientReadDeviceCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(ModbusDeviceIdentificationCategory.Extended, category);
			Assert.AreEqual(ModbusDeviceIdentificationObject.VendorName, objectId);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReadDeviceIdentificationIndividual()
		{
			// Arrange
			byte[] request = [1, 43, 14, 4, 0];
			_clientDeviceIdentificationResponse.IsIndividualAccessAllowed = true;
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [
				1, 43, 14, 4,
				132, 0, 0, 1,
				0, 10, 86, 101, 110, 100, 111, 114, 78, 97, 109, 101];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 22), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.ReadDeviceIdentificationAsync(1, ModbusDeviceIdentificationCategory.Individual, ModbusDeviceIdentificationObject.VendorName, It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, category, objectId) = _clientReadDeviceCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(ModbusDeviceIdentificationCategory.Individual, category);
			Assert.AreEqual(ModbusDeviceIdentificationObject.VendorName, objectId);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureForWrongTypeOnReadDeviceIdentification()
		{
			// Arrange
			byte[] request = [1, 43, 14, 1, 0];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [1, 171, 4];

			using var proxy = GetProxy();
			_clientMock.Setup(m => m.ReadDeviceIdentificationAsync(It.IsAny<byte>(), It.IsAny<ModbusDeviceIdentificationCategory>(), It.IsAny<ModbusDeviceIdentificationObject>(), It.IsAny<CancellationToken>()))
				.Callback<byte, ModbusDeviceIdentificationCategory, ModbusDeviceIdentificationObject, CancellationToken>((unitId, category, objectId, _) => _clientReadDeviceCallbacks.Add((unitId, category, objectId)))
				.ThrowsAsync(new ModbusException());
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.ReadDeviceIdentificationAsync(1, ModbusDeviceIdentificationCategory.Basic, ModbusDeviceIdentificationObject.VendorName, It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, category, objectId) = _clientReadDeviceCallbacks.First();
			Assert.AreEqual(1, unitId);
			Assert.AreEqual(ModbusDeviceIdentificationCategory.Basic, category);
			Assert.AreEqual(ModbusDeviceIdentificationObject.VendorName, objectId);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
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
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [3, 5, 0, 7, 255, 0];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 8), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.WriteSingleCoilAsync(3, It.IsAny<Coil>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, coil) = _writeSingleCoilCallbacks.First();
			Assert.AreEqual(3, unitId);
			Assert.AreEqual(7, coil.Address);
			Assert.IsTrue(coil.Value);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnWriteSingleCoil()
		{
			// Arrange
			byte[] request = [3, 5, 0, 7, 255];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnIllegalDataValueOnWriteSingleCoil()
		{
			// Arrange
			byte[] request = [3, 5, 0, 7, 250, 0];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [3, 133, 3];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnWriteSingleCoilForNotSuccessful()
		{
			// Arrange
			_clientWriteResponse = false;
			byte[] request = [3, 5, 0, 7, 255, 0];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [3, 133, 4];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.WriteSingleCoilAsync(3, It.IsAny<Coil>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, coil) = _writeSingleCoilCallbacks.First();
			Assert.AreEqual(3, unitId);
			Assert.AreEqual(7, coil.Address);
			Assert.IsTrue(coil.Value);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnWriteSingleCoilForException()
		{
			// Arrange
			byte[] request = [3, 5, 0, 7, 255, 0];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [3, 133, 4];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			_clientMock
				.Setup(m => m.WriteSingleCoilAsync(It.IsAny<byte>(), It.IsAny<Coil>(), It.IsAny<CancellationToken>()))
				.Callback<byte, Coil, CancellationToken>((unitId, coil, _) => _writeSingleCoilCallbacks.Add((unitId, coil)))
				.ThrowsAsync(new ModbusException());

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.WriteSingleCoilAsync(3, It.IsAny<Coil>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, coil) = _writeSingleCoilCallbacks.First();
			Assert.AreEqual(3, unitId);
			Assert.AreEqual(7, coil.Address);
			Assert.IsTrue(coil.Value);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		#endregion Write Single Coil (Fn 5)

		#region Write Single Register (Fn 6)

		[TestMethod]
		public async Task ShouldWriteSingleRegister()
		{
			// Arrange
			byte[] request = [4, 6, 0, 1, 0, 3];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [4, 6, 0, 1, 0, 3];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 8), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.WriteSingleHoldingRegisterAsync(4, It.IsAny<HoldingRegister>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, register) = _writeSingleRegisterCallbacks.First();
			Assert.AreEqual(4, unitId);
			Assert.AreEqual(1, register.Address);
			Assert.AreEqual(3, register.Value);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldIgnoreTooShortRequestOnWriteSingleRegister()
		{
			// Arrange
			byte[] request = [4, 6, 0, 1, 0];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnWriteSingleRegisterForNotSuccessful()
		{
			// Arrange
			_clientWriteResponse = false;
			byte[] request = [4, 6, 0, 1, 0, 3];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [4, 134, 4];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.WriteSingleHoldingRegisterAsync(4, It.IsAny<HoldingRegister>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, register) = _writeSingleRegisterCallbacks.First();
			Assert.AreEqual(4, unitId);
			Assert.AreEqual(1, register.Address);
			Assert.AreEqual(3, register.Value);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnWriteSingleRegisterForException()
		{
			// Arrange
			byte[] request = [4, 6, 0, 1, 0, 3];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [4, 134, 4];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			_clientMock
				.Setup(m => m.WriteSingleHoldingRegisterAsync(It.IsAny<byte>(), It.IsAny<HoldingRegister>(), It.IsAny<CancellationToken>()))
				.Callback<byte, HoldingRegister, CancellationToken>((unitId, register, _) => _writeSingleRegisterCallbacks.Add((unitId, register)))
				.ThrowsAsync(new ModbusException());

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.WriteSingleHoldingRegisterAsync(4, It.IsAny<HoldingRegister>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			var (unitId, register) = _writeSingleRegisterCallbacks.First();
			Assert.AreEqual(4, unitId);
			Assert.AreEqual(1, register.Address);
			Assert.AreEqual(3, register.Value);

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		#endregion Write Single Register (Fn 6)

		#region Write Multiple Coils (Fn 15)

		[TestMethod]
		public async Task ShouldWriteMultipleCoils()
		{
			// Arrange
			byte[] request = [1, 15, 0, 13, 0, 10, 2, 205, 1];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [1, 15, 0, 13, 0, 10];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 8), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.WriteMultipleCoilsAsync(1, It.IsAny<IReadOnlyList<Coil>>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());

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
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnIllegalDataValueOnWriteMultipleCoils()
		{
			// Arrange
			byte[] request = [1, 15, 0, 13, 0, 10, 2, 205];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [1, 143, 3];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnWriteMultipleCoilsForNotSuccessful()
		{
			// Arrange
			_clientWriteResponse = false;
			byte[] request = [1, 15, 0, 13, 0, 10, 2, 205, 1];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [1, 143, 4];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.WriteMultipleCoilsAsync(1, It.IsAny<IReadOnlyList<Coil>>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());

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
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [1, 143, 4];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			_clientMock
				.Setup(m => m.WriteMultipleCoilsAsync(It.IsAny<byte>(), It.IsAny<IReadOnlyList<Coil>>(), It.IsAny<CancellationToken>()))
				.Callback<byte, IReadOnlyList<Coil>, CancellationToken>((unitId, coils, _) => _writeMultipleCoilsCallbacks.Add((unitId, coils.ToList())))
				.ThrowsAsync(new ModbusException());

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.WriteMultipleCoilsAsync(1, It.IsAny<IReadOnlyList<Coil>>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());

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
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [1, 16, 0, 1, 0, 2];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 8), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.WriteMultipleHoldingRegistersAsync(1, It.IsAny<IReadOnlyList<HoldingRegister>>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());

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
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReturnIllegalDataValueOnWriteMultipleRegisters()
		{
			// Arrange
			byte[] request = [1, 16, 0, 1, 0, 2, 4, 0, 10, 1];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [1, 144, 3];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());
		}

		[TestMethod]
		public async Task ShouldReturnSlaveDeviceFailureOnWriteMultipleRegistersForNotSuccessful()
		{
			// Arrange
			_clientWriteResponse = false;
			byte[] request = [1, 16, 0, 1, 0, 2, 4, 0, 10, 1, 2];
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [1, 144, 4];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.WriteMultipleHoldingRegistersAsync(1, It.IsAny<IReadOnlyList<HoldingRegister>>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());

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
			_requestBytesQueue.Enqueue([.. request, .. RtuProtocol.CRC16(request)]);
			byte[] expectedResponse = [1, 144, 4];

			using var proxy = GetProxy();
			await proxy.StartAsync();

			_clientMock
				.Setup(m => m.WriteMultipleHoldingRegistersAsync(It.IsAny<byte>(), It.IsAny<IReadOnlyList<HoldingRegister>>(), It.IsAny<CancellationToken>()))
				.Callback<byte, IReadOnlyList<HoldingRegister>, CancellationToken>((unitId, coils, _) => _writeMultipleRegistersCallbacks.Add((unitId, coils.ToList())))
				.ThrowsAsync(new ModbusException());

			// Act
			_serialPortMock.Raise(m => m.DataReceived += null, _dataReceivedEventArgs);

			// Assert
			_serialPortMock.VerifyGet(m => m.PortName, Times.Once);
			_serialPortMock.VerifyGet(m => m.BytesToRead, Times.Once);

			_serialPortMock.Verify(m => m.Close(), Times.Once);
			_serialPortMock.Verify(m => m.Open(), Times.Once);
			_serialPortMock.Verify(m => m.Read(It.IsAny<byte[]>(), 0, RtuProtocol.MAX_ADU_LENGTH), Times.Once);
			_serialPortMock.Verify(m => m.Write(It.IsAny<byte[]>(), 0, 5), Times.Once);

			_serialPortMock.VerifyAdd(m => m.DataReceived += It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);
			_serialPortMock.VerifyRemove(m => m.DataReceived -= It.IsAny<SerialDataReceivedEventHandler>(), Times.Once);

			_clientMock.Verify(m => m.WriteMultipleHoldingRegistersAsync(1, It.IsAny<IReadOnlyList<HoldingRegister>>(), It.IsAny<CancellationToken>()), Times.Once);

			VerifyNoOtherCalls();

			CollectionAssert.AreEqual(expectedResponse.Concat(RtuProtocol.CRC16(expectedResponse)).ToArray(), _responseBytesCallbacks.First());

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
			_serialPortMock.VerifyNoOtherCalls();
		}

		private ModbusRtuProxy GetProxy()
		{
			var connection = new Mock<IModbusConnection>();

			_clientMock = new Mock<ModbusClientBase>(connection.Object);
			_serialPortMock = new Mock<SerialPortWrapper>();

			#region General

			_serialPortMock.Setup(m => m.PortName).Returns("some-port");
			_serialPortMock.Setup(m => m.BaudRate).Returns(19200);
			_serialPortMock.Setup(m => m.DataBits).Returns(8);
			_serialPortMock.Setup(m => m.Handshake).Returns(Handshake.None);
			_serialPortMock.Setup(m => m.Parity).Returns(Parity.Even);
			_serialPortMock.Setup(m => m.RtsEnable).Returns(false);
			_serialPortMock.Setup(m => m.StopBits).Returns(StopBits.One);
			_serialPortMock.Setup(m => m.IsOpen).Returns(true);
			_serialPortMock.Setup(m => m.ReadTimeout).Returns(1000);
			_serialPortMock.Setup(m => m.WriteTimeout).Returns(1000);

			_serialPortMock
				.Setup(m => m.BytesToRead)
				// This does not reflect the correct value but is sufficient for testing
				.Returns(() => _requestBytesQueue.Count);

			_serialPortMock
				.Setup(m => m.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
				.Returns<byte[], int, int>((buffer, offset, count) =>
				{
					byte[] bytes = _requestBytesQueue.Dequeue();
					int minLength = Math.Min(bytes.Length, count);

					Array.Copy(bytes, 0, buffer, offset, minLength);
					return minLength;
				});
			_serialPortMock
				.Setup(m => m.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
				.Callback<byte[], int, int>((buffer, _, __) => _responseBytesCallbacks.Add(buffer));

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

			var proxy = new ModbusRtuProxy(_clientMock.Object, "some-port");
			var serialPortField = proxy.GetType().GetField("_serialPort", BindingFlags.NonPublic | BindingFlags.Instance);

			((IDisposable)serialPortField.GetValue(proxy)).Dispose();
			serialPortField.SetValue(proxy, _serialPortMock.Object);

			return proxy;
		}
	}
}
