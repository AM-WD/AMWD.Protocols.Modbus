using System.Collections.Generic;
using System.Reflection;
using AMWD.Protocols.Modbus.Common.Protocols;

namespace AMWD.Protocols.Modbus.Tests.Common.Protocols
{
	[TestClass]
	public class TcpProtocolTest
	{
		private const byte UNIT_ID = 0x2A; // 42

		#region Read Coils

		[TestMethod]
		public void ShouldSerializeReadCoils()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			var bytes = protocol.SerializeReadCoils(UNIT_ID, 19, 19);

			// Assert
			Assert.IsNotNull(bytes);
			Assert.AreEqual(12, bytes.Count);

			//    Transaction id
			Assert.AreEqual(0x00, bytes[0]);
			Assert.AreEqual(0x01, bytes[1]);

			//    Protocol identifier
			Assert.AreEqual(0x00, bytes[2]);
			Assert.AreEqual(0x00, bytes[3]);

			//    Following bytes
			Assert.AreEqual(0x00, bytes[4]);
			Assert.AreEqual(0x06, bytes[5]);

			//    Unit id
			Assert.AreEqual(UNIT_ID, bytes[6]);

			//    Function code
			Assert.AreEqual(0x01, bytes[7]);

			//    Starting address
			Assert.AreEqual(0x00, bytes[8]);
			Assert.AreEqual(0x13, bytes[9]);
			//    Quantity
			Assert.AreEqual(0x00, bytes[10]);
			Assert.AreEqual(0x13, bytes[11]);
		}

		[DataTestMethod]
		[DataRow(0)]
		[DataRow(2001)]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void ShouldThrowOutOfRangeForCountOnSerializeReadCoils(int count)
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeReadCoils(UNIT_ID, 19, (ushort)count);

			// Assert - ArgumentOutOfRangeException
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void ShouldThrowOutOfRangeForStartingAddressOnSerializeReadCoils()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeReadCoils(UNIT_ID, ushort.MaxValue, 2);

