using System.Collections.Generic;
using System.Text;
using AMWD.Protocols.Modbus.Common.Protocols;

namespace AMWD.Protocols.Modbus.Tests.Common.Protocols
{
	[TestClass]
	public class RtuProtocolTest
	{
		private const byte UNIT_ID = 0x2A; // 42

		#region Read Coils

		[TestMethod]
		public void ShouldSerializeReadCoils()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act
			var bytes = protocol.SerializeReadCoils(UNIT_ID, 19, 19);

			// Assert
			Assert.IsNotNull(bytes);
			Assert.AreEqual(8, bytes.Count);

			//    Unit id
			Assert.AreEqual(UNIT_ID, bytes[0]);

			//    Function code
			Assert.AreEqual(0x01, bytes[1]);

			//    Starting address
			Assert.AreEqual(0x00, bytes[2]);
			Assert.AreEqual(0x13, bytes[3]);
			//    Quantity
			Assert.AreEqual(0x00, bytes[4]);
			Assert.AreEqual(0x13, bytes[5]);

			// CRC check will be ignored
		}

		[DataTestMethod]
		[DataRow(0)]
		[DataRow(2001)]
		public void ShouldThrowOutOfRangeForCountOnSerializeReadCoils(int count)
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => protocol.SerializeReadCoils(UNIT_ID, 19, (ushort)count));
		}

		[TestMethod]
		public void ShouldThrowOutOfRangeForStartingAddressOnSerializeReadCoils()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => protocol.SerializeReadCoils(UNIT_ID, ushort.MaxValue, 2));
		}

		[TestMethod]
		public void ShouldDeserializeReadCoils()
		{
			// Arrange
			int[] setValues = [0, 2, 3, 6, 7, 8, 9, 11, 13, 14, 16, 18];
			var protocol = new RtuProtocol();

			// Act
			var coils = protocol.DeserializeReadCoils([UNIT_ID, 0x01, 0x03, 0xCD, 0x6B, 0x05, 0x00, 0x00]);

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
		public void ShouldThrowExceptionOnDeserializeReadCoils()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ModbusException>(() => protocol.DeserializeReadCoils([UNIT_ID, 0x01, 0x02, 0xCD, 0x6B, 0x05, 0x00, 0x00]));
		}

		#endregion Read Coils

		#region Read Discrete Inputs

		[TestMethod]
		public void ShouldSerializeReadDiscreteInputs()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act
			var bytes = protocol.SerializeReadDiscreteInputs(UNIT_ID, 19, 19);

			// Assert
			Assert.IsNotNull(bytes);
			Assert.AreEqual(8, bytes.Count);

			//    Unit id
			Assert.AreEqual(UNIT_ID, bytes[0]);

			//    Function code
			Assert.AreEqual(0x02, bytes[1]);

			//    Starting address
			Assert.AreEqual(0x00, bytes[2]);
			Assert.AreEqual(0x13, bytes[3]);
			//    Quantity
			Assert.AreEqual(0x00, bytes[4]);
			Assert.AreEqual(0x13, bytes[5]);

			// CRC check will be ignored
		}

		[DataTestMethod]
		[DataRow(0)]
		[DataRow(2001)]
		public void ShouldThrowOutOfRangeForCountOnSerializeReadDiscreteInputs(int count)
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => protocol.SerializeReadDiscreteInputs(UNIT_ID, 19, (ushort)count));
		}

		[TestMethod]
		public void ShouldThrowOutOfRangeForStartingAddressOnSerializeReadDiscreteInputs()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => protocol.SerializeReadDiscreteInputs(UNIT_ID, ushort.MaxValue, 2));
		}

		[TestMethod]
		public void ShouldDeserializeReadDiscreteInputs()
		{
			// Arrange
			int[] setValues = [0, 2, 3, 6, 7, 8, 9, 11, 13, 14, 16, 18];
			var protocol = new RtuProtocol();

			// Act
			var coils = protocol.DeserializeReadDiscreteInputs([UNIT_ID, 0x02, 0x03, 0xCD, 0x6B, 0x05, 0x00, 0x00]);

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
		public void ShouldThrowExceptionOnDeserializeReadDiscreteInputs()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ModbusException>(() => protocol.DeserializeReadDiscreteInputs([UNIT_ID, 0x02, 0x02, 0xCD, 0x6B, 0x05, 0x00, 0x00]));
		}

		#endregion Read Discrete Inputs

		#region Read Holding Registers

		[TestMethod]
		public void ShouldSerializeReadHoldingRegisters()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act
			var bytes = protocol.SerializeReadHoldingRegisters(UNIT_ID, 107, 2);

			// Assert
			Assert.IsNotNull(bytes);
			Assert.AreEqual(8, bytes.Count);

			//    Unit id
			Assert.AreEqual(UNIT_ID, bytes[0]);

			//    Function code
			Assert.AreEqual(0x03, bytes[1]);

			//    Starting address
			Assert.AreEqual(0x00, bytes[2]);
			Assert.AreEqual(0x6B, bytes[3]);
			//    Quantity
			Assert.AreEqual(0x00, bytes[4]);
			Assert.AreEqual(0x02, bytes[5]);

			// CRC check will be ignored
		}

		[DataTestMethod]
		[DataRow(0)]
		[DataRow(126)]
		public void ShouldThrowOutOfRangeForCountOnSerializeReadHoldingRegisters(int count)
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => protocol.SerializeReadHoldingRegisters(UNIT_ID, 19, (ushort)count));
		}

		[TestMethod]
		public void ShouldThrowOutOfRangeForStartingAddressOnSerializeReadHoldingRegisters()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => protocol.SerializeReadHoldingRegisters(UNIT_ID, ushort.MaxValue, 2));
		}

		[TestMethod]
		public void ShouldDeserializeReadHoldingRegisters()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act
			var registers = protocol.DeserializeReadHoldingRegisters([UNIT_ID, 0x03, 0x04, 0x02, 0x2B, 0x00, 0x64, 0x00, 0x00]);

			// Assert
			Assert.IsNotNull(registers);
			Assert.AreEqual(2, registers.Count);

			Assert.AreEqual(0, registers[0].Address);
			Assert.AreEqual(555, registers[0].Value);

			Assert.AreEqual(1, registers[1].Address);
			Assert.AreEqual(100, registers[1].Value);
		}

		[TestMethod]
		public void ShouldThrowExceptionOnDeserializeReadHoldingRegisters()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ModbusException>(() => protocol.DeserializeReadHoldingRegisters([UNIT_ID, 0x03, 0x04, 0x02, 0x2B, 0x00, 0x00]));
		}

		#endregion Read Holding Registers

		#region Read Input Registers

		[TestMethod]
		public void ShouldSerializeReadInputRegisters()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act
			var bytes = protocol.SerializeReadInputRegisters(UNIT_ID, 107, 2);

			// Assert
			Assert.IsNotNull(bytes);
			Assert.AreEqual(8, bytes.Count);

			//    Unit id
			Assert.AreEqual(UNIT_ID, bytes[0]);

			//    Function code
			Assert.AreEqual(0x04, bytes[1]);

			//    Starting address
			Assert.AreEqual(0x00, bytes[2]);
			Assert.AreEqual(0x6B, bytes[3]);
			//    Quantity
			Assert.AreEqual(0x00, bytes[4]);
			Assert.AreEqual(0x02, bytes[5]);

			// CRC check will be ignored
		}

		[DataTestMethod]
		[DataRow(0)]
		[DataRow(126)]
		public void ShouldThrowOutOfRangeForCountOnSerializeReadInputRegisters(int count)
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => protocol.SerializeReadInputRegisters(UNIT_ID, 19, (ushort)count));
		}

		[TestMethod]
		public void ShouldThrowOutOfRangeForStartingAddressOnSerializeReadInputRegisters()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => protocol.SerializeReadInputRegisters(UNIT_ID, ushort.MaxValue, 2));
		}

		[TestMethod]
		public void ShouldDeserializeReadInputRegisters()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act
			var registers = protocol.DeserializeReadInputRegisters([UNIT_ID, 0x04, 0x04, 0x02, 0x2B, 0x00, 0x64, 0x00, 0x00]);

			// Assert
			Assert.IsNotNull(registers);
			Assert.AreEqual(2, registers.Count);

			Assert.AreEqual(0, registers[0].Address);
			Assert.AreEqual(555, registers[0].Value);

			Assert.AreEqual(1, registers[1].Address);
			Assert.AreEqual(100, registers[1].Value);
		}

		[TestMethod]
		public void ShouldThrowExceptionOnDeserializeReadInputRegisters()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ModbusException>(() => protocol.DeserializeReadInputRegisters([UNIT_ID, 0x04, 0x04, 0x02, 0x2B, 0x00, 0x00]));
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
			var protocol = new RtuProtocol();

			// Act
			var bytes = protocol.SerializeReadDeviceIdentification(UNIT_ID, category, ModbusDeviceIdentificationObject.ProductCode);

			// Assert
			Assert.IsNotNull(bytes);
			Assert.AreEqual(7, bytes.Count);

			//    Unit id
			Assert.AreEqual(UNIT_ID, bytes[0]);

			//    Function code
			Assert.AreEqual(0x2B, bytes[1]);

			//    MEI Type
			Assert.AreEqual(0x0E, bytes[2]);

			//    Category
			Assert.AreEqual((byte)category, bytes[3]);

			//    Object Id
			Assert.AreEqual((byte)ModbusDeviceIdentificationObject.ProductCode, bytes[4]);

			// CRC check will be ignored
		}

		[TestMethod]
		public void ShouldThrowOutOfRangeExceptionForCategoryOnSerializeReadDeviceIdentification()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => protocol.SerializeReadDeviceIdentification(UNIT_ID, (ModbusDeviceIdentificationCategory)10, ModbusDeviceIdentificationObject.ProductCode));
		}

		[DataTestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void ShouldDeserializeReadDeviceIdentification(bool moreAndIndividual)
		{
			// Arrange
			byte[] response = [UNIT_ID, 0x2B, 0x0E, 0x02, (byte)(moreAndIndividual ? 0x82 : 0x02), (byte)(moreAndIndividual ? 0xFF : 0x00), (byte)(moreAndIndividual ? 0x05 : 0x00), 0x01, 0x04, 0x02, 0x41, 0x4D, 0x00, 0x00];
			var protocol = new RtuProtocol();

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
		public void ShouldThrowExceptionOnDeserializeReadDeviceIdentificationForMeiType()
		{
			// Arrange
			byte[] response = [UNIT_ID, 0x2B, 0x0D, 0x00, 0x00];
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ModbusException>(() => protocol.DeserializeReadDeviceIdentification(response));
		}

		[TestMethod]
		public void ShouldThrowExceptionOnDeserializeReadDeviceIdentificationForCategory()
		{
			// Arrange
			byte[] response = [UNIT_ID, 0x2B, 0x0E, 0x08, 0x00, 0x00];
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ModbusException>(() => protocol.DeserializeReadDeviceIdentification(response));
		}

		#endregion Read Device Identification

		#region Write Single Coil

		[TestMethod]
		public void ShouldSerializeWriteSingleCoil()
		{
			// Arrange
			var coil = new Coil { Address = 109, Value = true };
			var protocol = new RtuProtocol();

			// Act
			var result = protocol.SerializeWriteSingleCoil(UNIT_ID, coil);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(8, result.Count);

			//    Unit id
			Assert.AreEqual(UNIT_ID, result[0]);

			//    Function code
			Assert.AreEqual(0x05, result[1]);

			//    Starting address
			Assert.AreEqual(0x00, result[2]);
			Assert.AreEqual(0x6D, result[3]);

			//    Value
			Assert.AreEqual(0xFF, result[4]);
			Assert.AreEqual(0x00, result[5]);

			// CRC check will be ignored
		}

		[TestMethod]
		public void ShouldThrowArgumentNullOnSerializeWriteSingleCoil()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentNullException>(() => protocol.SerializeWriteSingleCoil(UNIT_ID, null));
		}

		[TestMethod]
		public void ShouldDeserializeWriteSingleCoil()
		{
			// Arrange
			byte[] bytes = [UNIT_ID, 0x05, 0x01, 0x0A, 0xFF, 0x00, 0x00, 0x00];
			var protocol = new RtuProtocol();

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
			var protocol = new RtuProtocol();

			// Act
			var result = protocol.SerializeWriteSingleHoldingRegister(UNIT_ID, register);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(8, result.Count);

			//    Unit id
			Assert.AreEqual(UNIT_ID, result[0]);

			//    Function code
			Assert.AreEqual(0x06, result[1]);

			//    Starting address
			Assert.AreEqual(0x00, result[2]);
			Assert.AreEqual(0x6D, result[3]);

			//    Value
			Assert.AreEqual(0x00, result[4]);
			Assert.AreEqual(0x7B, result[5]);

			// CRC check will be ignored
		}

		[TestMethod]
		public void ShouldThrowArgumentNullOnSerializeWriteSingleHoldingRegister()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentNullException>(() => protocol.SerializeWriteSingleHoldingRegister(UNIT_ID, null));
		}

		[TestMethod]
		public void ShouldDeserializeWriteSingleHoldingRegister()
		{
			// Arrange
			byte[] bytes = [UNIT_ID, 0x06, 0x02, 0x02, 0x01, 0x23, 0x00, 0x00];
			var protocol = new RtuProtocol();

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
			var protocol = new RtuProtocol();

			// Act
			var result = protocol.SerializeWriteMultipleCoils(UNIT_ID, coils);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(10, result.Count);

			//    Unit id
			Assert.AreEqual(UNIT_ID, result[0]);

			//    Function code
			Assert.AreEqual(0x0F, result[1]);

			//    Starting address
			Assert.AreEqual(0x00, result[2]);
			Assert.AreEqual(0x0A, result[3]);

			//    Quantity
			Assert.AreEqual(0x00, result[4]);
			Assert.AreEqual(0x05, result[5]);

			//    Byte count
			Assert.AreEqual(0x01, result[6]);

			//    Values
			Assert.AreEqual(0x15, result[7]);

			// CRC check will be ignored
		}

		[TestMethod]
		public void ShouldThrowArgumentNullOnSerializeWriteMultipleCoils()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentNullException>(() => protocol.SerializeWriteMultipleCoils(UNIT_ID, null));
		}

		[DataTestMethod]
		[DataRow(0)]
		[DataRow(1969)]
		public void ShouldThrowOutOfRangeForCountOnSerializeWriteMultipleCoils(int count)
		{
			// Arrange
			var coils = new List<Coil>();
			for (int i = 0; i < count; i++)
				coils.Add(new() { Address = (ushort)i });

			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => protocol.SerializeWriteMultipleCoils(UNIT_ID, coils));
		}

		[TestMethod]
		public void ShouldThrowArgumentExceptionForDuplicateEntryOnSerializeMultipleCoils()
		{
			// Arrange
			var coils = new Coil[]
			{
				new() { Address = 10, Value = true },
				new() { Address = 10, Value = false },
			};
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentException>(() => protocol.SerializeWriteMultipleCoils(UNIT_ID, coils));
		}

		[TestMethod]
		public void ShouldThrowArgumentExceptionForGapInAddressOnSerializeMultipleCoils()
		{
			// Arrange
			var coils = new Coil[]
			{
				new() { Address = 10, Value = true },
				new() { Address = 12, Value = false },
			};
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentException>(() => protocol.SerializeWriteMultipleCoils(UNIT_ID, coils));
		}

		[TestMethod]
		public void ShouldDeserializeWriteMultipleCoils()
		{
			// Arrange
			byte[] bytes = [UNIT_ID, 0x0F, 0x01, 0x0A, 0x00, 0x0B, 0x00, 0x00];
			var protocol = new RtuProtocol();

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
			var protocol = new RtuProtocol();

			// Act
			var result = protocol.SerializeWriteMultipleHoldingRegisters(UNIT_ID, registers);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(13, result.Count);

			//    Unit id
			Assert.AreEqual(UNIT_ID, result[0]);

			//    Function code
			Assert.AreEqual(0x10, result[1]);

			//    Starting address
			Assert.AreEqual(0x00, result[2]);
			Assert.AreEqual(0x0A, result[3]);

			//    Quantity
			Assert.AreEqual(0x00, result[4]);
			Assert.AreEqual(0x02, result[5]);

			//    Byte count
			Assert.AreEqual(0x04, result[6]);

			//    Values
			Assert.AreEqual(0x00, result[7]);
			Assert.AreEqual(0x0A, result[8]);
			Assert.AreEqual(0x00, result[9]);
			Assert.AreEqual(0x0B, result[10]);

			// CRC check will be ignored
		}

		[TestMethod]
		public void ShouldThrowArgumentNullOnSerializeWriteMultipleHoldingRegisters()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentNullException>(() => protocol.SerializeWriteMultipleHoldingRegisters(UNIT_ID, null));
		}

		[DataTestMethod]
		[DataRow(0)]
		[DataRow(124)]
		public void ShouldThrowOutOfRangeForCountOnSerializeWriteMultipleHoldingRegisters(int count)
		{
			// Arrange
			var registers = new List<HoldingRegister>();
			for (int i = 0; i < count; i++)
				registers.Add(new() { Address = (ushort)i });

			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => protocol.SerializeWriteMultipleHoldingRegisters(UNIT_ID, registers));
		}

		[TestMethod]
		public void ShouldThrowArgumentExceptionForDuplicateEntryOnSerializeMultipleHoldingRegisters()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 10 },
				new() { Address = 10 },
			};
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentException>(() => protocol.SerializeWriteMultipleHoldingRegisters(UNIT_ID, registers));
		}

		[TestMethod]
		public void ShouldThrowArgumentExceptionForGapInAddressOnSerializeMultipleHoldingRegisters()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 10 },
				new() { Address = 12 },
			};
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ArgumentException>(() => protocol.SerializeWriteMultipleHoldingRegisters(UNIT_ID, registers));
		}

		[TestMethod]
		public void ShouldDeserializeWriteMultipleHoldingRegisters()
		{
			// Arrange
			byte[] bytes = [UNIT_ID, 0x10, 0x02, 0x0A, 0x00, 0x0A, 0x00, 0x00];
			var protocol = new RtuProtocol();

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
			byte[] bytes = [UNIT_ID, 0x01];
			var protocol = new RtuProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsFalse(complete);
		}

		[TestMethod]
		public void ShouldReturnFalseForErrorOnCheckResponseComplete()
		{
			// Arrange
			byte[] bytes = [UNIT_ID, 0x81, 0x01, 0x00];
			var protocol = new RtuProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsFalse(complete);
		}

		[TestMethod]
		public void ShouldReturnTrueForErrorOnCheckResponseComplete()
		{
			// Arrange
			byte[] bytes = [UNIT_ID, 0x81, 0x01, 0x00, 0x00];
			var protocol = new RtuProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsTrue(complete);
		}

		[DataTestMethod]
		[DataRow(0x01)] // Read Coils
		[DataRow(0x02)] // Read Discrete Inputs
		[DataRow(0x03)] // Read Holding Registers
		[DataRow(0x04)] // Read Input Registers
		public void ShouldReturnFalseForMissingBytesOnReadFunctionsOnCheckResponseComplete(int functionCode)
		{
			// Arrange
			byte[] bytes = [UNIT_ID, (byte)functionCode, 0x01, 0x00, 0x00];
			var protocol = new RtuProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsFalse(complete);
		}

		[DataTestMethod]
		[DataRow(0x01)] // Read Coils
		[DataRow(0x02)] // Read Discrete Inputs
		[DataRow(0x03)] // Read Holding Registers
		[DataRow(0x04)] // Read Input Registers
		public void ShouldReturnTrueOnReadFunctionsOnCheckResponseComplete(int functionCode)
		{
			// Arrange
			byte[] bytes = [UNIT_ID, (byte)functionCode, 0x01, 0x00, 0x12, 0x34];
			var protocol = new RtuProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsTrue(complete);
		}

		[DataTestMethod]
		[DataRow(0x05)] // Write Single Coil
		[DataRow(0x06)] // Write Single Register
		[DataRow(0x0F)] // Write Multiple Coils
		[DataRow(0x10)] // Write Multiple Registers
		public void ShouldReturnFalseForMissingBytesOnWriteFunctionsOnCheckResponseComplete(int functionCode)
		{
			// Arrange
			byte[] bytes = [UNIT_ID, (byte)functionCode, 0x00, 0x10, 0xFF, 0x00, 0x00];
			var protocol = new RtuProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsFalse(complete);
		}

		[DataTestMethod]
		[DataRow(0x05)] // Write Single Coil
		[DataRow(0x06)] // Write Single Register
		[DataRow(0x0F)] // Write Multiple Coils
		[DataRow(0x10)] // Write Multiple Registers
		public void ShouldReturnTrueOnWriteFunctionsOnCheckResponseComplete(int functionCode)
		{
			// Arrange
			byte[] bytes = [UNIT_ID, (byte)functionCode, 0x00, 0x10, 0xFF, 0x00, 0x12, 0x34];
			var protocol = new RtuProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsTrue(complete);
		}

		[TestMethod]
		public void ShouldReturnFalseForMissingBytesOnReadDeviceIdentificationOnCheckResponseComplete()
		{
			// Arrange
			byte[] bytes = [UNIT_ID, 0x2B, 0x0E, 0x01, 0x81, 0x00, 0x00];
			var protocol = new RtuProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsFalse(complete);
		}

		[TestMethod]
		public void ShouldReturnFalseForMissingCrcOnReadDeviceIdentificationOnCheckResponseComplete()
		{
			// Arrange
			byte[] bytes = [UNIT_ID, 0x2B, 0x0E, 0x01, 0x81, 0x00, 0x00, 0x00];
			var protocol = new RtuProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsFalse(complete);
		}

		[TestMethod]
		public void ShouldReturnTrueOnReadDeviceIdentificationForZeroObjectsOnCheckResponseComplete()
		{
			// Arrange
			byte[] bytes = [UNIT_ID, 0x2B, 0x0E, 0x01, 0x81, 0x00, 0x00, 0x00, 0x12, 0x34];
			var protocol = new RtuProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsTrue(complete);
		}

		[TestMethod]
		public void ShouldReturnFalseOnMissingBytesForDeviceIdentificationOnCheckResponseComplete()
		{
			// Arrange
			byte[] bytes = [UNIT_ID, 0x2B, 0x0E, 0x01, 0x81, 0x00, 0x00, 0x02, 0x00, 0x02, 0x55, 0x66];
			var protocol = new RtuProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsFalse(complete);
		}

		[TestMethod]
		public void ShouldReturnTrueForDeviceIdentificationOnCheckResponseComplete()
		{
			// Arrange
			byte[] bytes = [UNIT_ID, 0x2B, 0x0E, 0x01, 0x81, 0x00, 0x00, 0x02, 0x00, 0x02, 0x55, 0x66, 0x01, 0x01, 0x77, 0x12, 0x34];
			var protocol = new RtuProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsTrue(complete);
		}

		[DataTestMethod]
		[DataRow(0x01)]
		[DataRow(0x02)]
		[DataRow(0x03)]
		[DataRow(0x04)]
		public void ShouldValidateReadResponse(int fn)
		{
			// Arrange
			byte[] request = [UNIT_ID, (byte)fn, 0x00, 0x01, 0x00, 0x02]; // CRC missing, OK
			byte[] response = [UNIT_ID, (byte)fn, 0x01, 0x00, 0x00, 0x00];
			SetCrc(response);
			var protocol = new RtuProtocol();

			// Act
			protocol.ValidateResponse(request, response);
		}

		[DataTestMethod]
		[DataRow(0x05)]
		[DataRow(0x06)]
		[DataRow(0x0F)]
		[DataRow(0x10)]
		public void ShouldValidateWriteResponse(int fn)
		{
			// Arrange
			byte[] request = [UNIT_ID, (byte)fn, 0x00, 0x01, 0xFF, 0x00]; // CRC missing, OK
			byte[] response = [UNIT_ID, (byte)fn, 0x00, 0x01, 0xFF, 0x00, 0x00, 0x00];
			SetCrc(response);
			var protocol = new RtuProtocol();

			// Act
			protocol.ValidateResponse(request, response);
		}

		[TestMethod]
		public void ShouldThrowForUnitIdOnValidateResponse()
		{
			// Arrange
			byte[] request = [UNIT_ID, 0x01, 0x00, 0x01, 0x00, 0x02]; // CRC missing, OK
			byte[] response = [UNIT_ID + 1, 0x01, 0x01, 0x00, 0x00, 0x00];
			SetCrc(response);
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ModbusException>(() => protocol.ValidateResponse(request, response));
		}

		[DataTestMethod]
		[DataRow(0x57, 0x6C)]
		[DataRow(0x58, 0x6B)]
		public void ShouldThrowForCrcOnValidateResponse(int hi, int lo)
		{
			// Arrange
			byte[] request = [UNIT_ID, 0x01, 0x00, 0x01, 0x00, 0x02]; // CRC missing, OK
			byte[] response = [UNIT_ID, 0x01, 0x01, 0x00, (byte)hi, (byte)lo];
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ModbusException>(() => protocol.ValidateResponse(request, response));
		}

		[TestMethod]
		public void ShouldThrowForFunctionCodeOnValidateResponse()
		{
			// Arrange
			byte[] request = [UNIT_ID, 0x01, 0x00, 0x01, 0x00, 0x02]; // CRC missing, OK
			byte[] response = [UNIT_ID, 0x02, 0x01, 0x00, 0x00, 0x00];
			SetCrc(response);
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ModbusException>(() => protocol.ValidateResponse(request, response));
		}

		[TestMethod]
		public void ShouldThrowForErrorOnValidateResponse()
		{
			// Arrange
			byte[] request = [UNIT_ID, 0x01, 0x00, 0x01, 0x00, 0x02]; // CRC missing, OK
			byte[] response = [UNIT_ID, 0x81, 0x01, 0x00, 0x00];
			SetCrc(response);
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ModbusException>(() => protocol.ValidateResponse(request, response));
		}

		[DataTestMethod]
		[DataRow(0x01)]
		[DataRow(0x02)]
		[DataRow(0x03)]
		[DataRow(0x04)]
		public void ShouldThrowForReadLengthOnValidateResponse(int fn)
		{
			// Arrange
			byte[] request = [UNIT_ID, (byte)fn, 0x00, 0x01, 0x00, 0x02]; // CRC missing, OK
			byte[] response = [UNIT_ID, (byte)fn, 0xFF, 0x00, 0x00, 0x00, 0x00];
			SetCrc(response);
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ModbusException>(() => protocol.ValidateResponse(request, response));
		}

		[DataTestMethod]
		[DataRow(0x05)]
		[DataRow(0x06)]
		[DataRow(0x0F)]
		[DataRow(0x10)]
		public void ShouldThrowForWriteLengthOnValidateResponse(int fn)
		{
			// Arrange
			byte[] request = [UNIT_ID, (byte)fn, 0x00, 0x01, 0x00, 0x02]; // CRC missing, OK
			byte[] response = [UNIT_ID, (byte)fn, 0x00, 0x13, 0x00, 0x02, 0x00, 0x00, 0x00];
			SetCrc(response);
			var protocol = new RtuProtocol();

			// Act + Assert
			Assert.ThrowsException<ModbusException>(() => protocol.ValidateResponse(request, response));
		}

		[TestMethod]
		public void ShouldReturnValidCrc16()
		{
			// This is the example of the spec, page 41.

			// Arrange
			byte[] bytes = [0x02, 0x07];

			// Act
			byte[] crc = RtuProtocol.CRC16(bytes);

			// Assert
			Assert.AreEqual(2, crc.Length);
			Assert.AreEqual(0x41, crc[0]);
			Assert.AreEqual(0x12, crc[1]);
		}

		[DataTestMethod]
		[DataRow(null)]
		[DataRow(new byte[0])]
		public void ShuldThrowArgumentNullExceptionForBytesOnCrc16(byte[] bytes)
		{
			// Arrange

			// Act + Assert
			Assert.ThrowsException<ArgumentNullException>(() => RtuProtocol.CRC16(bytes));
		}

		[DataTestMethod]
		[DataRow(-1)]
		[DataRow(10)]
		public void ShouldThrowArgumentOutOfRangeForStartOnCrc16(int start)
		{
			// Arrange
			byte[] bytes = Encoding.UTF8.GetBytes("0123456789");

			// Act + Assert
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => RtuProtocol.CRC16(bytes, start));
		}

		[DataTestMethod]
		[DataRow(0)]
		[DataRow(11)]
		public void ShouldThrowArgumentOutOfRangeForLengthOnCrc16(int length)
		{
			// Arrange
			byte[] bytes = Encoding.UTF8.GetBytes("0123456789");

			// Act + Assert
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => RtuProtocol.CRC16(bytes, 0, length));
		}

		#endregion Validation

		[TestMethod]
		public void ShouldNameRtu()
		{
			// Arrange
			var protocol = new RtuProtocol();

			// Act
			string result = protocol.Name;

			// Assert
			Assert.AreEqual("RTU", result);
		}

		private static void SetCrc(byte[] bytes)
		{
			byte[] crc = RtuProtocol.CRC16(bytes, 0, bytes.Length - 2);
			bytes[^2] = crc[0];
			bytes[^1] = crc[1];
		}
	}
}
