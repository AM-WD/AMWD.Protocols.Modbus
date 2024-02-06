using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.Modbus.Common.Contracts;
using Moq;

namespace AMWD.Protocols.Modbus.Tests.Common.Contracts
{
	[TestClass]
	public class ModbusClientBaseTest
	{
		// Consts
		private const byte UNIT_ID = 42;
		private const ushort START_ADDRESS = 123;
		private const ushort READ_COUNT = 12;

		// Mocks
		private Mock<IModbusConnection> _connection;
		private Mock<IModbusProtocol> _protocol;

		// Responses
		private bool _connectionIsConnectecd;
		private List<Coil> _readCoilsResponse;
		private List<DiscreteInput> _readDiscreteInputsResponse;
		private List<HoldingRegister> _readHoldingRegistersResponse;
		private List<InputRegister> _readInputRegistersResponse;
		private Coil _writeSingleCoilResponse;
		private HoldingRegister _writeSingleHoldingRegisterResponse;
		private (ushort startAddress, ushort count) _writeMultipleCoilsResponse;
		private (ushort startAddress, ushort count) _writeMultipleHoldingRegistersResponse;

		[TestInitialize]
		public void Initialize()
		{
			_connectionIsConnectecd = true;

			_readCoilsResponse = new List<Coil>();
			_readDiscreteInputsResponse = new List<DiscreteInput>();
			_readHoldingRegistersResponse = new List<HoldingRegister>();
			_readInputRegistersResponse = new List<InputRegister>();

			for (int i = 0; i < READ_COUNT; i++)
			{
				_readCoilsResponse.Add(new Coil
				{
					Address = (ushort)i,
					HighByte = (byte)((i % 2 == 0) ? 0xFF : 0x00)
				});
				_readDiscreteInputsResponse.Add(new DiscreteInput
				{
					Address = (ushort)i,
					HighByte = (byte)((i % 2 == 1) ? 0xFF : 0x00)
				});

				_readHoldingRegistersResponse.Add(new HoldingRegister
				{
					Address = (ushort)i,
					HighByte = 0x00,
					LowByte = (byte)(i + 10)
				});
				_readInputRegistersResponse.Add(new InputRegister
				{
					Address = (ushort)i,
					HighByte = 0x00,
					LowByte = (byte)(i + 15)
				});
			}

			_writeSingleCoilResponse = new Coil { Address = START_ADDRESS };
			_writeSingleHoldingRegisterResponse = new HoldingRegister { Address = START_ADDRESS, Value = 0x1234 };

			_writeMultipleCoilsResponse = (START_ADDRESS, READ_COUNT);
			_writeMultipleHoldingRegistersResponse = (START_ADDRESS, READ_COUNT);
		}