			// Assert - ArgumentOutOfRangeException
		}

		[TestMethod]
		public void ShouldDeserializeReadCoils()
		{
			// Arrange
			int[] setValues = [0, 2, 3, 6, 7, 8, 9, 11, 13, 14, 16, 18];
			var protocol = new TcpProtocol();

			// Act
			var coils = protocol.DeserializeReadCoils([0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x01, 0x03, 0xCD, 0x6B, 0x05]);

			// Assert
			Assert.IsNotNull(coils);
			Assert.AreEqual(24, coils.Count);

			for (int i = 0; i < 24; i++)
			{
				Assert.AreEqual(i, coils[i].Address);

				if (setValues.Contains(i))
					Assert.IsTrue(coils[i].Value);
				else
					Assert.IsFalse(coils[i].Value);
			}
		}

		[TestMethod]
		[ExpectedException(typeof(ModbusException))]
		public void ShouldThrowExceptionOnDeserializeReadCoils()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			var coils = protocol.DeserializeReadCoils([0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x01, 0x02, 0xCD, 0x6B, 0x05]);

			// Assert - ModbusException
		}

		#endregion Read Coils

		#region Read Discrete Inputs

		[TestMethod]
		public void ShouldSerializeReadDiscreteInputs()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			var bytes = protocol.SerializeReadDiscreteInputs(UNIT_ID, 260, 16);

			// Assert
			Assert.IsNotNull(bytes);
			Assert.AreEqual(12, bytes.Count);

			//    Transaction id
			Assert.AreEqual(0x00, bytes[0]);
			Assert.AreEqual(0x01, bytes[1]);

			//    Protocol identifier
			Assert.AreEqual(0x00, bytes[2]);
			Assert.AreEqual(0x00, bytes[3]);

			//    Following bytes
			Assert.AreEqual(0x00, bytes[4]);
			Assert.AreEqual(0x06, bytes[5]);

			//    Unit id
			Assert.AreEqual(UNIT_ID, bytes[6]);

			//    Function code
			Assert.AreEqual(0x02, bytes[7]);

			//    Starting address
			Assert.AreEqual(0x01, bytes[8]);
			Assert.AreEqual(0x04, bytes[9]);
			//    Quantity
			Assert.AreEqual(0x00, bytes[10]);
			Assert.AreEqual(0x10, bytes[11]);
		}

		[DataTestMethod]
		[DataRow(0)]
		[DataRow(2001)]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void ShouldThrowOutOfRangeForCountOnSerializeReadDiscreteInputs(int count)
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeReadDiscreteInputs(UNIT_ID, 19, (ushort)count);

			// Assert - ArgumentOutOfRangeException
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void ShouldThrowOutOfRangeForStartingAddressOnSerializeReadDiscreteInputs()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeReadDiscreteInputs(UNIT_ID, ushort.MaxValue, 2);

			// Assert - ArgumentOutOfRangeException
		}

		[TestMethod]
		public void ShouldDeserializeReadDiscreteInputs()
		{
			// Arrange
			int[] setValues = [0, 2, 3, 6, 7, 8, 9, 11, 13, 14, 16, 17];
			var protocol = new TcpProtocol();

			// Act
			var inputs = protocol.DeserializeReadDiscreteInputs([0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x02, 0x03, 0xCD, 0x6B, 0x03]);

			// Assert
			Assert.IsNotNull(inputs);
			Assert.AreEqual(24, inputs.Count);

			for (int i = 0; i < 24; i++)
			{
				Assert.AreEqual(i, inputs[i].Address);

				if (setValues.Contains(i))
					Assert.IsTrue(inputs[i].Value);
				else
					Assert.IsFalse(inputs[i].Value);
			}
		}

		[TestMethod]
		[ExpectedException(typeof(ModbusException))]
		public void ShouldThrowExceptionOnDeserializeReadDiscreteInputs()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.DeserializeReadDiscreteInputs([0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x02, 0x03, 0xCD, 0x6B]);

			// Assert - ModbusException
		}

		#endregion Read Discrete Inputs

		#region Read Holding Registers

		[TestMethod]
		public void ShouldSerializeReadHoldingRegisters()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			var bytes = protocol.SerializeReadHoldingRegisters(UNIT_ID, 107, 2);

			// Assert
			Assert.IsNotNull(bytes);
			Assert.AreEqual(12, bytes.Count);

			//    Transaction id
			Assert.AreEqual(0x00, bytes[0]);
			Assert.AreEqual(0x01, bytes[1]);

			//    Protocol identifier
			Assert.AreEqual(0x00, bytes[2]);
			Assert.AreEqual(0x00, bytes[3]);

			//    Following bytes
			Assert.AreEqual(0x00, bytes[4]);
			Assert.AreEqual(0x06, bytes[5]);

			//    Unit id
			Assert.AreEqual(UNIT_ID, bytes[6]);

			//    Function code
			Assert.AreEqual(0x03, bytes[7]);

			//    Starting address
			Assert.AreEqual(0x00, bytes[8]);
			Assert.AreEqual(0x6B, bytes[9]);
			//    Quantity
			Assert.AreEqual(0x00, bytes[10]);
			Assert.AreEqual(0x02, bytes[11]);
		}

		[DataTestMethod]
		[DataRow(0)]
		[DataRow(126)]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void ShouldThrowOutOfRangeForCountOnSerializeReadHoldingRegisters(int count)
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeReadHoldingRegisters(UNIT_ID, 19, (ushort)count);

			// Assert - ArgumentOutOfRangeException
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void ShouldThrowOutOfRangeForStartingAddressOnSerializeReadHoldingRegisters()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeReadHoldingRegisters(UNIT_ID, ushort.MaxValue, 2);

			// Assert - ArgumentOutOfRangeException
		}

		[TestMethod]
		public void ShouldDeserializeReadHoldingRegisters()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			var registers = protocol.DeserializeReadHoldingRegisters([0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x03, 0x04, 0x02, 0x2B, 0x00, 0x64]);

			// Assert
			Assert.IsNotNull(registers);
			Assert.AreEqual(2, registers.Count);

			Assert.AreEqual(0, registers[0].Address);
			Assert.AreEqual(555, registers[0].Value);

			Assert.AreEqual(1, registers[1].Address);
			Assert.AreEqual(100, registers[1].Value);
		}

		[TestMethod]
		[ExpectedException(typeof(ModbusException))]
		public void ShouldThrowExceptionOnDeserializeReadHoldingRegisters()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.DeserializeReadHoldingRegisters([0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x03, 0x04, 0x02, 0x2B]);

			// Assert - ModbusException
		}

		#endregion Read Holding Registers

		#region Read Input Registers

		[TestMethod]
		public void ShouldSerializeReadInputRegisters()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			var bytes = protocol.SerializeReadInputRegisters(UNIT_ID, 109, 3);

			// Assert
			Assert.IsNotNull(bytes);
			Assert.AreEqual(12, bytes.Count);

			//    Transaction id
			Assert.AreEqual(0x00, bytes[0]);
			Assert.AreEqual(0x01, bytes[1]);

			//    Protocol identifier
			Assert.AreEqual(0x00, bytes[2]);
			Assert.AreEqual(0x00, bytes[3]);

			//    Following bytes
			Assert.AreEqual(0x00, bytes[4]);
			Assert.AreEqual(0x06, bytes[5]);

			//    Unit id
			Assert.AreEqual(UNIT_ID, bytes[6]);

			//    Function code
			Assert.AreEqual(0x04, bytes[7]);

			//    Starting address
			Assert.AreEqual(0x00, bytes[8]);
			Assert.AreEqual(0x6D, bytes[9]);
			//    Quantity
			Assert.AreEqual(0x00, bytes[10]);
			Assert.AreEqual(0x03, bytes[11]);
		}

		[DataTestMethod]
		[DataRow(0)]
		[DataRow(126)]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void ShouldThrowOutOfRangeForCountOnSerializeReadInputRegisters(int count)
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeReadInputRegisters(UNIT_ID, 19, (ushort)count);

			// Assert - ArgumentOutOfRangeException
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void ShouldThrowOutOfRangeForStartingAddressOnSerializeReadInputRegisters()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeReadInputRegisters(UNIT_ID, ushort.MaxValue, 2);

			// Assert - ArgumentOutOfRangeException
		}

		[TestMethod]
		public void ShouldDeserializeReadInputRegisters()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			var registers = protocol.DeserializeReadInputRegisters([0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x04, 0x04, 0x02, 0x2A, 0x00, 0x60]);

			// Assert
			Assert.IsNotNull(registers);
			Assert.AreEqual(2, registers.Count);

			Assert.AreEqual(0, registers[0].Address);
			Assert.AreEqual(554, registers[0].Value);

			Assert.AreEqual(1, registers[1].Address);
			Assert.AreEqual(96, registers[1].Value);
		}

		[TestMethod]
		[ExpectedException(typeof(ModbusException))]
		public void ShouldThrowExceptionOnDeserializeReadInputRegisters()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.DeserializeReadInputRegisters([0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x04, 0x04, 0x02, 0x2B]);

			// Assert - ModbusException
		}

		#endregion Read Input Registers

		#region Read Device Identification

		[DataTestMethod]
		[DataRow(ModbusDeviceIdentificationCategory.Basic)]
		[DataRow(ModbusDeviceIdentificationCategory.Regular)]
		[DataRow(ModbusDeviceIdentificationCategory.Extended)]
		[DataRow(ModbusDeviceIdentificationCategory.Individual)]
		public void ShouldSerializeReadDeviceIdentification(ModbusDeviceIdentificationCategory category)
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			var bytes = protocol.SerializeReadDeviceIdentification(UNIT_ID, category, ModbusDeviceIdentificationObject.ProductCode);

			// Assert
			Assert.IsNotNull(bytes);
			Assert.AreEqual(11, bytes.Count);

			//    Transaction id
			Assert.AreEqual(0x00, bytes[0]);
			Assert.AreEqual(0x01, bytes[1]);

			//    Protocol identifier
			Assert.AreEqual(0x00, bytes[2]);
			Assert.AreEqual(0x00, bytes[3]);

			//    Following bytes
			Assert.AreEqual(0x00, bytes[4]);
			Assert.AreEqual(0x05, bytes[5]);

			//    Unit id
			Assert.AreEqual(UNIT_ID, bytes[6]);

			//    Function code
			Assert.AreEqual(0x2B, bytes[7]);

			//    MEI Type
			Assert.AreEqual(0x0E, bytes[8]);

			//    Category
			Assert.AreEqual((byte)category, bytes[9]);

			//    Object Id
			Assert.AreEqual((byte)ModbusDeviceIdentificationObject.ProductCode, bytes[10]);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void ShouldThrowOutOfRangeExceptionOnSerializeReadDeviceIdentification()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeReadDeviceIdentification(UNIT_ID, (ModbusDeviceIdentificationCategory)10, ModbusDeviceIdentificationObject.ProductCode);

			// Assert - ArgumentOutOfRangeException
		}

		[DataTestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void ShouldDeserializeReadDeviceIdentification(bool moreAndIndividual)
		{
			// Arrange
			byte[] response = [0x00, 0x01, 0x00, 0x00, 0x00, 0x0D, 0x2A, 0x2B, 0x0E, 0x02, (byte)(moreAndIndividual ? 0x82 : 0x02), (byte)(moreAndIndividual ? 0xFF : 0x00), (byte)(moreAndIndividual ? 0x05 : 0x00), 0x01, 0x04, 0x02, 0x41, 0x4D];
			var protocol = new TcpProtocol();

			// Act
			var result = protocol.DeserializeReadDeviceIdentification(response);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(moreAndIndividual, result.AllowsIndividualAccess);
			Assert.AreEqual(moreAndIndividual, result.MoreRequestsNeeded);
			Assert.AreEqual(moreAndIndividual ? 0x05 : 0x00, result.NextObjectIdToRequest);

			Assert.AreEqual(1, result.Objects.Count);
			Assert.AreEqual(4, result.Objects.First().Key);
			CollectionAssert.AreEqual("AM"u8.ToArray(), result.Objects.First().Value);
		}

		[TestMethod]
		[ExpectedException(typeof(ModbusException))]
		public void ShouldThrowExceptionOnDeserializeReadDeviceIdentificationForMeiType()
		{
			// Arrange
			byte[] response = [0x00, 0x01, 0x00, 0x00, 0x00, 0x0D, 0x2A, 0x2B, 0x0D];
			var protocol = new TcpProtocol();

			// Act
			protocol.DeserializeReadDeviceIdentification(response);
		}

		[TestMethod]
		[ExpectedException(typeof(ModbusException))]
		public void ShouldThrowExceptionOnDeserializeReadDeviceIdentificationForCategory()
		{
			// Arrange
			byte[] response = [0x00, 0x01, 0x00, 0x00, 0x00, 0x0D, 0x2A, 0x2B, 0x0E, 0x08];
			var protocol = new TcpProtocol();

			// Act
			protocol.DeserializeReadDeviceIdentification(response);
		}

		#endregion Read Device Identification

		#region Write Single Coil

		[TestMethod]
		public void ShouldSerializeWriteSingleCoil()
		{
			// Arrange
			var coil = new Coil { Address = 109, Value = true };
			var protocol = new TcpProtocol();

			// Act
			var result = protocol.SerializeWriteSingleCoil(UNIT_ID, coil);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(12, result.Count);

			//    Transaction id
			Assert.AreEqual(0x00, result[0]);
			Assert.AreEqual(0x01, result[1]);

			//    Protocol identifier
			Assert.AreEqual(0x00, result[2]);
			Assert.AreEqual(0x00, result[3]);

			//    Following bytes
			Assert.AreEqual(0x00, result[4]);
			Assert.AreEqual(0x06, result[5]);

			//    Unit id
			Assert.AreEqual(UNIT_ID, result[6]);

			//    Function code
			Assert.AreEqual(0x05, result[7]);

			//    Starting address
			Assert.AreEqual(0x00, result[8]);
			Assert.AreEqual(0x6D, result[9]);

			//    Value
			Assert.AreEqual(0xFF, result[10]);
			Assert.AreEqual(0x00, result[11]);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ShouldThrowArgumentNullOnSerializeWriteSingleCoil()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeWriteSingleCoil(UNIT_ID, null);

			// Assert - ArgumentNullException
		}

		[TestMethod]
		public void ShouldDeserializeWriteSingleCoil()
		{
			// Arrange
			byte[] bytes = [0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x05, 0x01, 0x0A, 0xFF, 0x00];
			var protocol = new TcpProtocol();

			// Act
			var coil = protocol.DeserializeWriteSingleCoil(bytes);

			// Assert
			Assert.IsNotNull(coil);
			Assert.AreEqual(266, coil.Address);
			Assert.IsTrue(coil.Value);
		}

		#endregion Write Single Coil

		#region Write Single Register

		[TestMethod]
		public void ShouldSerializeWriteSingleHoldingRegister()
		{
			// Arrange
			var register = new HoldingRegister { Address = 109, Value = 123 };
			var protocol = new TcpProtocol();

			// Act
			var result = protocol.SerializeWriteSingleHoldingRegister(UNIT_ID, register);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(12, result.Count);

			//    Transaction id
			Assert.AreEqual(0x00, result[0]);
			Assert.AreEqual(0x01, result[1]);

			//    Protocol identifier
			Assert.AreEqual(0x00, result[2]);
			Assert.AreEqual(0x00, result[3]);

			//    Following bytes
			Assert.AreEqual(0x00, result[4]);
			Assert.AreEqual(0x06, result[5]);

			//    Unit id
			Assert.AreEqual(UNIT_ID, result[6]);

			//    Function code
			Assert.AreEqual(0x06, result[7]);

			//    Starting address
			Assert.AreEqual(0x00, result[8]);
			Assert.AreEqual(0x6D, result[9]);

			//    Value
			Assert.AreEqual(0x00, result[10]);
			Assert.AreEqual(0x7B, result[11]);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ShouldThrowArgumentNullOnSerializeWriteSingleHoldingRegister()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeWriteSingleHoldingRegister(UNIT_ID, null);

			// Assert - ArgumentNullException
		}

		[TestMethod]
		public void ShouldDeserializeWriteSingleHoldingRegister()
		{
			// Arrange
			byte[] bytes = [0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x06, 0x02, 0x02, 0x01, 0x23];
			var protocol = new TcpProtocol();

			// Act
			var register = protocol.DeserializeWriteSingleHoldingRegister(bytes);

			// Assert
			Assert.IsNotNull(register);
			Assert.AreEqual(514, register.Address);
			Assert.AreEqual(291, register.Value);
		}

		#endregion Write Single Register

		#region Write Multiple Coils

		[TestMethod]
		public void ShouldSerializeWriteMultipleCoils()
		{
			// Arrange
			var coils = new Coil[]
			{
				new() { Address = 10, Value = true },
				new() { Address = 11, Value = false },
				new() { Address = 12, Value = true },
				new() { Address = 13, Value = false },
				new() { Address = 14, Value = true },
			};
			var protocol = new TcpProtocol();

			// Act
			var result = protocol.SerializeWriteMultipleCoils(UNIT_ID, coils);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(14, result.Count);

			//    Transaction id
			Assert.AreEqual(0x00, result[0]);
			Assert.AreEqual(0x01, result[1]);

			//    Protocol identifier
			Assert.AreEqual(0x00, result[2]);
			Assert.AreEqual(0x00, result[3]);

			//    Following bytes
			Assert.AreEqual(0x00, result[4]);
			Assert.AreEqual(0x08, result[5]);

			//    Unit id
			Assert.AreEqual(UNIT_ID, result[6]);

			//    Function code
			Assert.AreEqual(0x0F, result[7]);

			//    Starting address
			Assert.AreEqual(0x00, result[8]);
			Assert.AreEqual(0x0A, result[9]);

			//    Quantity
			Assert.AreEqual(0x00, result[10]);
			Assert.AreEqual(0x05, result[11]);

			//    Byte count
			Assert.AreEqual(0x01, result[12]);

			//    Values
			Assert.AreEqual(0x15, result[13]);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ShouldThrowArgumentNullOnSerializeWriteMultipleCoils()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeWriteMultipleCoils(UNIT_ID, null);

			// Assert - ArgumentNullException
		}

		[DataTestMethod]
		[DataRow(0)]
		[DataRow(1969)]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void ShouldThrowOutOfRangeForCountOnSerializeWriteMultipleCoils(int count)
		{
			// Arrange
			var coils = new List<Coil>();
			for (int i = 0; i < count; i++)
				coils.Add(new() { Address = (ushort)i });

			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeWriteMultipleCoils(UNIT_ID, coils);

			// Assert - ArgumentOutOfRangeException
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void ShouldThrowArgumentExceptionForDuplicateEntryOnSerializeMultipleCoils()
		{
			// Arrange
			var coils = new Coil[]
			{
				new() { Address = 10, Value = true },
				new() { Address = 10, Value = false },
			};
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeWriteMultipleCoils(UNIT_ID, coils);

			// Assert - ArgumentException
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void ShouldThrowArgumentExceptionForGapInAddressOnSerializeMultipleCoils()
		{
			// Arrange
			var coils = new Coil[]
			{
				new() { Address = 10, Value = true },
				new() { Address = 12, Value = false },
			};
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeWriteMultipleCoils(UNIT_ID, coils);

			// Assert - ArgumentException
		}

		[TestMethod]
		public void ShouldDeserializeWriteMultipleCoils()
		{
			// Arrange
			byte[] bytes = [0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x0F, 0x01, 0x0A, 0x00, 0x0B];
			var protocol = new TcpProtocol();

			// Act
			var (firstAddress, numberOfCoils) = protocol.DeserializeWriteMultipleCoils(bytes);

			// Assert
			Assert.AreEqual(266, firstAddress);
			Assert.AreEqual(11, numberOfCoils);
		}

		#endregion Write Multiple Coils

		#region Write Multiple Holding Registers

		[TestMethod]
		public void ShouldSerializeWriteMultipleHoldingRegisters()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 10, Value = 10 },
				new() { Address = 11, Value = 11 }
			};
			var protocol = new TcpProtocol();

			// Act
			var result = protocol.SerializeWriteMultipleHoldingRegisters(UNIT_ID, registers);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(17, result.Count);

			//    Transaction id
			Assert.AreEqual(0x00, result[0]);
			Assert.AreEqual(0x01, result[1]);

			//    Protocol identifier
			Assert.AreEqual(0x00, result[2]);
			Assert.AreEqual(0x00, result[3]);

			//    Following bytes
			Assert.AreEqual(0x00, result[4]);
			Assert.AreEqual(0x0B, result[5]);

			//    Unit id
			Assert.AreEqual(UNIT_ID, result[6]);

			//    Function code
			Assert.AreEqual(0x10, result[7]);

			//    Starting address
			Assert.AreEqual(0x00, result[8]);
			Assert.AreEqual(0x0A, result[9]);

			//    Quantity
			Assert.AreEqual(0x00, result[10]);
			Assert.AreEqual(0x02, result[11]);

			//    Byte count
			Assert.AreEqual(0x04, result[12]);

			//    Values
			Assert.AreEqual(0x00, result[13]);
			Assert.AreEqual(0x0A, result[14]);
			Assert.AreEqual(0x00, result[15]);
			Assert.AreEqual(0x0B, result[16]);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ShouldThrowArgumentNullOnSerializeWriteMultipleHoldingRegisters()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeWriteMultipleHoldingRegisters(UNIT_ID, null);

			// Assert - ArgumentNullException
		}

		[DataTestMethod]
		[DataRow(0)]
		[DataRow(124)]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void ShouldThrowOutOfRangeForCountOnSerializeWriteMultipleHoldingRegisters(int count)
		{
			// Arrange
			var registers = new List<HoldingRegister>();
			for (int i = 0; i < count; i++)
				registers.Add(new() { Address = (ushort)i });

			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeWriteMultipleHoldingRegisters(UNIT_ID, registers);

			// Assert - ArgumentOutOfRangeException
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void ShouldThrowArgumentExceptionForDuplicateEntryOnSerializeMultipleHoldingRegisters()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 10 },
				new() { Address = 10 },
			};
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeWriteMultipleHoldingRegisters(UNIT_ID, registers);

			// Assert - ArgumentException
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void ShouldThrowArgumentExceptionForGapInAddressOnSerializeMultipleHoldingRegisters()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 10 },
				new() { Address = 12 },
			};
			var protocol = new TcpProtocol();

			// Act
			protocol.SerializeWriteMultipleHoldingRegisters(UNIT_ID, registers);

			// Assert - ArgumentException
		}

		[TestMethod]
		public void ShouldDeserializeWriteMultipleHoldingRegisters()
		{
			// Arrange
			byte[] bytes = [0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x10, 0x02, 0x0A, 0x00, 0x0A];
			var protocol = new TcpProtocol();

			// Act
			var (firstAddress, numberOfCoils) = protocol.DeserializeWriteMultipleHoldingRegisters(bytes);

			// Assert
			Assert.AreEqual(522, firstAddress);
			Assert.AreEqual(10, numberOfCoils);
		}

		#endregion Write Multiple Holding Registers

		#region Validation

		[TestMethod]
		public void ShouldReturnFalseForMinLengthOnCheckResponseComplete()
		{
			// Arrange
			byte[] bytes = [0x00, 0x01, 0x00];
			var protocol = new TcpProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsFalse(complete);
		}

		[TestMethod]
		public void ShouldReturnFalseForFollowingBytesOnCheckResponseComplete()
		{
			// Arrange
			byte[] bytes = [0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x81];
			var protocol = new TcpProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsFalse(complete);
		}

		[TestMethod]
		public void ShouldReturnTrueOnCheckResponseComplete()
		{
			// Arrange
			byte[] bytes = [0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x81, 0x01];
			var protocol = new TcpProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsTrue(complete);
		}

		[TestMethod]
		public void ShouldValidateResponse()
		{
			// Arrange
			byte[] request = [0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x01, 0x00, 0x01, 0x00, 0x02];
			byte[] response = [0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x2A, 0x01, 0x01, 0x00];
			var protocol = new TcpProtocol();

			// Act
			protocol.ValidateResponse(request, response);
		}

		[TestMethod]
		public void ShouldValidateResponseIgnoringTransactionId()
		{
			// Arrange
			byte[] request = [0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x01, 0x00, 0x01, 0x00, 0x02];
			byte[] response = [0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x2A, 0x01, 0x01, 0x00];
			var protocol = new TcpProtocol { DisableTransactionId = true };

			// Act
			protocol.ValidateResponse(request, response);
		}

		[DataTestMethod]
		[DataRow(0x00, 0x00)]
		[DataRow(0x01, 0x01)]
		[ExpectedException(typeof(ModbusException))]
		public void ShouldThrowForTransactionIdOnValidateResponse(int hi, int lo)
		{
			// Arrange
			byte[] request = [(byte)hi, (byte)lo, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x01, 0x00, 0x01, 0x00, 0x02];
			byte[] response = [0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x2A, 0x01, 0x01, 0x00];
			var protocol = new TcpProtocol();

			// Act
			protocol.ValidateResponse(request, response);
		}

		[DataTestMethod]
		[DataRow(0x00, 0x01)]
		[DataRow(0x01, 0x00)]
		[ExpectedException(typeof(ModbusException))]
		public void ShouldThrowForProtocolIdOnValidateResponse(int hi, int lo)
		{
			// Arrange
			byte[] request = [0x00, 0x01, (byte)hi, (byte)lo, 0x00, 0x06, 0x2A, 0x01, 0x00, 0x01, 0x00, 0x02];
			byte[] response = [0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x2A, 0x01, 0x01, 0x00];
			var protocol = new TcpProtocol();

			// Act
			protocol.ValidateResponse(request, response);
		}

		[TestMethod]
		[ExpectedException(typeof(ModbusException))]
		public void ShouldThrowForFollowingBytesOnValidateResponse()
		{
			// Arrange
			byte[] request = [0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x01, 0x00, 0x01, 0x00, 0x02];
			byte[] response = [0x00, 0x01, 0x00, 0x00, 0x00, 0x05, 0x2A, 0x01, 0x01, 0x00];
			var protocol = new TcpProtocol();

			// Act
			protocol.ValidateResponse(request, response);
		}

		[TestMethod]
		[ExpectedException(typeof(ModbusException))]
		public void ShouldThrowForUnitIdOnValidateResponse()
		{
			// Arrange
			byte[] request = [0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x01, 0x00, 0x01, 0x00, 0x02];
			byte[] response = [0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x2B, 0x01, 0x01, 0x00];
			var protocol = new TcpProtocol();

			// Act
			protocol.ValidateResponse(request, response);
		}

		[TestMethod]
		[ExpectedException(typeof(ModbusException))]
		public void ShouldThrowForFunctionCodeOnValidateResponse()
		{
			// Arrange
			byte[] request = [0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x01, 0x00, 0x01, 0x00, 0x02];
			byte[] response = [0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x2A, 0x02, 0x01, 0x00];
			var protocol = new TcpProtocol();

			// Act
			protocol.ValidateResponse(request, response);
		}

		[TestMethod]
		[ExpectedException(typeof(ModbusException))]
		public void ShouldThrowForModbusErrorOnValidateResponse()
		{
			// Arrange
			byte[] request = [0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x2A, 0x01, 0x00, 0x01, 0x00, 0x02];
			byte[] response = [0x00, 0x01, 0x00, 0x00, 0x00, 0x03, 0x2A, 0x81, 0x01];
			var protocol = new TcpProtocol();

			// Act
			protocol.ValidateResponse(request, response);
		}

		#endregion Validation

		#region Helper

		[TestMethod]
		public void ShouldIncreaseTransactionId()
		{
			// Arrange
			var list = new List<byte[]>();
			var protocol = new TcpProtocol();

			// Act
			list.Add(protocol.SerializeReadCoils(UNIT_ID, 10, 10).ToArray());
			list.Add(protocol.SerializeReadCoils(UNIT_ID, 10, 10).ToArray());
			list.Add(protocol.SerializeReadCoils(UNIT_ID, 10, 10).ToArray());

			// Assert
			for (int i = 0; i < list.Count; i++)
			{
				Assert.AreEqual(0x00, list[i][0]);
				Assert.AreEqual((byte)(i + 1), list[i][1]);

				// Other asserts already done
			}
		}

		[TestMethod]
		public void ShouldNotIncreaseTransactionId()
		{
			// Arrange
			var list = new List<byte[]>();
			var protocol = new TcpProtocol { DisableTransactionId = true };

			// Act
			list.Add(protocol.SerializeReadCoils(UNIT_ID, 10, 10).ToArray());
			list.Add(protocol.SerializeReadCoils(UNIT_ID, 10, 10).ToArray());
			list.Add(protocol.SerializeReadCoils(UNIT_ID, 10, 10).ToArray());

			// Assert
			for (int i = 0; i < list.Count; i++)
			{
				Assert.AreEqual(0x00, list[i][0]);
				Assert.AreEqual(0x00, list[i][1]);

				// Other asserts already done
			}
		}

		[TestMethod]
		public void ShouldResetTransactionIdOnMaxValue()
		{
			// Arrange
			var protocol = new TcpProtocol();
			protocol.GetType()
				.GetField("_transactionId", BindingFlags.NonPublic | BindingFlags.Instance)
				.SetValue(protocol, (ushort)(ushort.MaxValue - 1));

			// Act
			var result1 = protocol.SerializeReadCoils(UNIT_ID, 10, 10);
			var result2 = protocol.SerializeReadCoils(UNIT_ID, 10, 10);

			// Assert
			Assert.AreEqual(0xFF, result1[0]);
			Assert.AreEqual(0xFF, result1[1]);

			Assert.AreEqual(0x00, result2[0]);
			Assert.AreEqual(0x00, result2[1]);
		}

		[TestMethod]
		public void ShouldNameTcp()
		{
			// Arrange
			var protocol = new TcpProtocol();

			// Act
			string result = protocol.Name;

			// Assert
			Assert.AreEqual("TCP", result);
		}

		#endregion Helper
	}
}