		[TestMethod]
		public void ShouldPrettyPrint()
		{
			// Arrange
			var client = GetClient();

			// Act
			string str = client.ToString();

			// Assert
			Assert.AreEqual("Modbus client using Moq protocol to connect via Mock", str);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ShouldThrowExceptionOnNullConnection()
		{
			// Arrange
			IModbusConnection connection = null;

			// Act
			new ModbusClientBaseWrapper(connection);

			// Assert - ArgumentNullException
		}

		[TestMethod]
		public async Task ShouldConnectSuccessfully()
		{
			// Arrange
			var client = GetClient();

			// Act
			await client.ConnectAsync();

			// Assert
			_connection.Verify(c => c.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldDisconnectSuccessfully()
		{
			// Arrange
			var client = GetClient();

			// Act
			await client.DisconnectAsync();

			// Assert
			_connection.Verify(c => c.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.VerifyNoOtherCalls();
		}

		[DataTestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void ShouldAlsoDisposeConnection(bool disposeConnection)
		{
			// Arrange
			var client = GetClient(disposeConnection);

			// Act
			client.Dispose();

			// Assert
			if (disposeConnection)
				_connection.Verify(c => c.Dispose(), Times.Once);

			_connection.VerifyNoOtherCalls();

			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldAllowDisposeMultipleTimes()
		{
			// Arrange
			var client = GetClient();

			// Act
			client.Dispose();
			client.Dispose();

			// Assert
			_connection.Verify(c => c.Dispose(), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		[ExpectedException(typeof(ObjectDisposedException))]
		public async Task ShouldAssertDisposed()
		{
			// Arrange
			var client = GetClient();
			client.Dispose();

			// Act
			await client.ReadCoilsAsync(UNIT_ID, START_ADDRESS, READ_COUNT);

			// Assert - ObjectDisposedException
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task ShouldAssertProtocolSet()
		{
			// Arrange
			var client = GetClient();
			client.Protocol = null;

			// Act
			await client.ReadCoilsAsync(UNIT_ID, START_ADDRESS, READ_COUNT);

			// Assert - ArgumentNullException
		}

		[TestMethod]
		[ExpectedException(typeof(ApplicationException))]
		public async Task ShouldAssertConnected()
		{
			// Arrange
			_connectionIsConnectecd = false;
			var client = GetClient();

			// Act
			await client.ReadCoilsAsync(UNIT_ID, START_ADDRESS, READ_COUNT);

			// Assert - ApplicationException
		}

		[TestMethod]
		public async Task ShouldReadCoils()
		{
			// Arrange
			_readCoilsResponse.Add(new Coil());
			var client = GetClient();

			// Act
			var result = await client.ReadCoilsAsync(UNIT_ID, START_ADDRESS, READ_COUNT);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(READ_COUNT, result.Count);

			for (int i = 0; i < READ_COUNT; i++)
			{
				Assert.AreEqual(START_ADDRESS + i, result[i].Address);
				Assert.AreEqual(i % 2 == 0, result[i].Value);
			}

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeReadCoils(UNIT_ID, START_ADDRESS, READ_COUNT), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeReadCoils(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReadDiscreteInputs()
		{
			// Arrange
			_readDiscreteInputsResponse.Add(new DiscreteInput());
			var client = GetClient();

			// Act
			var result = await client.ReadDiscreteInputsAsync(UNIT_ID, START_ADDRESS, READ_COUNT);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(READ_COUNT, result.Count);

			for (int i = 0; i < READ_COUNT; i++)
			{
				Assert.AreEqual(START_ADDRESS + i, result[i].Address);
				Assert.AreEqual(i % 2 == 1, result[i].Value);
			}

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeReadDiscreteInputs(UNIT_ID, START_ADDRESS, READ_COUNT), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeReadDiscreteInputs(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReadHoldingRegisters()
		{
			// Arrange
			var client = GetClient();

			// Act
			var result = await client.ReadHoldingRegistersAsync(UNIT_ID, START_ADDRESS, READ_COUNT);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(READ_COUNT, result.Count);

			for (int i = 0; i < READ_COUNT; i++)
			{
				Assert.AreEqual(START_ADDRESS + i, result[i].Address);
				Assert.AreEqual(i + 10, result[i].Value);
			}

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeReadHoldingRegisters(UNIT_ID, START_ADDRESS, READ_COUNT), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeReadHoldingRegisters(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldReadInputRegisters()
		{
			// Arrange
			var client = GetClient();

			// Act
			var result = await client.ReadInputRegistersAsync(UNIT_ID, START_ADDRESS, READ_COUNT);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(READ_COUNT, result.Count);

			for (int i = 0; i < READ_COUNT; i++)
			{
				Assert.AreEqual(START_ADDRESS + i, result[i].Address);
				Assert.AreEqual(i + 15, result[i].Value);
			}

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeReadInputRegisters(UNIT_ID, START_ADDRESS, READ_COUNT), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeReadInputRegisters(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldWriteSingleCoil()
		{
			// Arrange
			var coil = new Coil
			{
				Address = START_ADDRESS,
				Value = false
			};
			var client = GetClient();

			// Act
			bool result = await client.WriteSingleCoilAsync(UNIT_ID, coil);

			// Assert
			Assert.IsTrue(result);

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeWriteSingleCoil(UNIT_ID, coil), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeWriteSingleCoil(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldFailWriteSingleCoilOnAddress()
		{
			// Arrange
			var coil = new Coil
			{
				Address = START_ADDRESS + 1,
				Value = false
			};
			var client = GetClient();

			// Act
			bool result = await client.WriteSingleCoilAsync(UNIT_ID, coil);

			// Assert
			Assert.IsFalse(result);

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeWriteSingleCoil(UNIT_ID, coil), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeWriteSingleCoil(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldFailWriteSingleCoilOnValue()
		{
			// Arrange
			var coil = new Coil
			{
				Address = START_ADDRESS,
				Value = true
			};
			var client = GetClient();

			// Act
			bool result = await client.WriteSingleCoilAsync(UNIT_ID, coil);

			// Assert
			Assert.IsFalse(result);

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeWriteSingleCoil(UNIT_ID, coil), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeWriteSingleCoil(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldWriteSingleHoldingRegister()
		{
			// Arrange
			var register = new HoldingRegister
			{
				Address = START_ADDRESS,
				Value = 0x1234
			};
			var client = GetClient();

			// Act
			bool result = await client.WriteSingleHoldingRegisterAsync(UNIT_ID, register);

			// Assert
			Assert.IsTrue(result);

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeWriteSingleHoldingRegister(UNIT_ID, register), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeWriteSingleHoldingRegister(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldFailWriteSingleHoldingRegisterOnAddress()
		{
			// Arrange
			var register = new HoldingRegister
			{
				Address = START_ADDRESS + 1,
				Value = 0x1234
			};
			var client = GetClient();

			// Act
			bool result = await client.WriteSingleHoldingRegisterAsync(UNIT_ID, register);

			// Assert
			Assert.IsFalse(result);

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeWriteSingleHoldingRegister(UNIT_ID, register), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeWriteSingleHoldingRegister(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldFailWriteSingleHoldingRegisterOnValue()
		{
			// Arrange
			var register = new HoldingRegister
			{
				Address = START_ADDRESS,
				Value = 0x1233
			};
			var client = GetClient();

			// Act
			bool result = await client.WriteSingleHoldingRegisterAsync(UNIT_ID, register);

			// Assert
			Assert.IsFalse(result);

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeWriteSingleHoldingRegister(UNIT_ID, register), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeWriteSingleHoldingRegister(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldWriteMultipleCoils()
		{
			// Arrange
			var coils = new List<Coil>();
			for (int i = 0; i < READ_COUNT; i++)
			{
				coils.Add(new Coil
				{
					Address = (ushort)(START_ADDRESS + i),
					Value = i % 2 == 0
				});
			}
			var client = GetClient();

			// Act
			bool result = await client.WriteMultipleCoilsAsync(UNIT_ID, coils);

			// Assert
			Assert.IsTrue(result);

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeWriteMultipleCoils(UNIT_ID, coils), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeWriteMultipleCoils(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldFailWriteMultipleCoilsOnAddress()
		{
			// Arrange
			_writeMultipleCoilsResponse.startAddress = START_ADDRESS + 1;
			var coils = new List<Coil>();
			for (int i = 0; i < READ_COUNT; i++)
			{
				coils.Add(new Coil
				{
					Address = (ushort)(START_ADDRESS + i),
					Value = i % 2 == 0
				});
			}
			var client = GetClient();

			// Act
			bool result = await client.WriteMultipleCoilsAsync(UNIT_ID, coils);

			// Assert
			Assert.IsFalse(result);

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeWriteMultipleCoils(UNIT_ID, coils), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeWriteMultipleCoils(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldFailWriteMultipleCoilsOnCount()
		{
			// Arrange
			_writeMultipleCoilsResponse.count = READ_COUNT + 1;
			var coils = new List<Coil>();
			for (int i = 0; i < READ_COUNT; i++)
			{
				coils.Add(new Coil
				{
					Address = (ushort)(START_ADDRESS + i),
					Value = i % 2 == 0
				});
			}
			var client = GetClient();

			// Act
			bool result = await client.WriteMultipleCoilsAsync(UNIT_ID, coils);

			// Assert
			Assert.IsFalse(result);

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeWriteMultipleCoils(UNIT_ID, coils), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeWriteMultipleCoils(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldWriteMultipleRegisters()
		{
			// Arrange
			var registers = new List<HoldingRegister>();
			for (int i = 0; i < READ_COUNT; i++)
			{
				registers.Add(new HoldingRegister
				{
					Address = (ushort)(START_ADDRESS + i),
					Value = (ushort)i
				});
			}
			var client = GetClient();

			// Act
			bool result = await client.WriteMultipleHoldingRegistersAsync(UNIT_ID, registers);

			// Assert
			Assert.IsTrue(result);

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeWriteMultipleHoldingRegisters(UNIT_ID, registers), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeWriteMultipleHoldingRegisters(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldFailWriteMultiplRegistersOnAddress()
		{
			// Arrange
			_writeMultipleHoldingRegistersResponse.startAddress = START_ADDRESS + 1;
			var registers = new List<HoldingRegister>();
			for (int i = 0; i < READ_COUNT; i++)
			{
				registers.Add(new HoldingRegister
				{
					Address = (ushort)(START_ADDRESS + i),
					Value = (ushort)i
				});
			}
			var client = GetClient();

			// Act
			bool result = await client.WriteMultipleHoldingRegistersAsync(UNIT_ID, registers);

			// Assert
			Assert.IsFalse(result);

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeWriteMultipleHoldingRegisters(UNIT_ID, registers), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeWriteMultipleHoldingRegisters(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		[TestMethod]
		public async Task ShouldFailWriteMultipleRegistersOnCount()
		{
			// Arrange
			_writeMultipleHoldingRegistersResponse.count = READ_COUNT + 1;
			var registers = new List<HoldingRegister>();
			for (int i = 0; i < READ_COUNT; i++)
			{
				registers.Add(new HoldingRegister
				{
					Address = (ushort)(START_ADDRESS + i),
					Value = (ushort)i
				});
			}
			var client = GetClient();

			// Act
			bool result = await client.WriteMultipleHoldingRegistersAsync(UNIT_ID, registers);

			// Assert
			Assert.IsFalse(result);

			_connection.VerifyGet(c => c.IsConnected, Times.Once);
			_connection.Verify(c => c.InvokeAsync(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<Func<IReadOnlyList<byte>, bool>>(), It.IsAny<CancellationToken>()), Times.Once);
			_connection.VerifyNoOtherCalls();

			_protocol.Verify(p => p.SerializeWriteMultipleHoldingRegisters(UNIT_ID, registers), Times.Once);
			_protocol.Verify(p => p.ValidateResponse(It.IsAny<IReadOnlyList<byte>>(), It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.Verify(p => p.DeserializeWriteMultipleHoldingRegisters(It.IsAny<IReadOnlyList<byte>>()), Times.Once);
			_protocol.VerifyNoOtherCalls();
		}

		private ModbusClientBase GetClient(bool disposeConnection = true)
		{
			_connection = new Mock<IModbusConnection>();
			_connection
				.SetupGet(c => c.Name)
				.Returns("Mock");
			_connection
				.SetupGet(c => c.IsConnected)
				.Returns(() => _connectionIsConnectecd);

			_protocol = new Mock<IModbusProtocol>();
			_protocol
				.SetupGet(p => p.Name)
				.Returns("Moq");
			_protocol
				.Setup(p => p.DeserializeReadCoils(It.IsAny<IReadOnlyList<byte>>()))
				.Returns(() => _readCoilsResponse);
			_protocol
				.Setup(p => p.DeserializeReadDiscreteInputs(It.IsAny<IReadOnlyList<byte>>()))
				.Returns(() => _readDiscreteInputsResponse);
			_protocol
				.Setup(p => p.DeserializeReadHoldingRegisters(It.IsAny<IReadOnlyList<byte>>()))
				.Returns(() => _readHoldingRegistersResponse);
			_protocol
				.Setup(p => p.DeserializeReadInputRegisters(It.IsAny<IReadOnlyList<byte>>()))
				.Returns(() => _readInputRegistersResponse);

			_protocol
				.Setup(p => p.DeserializeWriteSingleCoil(It.IsAny<IReadOnlyList<byte>>()))
				.Returns(() => _writeSingleCoilResponse);
			_protocol
				.Setup(p => p.DeserializeWriteSingleHoldingRegister(It.IsAny<IReadOnlyList<byte>>()))
				.Returns(() => _writeSingleHoldingRegisterResponse);
			_protocol
				.Setup(p => p.DeserializeWriteMultipleCoils(It.IsAny<IReadOnlyList<byte>>()))
				.Returns(() => _writeMultipleCoilsResponse);
			_protocol
				.Setup(p => p.DeserializeWriteMultipleHoldingRegisters(It.IsAny<IReadOnlyList<byte>>()))
				.Returns(() => _writeMultipleHoldingRegistersResponse);

			return new ModbusClientBaseWrapper(_connection.Object, disposeConnection)
			{
				Protocol = _protocol.Object,
			};
		}

		internal class ModbusClientBaseWrapper : ModbusClientBase
		{
			public ModbusClientBaseWrapper(IModbusConnection connection)
				: base(connection)
			{ }

			public ModbusClientBaseWrapper(IModbusConnection connection, bool disposeConnection)
				: base(connection, disposeConnection)
			{ }

			public override IModbusProtocol Protocol { get; set; }
		}
	}
}
