using System.Text;
using AMWD.Protocols.Modbus.Common.Protocols;

namespace AMWD.Protocols.Modbus.Tests.Common.Protocols
{
	[TestClass]
	public class AsciiProtocolTest
	{
		private const byte UNIT_ID = 0x2A; // 42

		#region Read Coils

		[TestMethod]
		public void ShouldSerializeReadCoils()
		{
			// Arrange
			string expectedResponse = $":{UNIT_ID:X2}0100130013";
			AddTrailer(ref expectedResponse);
			byte[] expectedBytes = Encoding.ASCII.GetBytes(expectedResponse);

			var protocol = new AsciiProtocol();

			// Act
			var bytes = protocol.SerializeReadCoils(UNIT_ID, 19, 19);

			// Assert
			Assert.IsNotNull(bytes);
			CollectionAssert.AreEqual(expectedBytes, bytes.ToArray());
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(2001)]
		public void ShouldThrowOutOfRangeForCountOnSerializeReadCoils(int count)
		{
			// Arrange
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => protocol.SerializeReadCoils(UNIT_ID, 19, (ushort)count));
		}

		[TestMethod]
		public void ShouldThrowOutOfRangeForStartingAddressOnSerializeReadCoils()
		{
			// Arrange
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => protocol.SerializeReadCoils(UNIT_ID, ushort.MaxValue, 2));
		}

		[TestMethod]
		public void ShouldDeserializeReadCoils()
		{
			// Arrange
			int[] setValues = [0, 2, 3, 6, 7, 8, 9, 11, 13, 14, 16, 18];

			string response = $":{UNIT_ID:X2}0103CD6B05";
			AddTrailer(ref response);
			byte[] responseBytes = Encoding.ASCII.GetBytes(response);

			var protocol = new AsciiProtocol();

			// Act
			var coils = protocol.DeserializeReadCoils(responseBytes);

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
			string response = $":{UNIT_ID:X2}0102CD6B05";
			AddTrailer(ref response);
			byte[] responseBytes = Encoding.ASCII.GetBytes(response);

			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ModbusException>(() => protocol.DeserializeReadCoils(responseBytes));
		}

		#endregion Read Coils

		#region Read Discrete Inputs

		[TestMethod]
		public void ShouldSerializeReadDiscreteInputs()
		{
			// Arrange
			string expectedResponse = $":{UNIT_ID:X2}0200130013";
			AddTrailer(ref expectedResponse);
			byte[] expectedBytes = Encoding.ASCII.GetBytes(expectedResponse);

			var protocol = new AsciiProtocol();

			// Act
			var bytes = protocol.SerializeReadDiscreteInputs(UNIT_ID, 19, 19);

			// Assert
			Assert.IsNotNull(bytes);
			CollectionAssert.AreEqual(expectedBytes, bytes.ToArray());
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(2001)]
		public void ShouldThrowOutOfRangeForCountOnSerializeReadDiscreteInputs(int count)
		{
			// Arrange
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => protocol.SerializeReadDiscreteInputs(UNIT_ID, 19, (ushort)count));
		}

		[TestMethod]
		public void ShouldThrowOutOfRangeForStartingAddressOnSerializeReadDiscreteInputs()
		{
			// Arrange
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => protocol.SerializeReadDiscreteInputs(UNIT_ID, ushort.MaxValue, 2));
		}

		[TestMethod]
		public void ShouldDeserializeReadDiscreteInputs()
		{
			// Arrange
			int[] setValues = [0, 2, 3, 6, 7, 8, 9, 11, 13, 14, 16, 18];

			string response = $":{UNIT_ID:X2}0203CD6B05";
			AddTrailer(ref response);
			byte[] responseBytes = Encoding.ASCII.GetBytes(response);

			var protocol = new AsciiProtocol();

			// Act
			var discreteInputs = protocol.DeserializeReadDiscreteInputs(responseBytes);

			// Assert
			Assert.IsNotNull(discreteInputs);
			Assert.AreEqual(24, discreteInputs.Count);

			for (int i = 0; i < 24; i++)
			{
				Assert.AreEqual(i, discreteInputs[i].Address);

				if (setValues.Contains(i))
					Assert.IsTrue(discreteInputs[i].Value);
				else
					Assert.IsFalse(discreteInputs[i].Value);
			}
		}

		[TestMethod]
		public void ShouldThrowExceptionOnDeserializeReadDiscreteInputs()
		{
			// Arrange
			string response = $":{UNIT_ID:X2}0202CD6B05";
			AddTrailer(ref response);
			byte[] responseBytes = Encoding.ASCII.GetBytes(response);

			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ModbusException>(() => protocol.DeserializeReadDiscreteInputs(responseBytes));
		}

		#endregion Read Discrete Inputs

		#region Read Holding Registers

		[TestMethod]
		public void ShouldSerializeReadHoldingRegisters()
		{
			// Arrange
			string expectedResponse = $":{UNIT_ID:X2}0300130013";
			AddTrailer(ref expectedResponse);
			byte[] expectedBytes = Encoding.ASCII.GetBytes(expectedResponse);

			var protocol = new AsciiProtocol();

			// Act
			var bytes = protocol.SerializeReadHoldingRegisters(UNIT_ID, 19, 19);

			// Assert
			Assert.IsNotNull(bytes);
			CollectionAssert.AreEqual(expectedBytes, bytes.ToArray());
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(2001)]
		public void ShouldThrowOutOfRangeForCountOnSerializeReadHoldingRegisters(int count)
		{
			// Arrange
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => protocol.SerializeReadHoldingRegisters(UNIT_ID, 19, (ushort)count));
		}

		[TestMethod]
		public void ShouldThrowOutOfRangeForStartingAddressOnSerializeReadHoldingRegisters()
		{
			// Arrange
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => protocol.SerializeReadHoldingRegisters(UNIT_ID, ushort.MaxValue, 2));
		}

		[TestMethod]
		public void ShouldDeserializeReadHoldingRegisters()
		{
			// Arrange
			string response = $":{UNIT_ID:X2}0304022B0064";
			AddTrailer(ref response);
			byte[] responseBytes = Encoding.ASCII.GetBytes(response);

			var protocol = new AsciiProtocol();

			// Act
			var registers = protocol.DeserializeReadHoldingRegisters(responseBytes);

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
			string response = $":{UNIT_ID:X2}0304022B";
			AddTrailer(ref response);
			byte[] responseBytes = Encoding.ASCII.GetBytes(response);

			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ModbusException>(() => protocol.DeserializeReadHoldingRegisters(responseBytes));
		}

		#endregion Read Holding Registers

		#region Read Input Registers

		[TestMethod]
		public void ShouldSerializeReadInputRegisters()
		{
			// Arrange
			string expectedResponse = $":{UNIT_ID:X2}0400130013";
			AddTrailer(ref expectedResponse);
			byte[] expectedBytes = Encoding.ASCII.GetBytes(expectedResponse);

			var protocol = new AsciiProtocol();

			// Act
			var bytes = protocol.SerializeReadInputRegisters(UNIT_ID, 19, 19);

			// Assert
			Assert.IsNotNull(bytes);
			CollectionAssert.AreEqual(expectedBytes, bytes.ToArray());
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(2001)]
		public void ShouldThrowOutOfRangeForCountOnSerializeReadInputRegisters(int count)
		{
			// Arrange
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => protocol.SerializeReadInputRegisters(UNIT_ID, 19, (ushort)count));
		}

		[TestMethod]
		public void ShouldThrowOutOfRangeForStartingAddressOnSerializeReadInputRegisters()
		{
			// Arrange
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => protocol.SerializeReadInputRegisters(UNIT_ID, ushort.MaxValue, 2));
		}

		[TestMethod]
		public void ShouldDeserializeReadInputRegisters()
		{
			// Arrange
			string response = $":{UNIT_ID:X2}0404022B0064";
			AddTrailer(ref response);
			byte[] responseBytes = Encoding.ASCII.GetBytes(response);

			var protocol = new AsciiProtocol();

			// Act
			var registers = protocol.DeserializeReadInputRegisters(responseBytes);

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
			string response = $":{UNIT_ID:X2}0404022B";
			AddTrailer(ref response);
			byte[] responseBytes = Encoding.ASCII.GetBytes(response);

			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ModbusException>(() => protocol.DeserializeReadInputRegisters(responseBytes));
		}

		#endregion Read Input Registers

		#region Read Device Identification

		[TestMethod]
		[DataRow(ModbusDeviceIdentificationCategory.Basic)]
		[DataRow(ModbusDeviceIdentificationCategory.Regular)]
		[DataRow(ModbusDeviceIdentificationCategory.Extended)]
		[DataRow(ModbusDeviceIdentificationCategory.Individual)]
		public void ShouldSerializeReadDeviceIdentification(ModbusDeviceIdentificationCategory category)
		{
			// Arrange
			string expectedResponse = $":{UNIT_ID:X2}2B0E{(byte)category:X2}{(byte)ModbusDeviceIdentificationObject.ProductCode:X2}";
			AddTrailer(ref expectedResponse);
			byte[] expectedBytes = Encoding.ASCII.GetBytes(expectedResponse);

			var protocol = new AsciiProtocol();

			// Act
			var bytes = protocol.SerializeReadDeviceIdentification(UNIT_ID, category, ModbusDeviceIdentificationObject.ProductCode);

			// Assert
			Assert.IsNotNull(bytes);
			CollectionAssert.AreEqual(expectedBytes, bytes.ToArray());
		}

		[TestMethod]
		public void ShouldThrowOutOfRangeExceptionForCategoryOnSerializeReadDeviceIdentification()
		{
			// Arrange
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => protocol.SerializeReadDeviceIdentification(UNIT_ID, (ModbusDeviceIdentificationCategory)10, ModbusDeviceIdentificationObject.ProductCode));
		}

		[TestMethod]
		[DataRow(false)]
		[DataRow(true)]
		public void ShouldDeserializeReadDeviceIdentification(bool moreAndIndividual)
		{
			// Arrange
			string response = $":{UNIT_ID:X2}2B0E02{(moreAndIndividual ? "82" : "02")}{(moreAndIndividual ? "FF" : "00")}{(moreAndIndividual ? "05" : "00")}010402414D";
			AddTrailer(ref response);
			byte[] responseBytes = Encoding.ASCII.GetBytes(response);

			var protocol = new AsciiProtocol();

			// Act
			var result = protocol.DeserializeReadDeviceIdentification(responseBytes);

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
			string response = $":{UNIT_ID:X2}2B0D";
			AddTrailer(ref response);
			byte[] responseBytes = Encoding.ASCII.GetBytes(response);

			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ModbusException>(() => protocol.DeserializeReadDeviceIdentification(responseBytes));
		}

		[TestMethod]
		public void ShouldThrowExceptionOnDeserializeReadDeviceIdentificationForCategory()
		{
			// Arrange
			string response = $":{UNIT_ID:X2}2B0E08";
			AddTrailer(ref response);
			byte[] responseBytes = Encoding.ASCII.GetBytes(response);

			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ModbusException>(() => protocol.DeserializeReadDeviceIdentification(responseBytes));
		}

		#endregion Read Device Identification

		#region Write Single Coil

		[TestMethod]
		public void ShouldSerializeWriteSingleCoil()
		{
			// Arrange
			string expectedResponse = $":{UNIT_ID:X2}05006DFF00";
			AddTrailer(ref expectedResponse);
			byte[] expectedBytes = Encoding.ASCII.GetBytes(expectedResponse);

			var coil = new Coil { Address = 109, Value = true };
			var protocol = new AsciiProtocol();

			// Act
			var result = protocol.SerializeWriteSingleCoil(UNIT_ID, coil);

			// Assert
			Assert.IsNotNull(result);
			CollectionAssert.AreEqual(expectedBytes, result.ToArray());
		}

		[TestMethod]
		public void ShouldThrowArgumentNullOnSerializeWriteSingleCoil()
		{
			// Arrange
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => protocol.SerializeWriteSingleCoil(UNIT_ID, null));
		}

		[TestMethod]
		public void ShouldDeserializeWriteSingleCoil()
		{
			// Arrange
			string response = $":{UNIT_ID:X2}05010AFF00";
			AddTrailer(ref response);
			byte[] bytes = Encoding.ASCII.GetBytes(response);

			var protocol = new AsciiProtocol();

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
			string expectedResponse = $":{UNIT_ID:X2}06006D007B";
			AddTrailer(ref expectedResponse);
			byte[] expectedBytes = Encoding.ASCII.GetBytes(expectedResponse);

			var register = new HoldingRegister { Address = 109, Value = 123 };
			var protocol = new AsciiProtocol();

			// Act
			var result = protocol.SerializeWriteSingleHoldingRegister(UNIT_ID, register);

			// Assert
			Assert.IsNotNull(result);
			CollectionAssert.AreEqual(expectedBytes, result.ToArray());
		}

		[TestMethod]
		public void ShouldThrowArgumentNullOnSerializeWriteSingleHoldingRegister()
		{
			// Arrange
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => protocol.SerializeWriteSingleHoldingRegister(UNIT_ID, null));
		}

		[TestMethod]
		public void ShouldDeserializeWriteSingleHoldingRegister()
		{
			// Arrange
			string response = $":{UNIT_ID:X2}0602020123";
			AddTrailer(ref response);
			byte[] bytes = Encoding.ASCII.GetBytes(response);

			var protocol = new AsciiProtocol();

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
			string expectedResponse = $":{UNIT_ID:X2}0F000A00050115";
			AddTrailer(ref expectedResponse);
			byte[] expectedBytes = Encoding.ASCII.GetBytes(expectedResponse);

			var coils = new Coil[]
			{
				new() { Address = 10, Value = true },
				new() { Address = 11, Value = false },
				new() { Address = 12, Value = true },
				new() { Address = 13, Value = false },
				new() { Address = 14, Value = true },
			};
			var protocol = new AsciiProtocol();

			// Act
			var result = protocol.SerializeWriteMultipleCoils(UNIT_ID, coils);

			// Assert
			Assert.IsNotNull(result);
			CollectionAssert.AreEqual(expectedBytes, result.ToArray());
		}

		[TestMethod]
		public void ShouldThrowArgumentNullOnSerializeWriteMultipleCoils()
		{
			// Arrange
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => protocol.SerializeWriteMultipleCoils(UNIT_ID, null));
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(1969)]
		public void ShouldThrowOutOfRangeForCountOnSerializeWriteMultipleCoils(int count)
		{
			// Arrange
			var coils = new List<Coil>();
			for (int i = 0; i < count; i++)
				coils.Add(new() { Address = (ushort)i });

			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => protocol.SerializeWriteMultipleCoils(UNIT_ID, coils));
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
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => protocol.SerializeWriteMultipleCoils(UNIT_ID, coils));
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
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => protocol.SerializeWriteMultipleCoils(UNIT_ID, coils));
		}

		[TestMethod]
		public void ShouldDeserializeWriteMultipleCoils()
		{
			// Arrange
			string response = $":{UNIT_ID:X2}0F010A000B";
			AddTrailer(ref response);
			byte[] bytes = Encoding.ASCII.GetBytes(response);

			var protocol = new AsciiProtocol();

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
			string expectedResponse = $":{UNIT_ID:X2}10000A000204000A000B";
			AddTrailer(ref expectedResponse);
			byte[] expectedBytes = Encoding.ASCII.GetBytes(expectedResponse);

			var registers = new HoldingRegister[]
			{
				new() { Address = 10, Value = 10 },
				new() { Address = 11, Value = 11 }
			};
			var protocol = new AsciiProtocol();

			// Act
			var result = protocol.SerializeWriteMultipleHoldingRegisters(UNIT_ID, registers);

			// Assert
			Assert.IsNotNull(result);
			CollectionAssert.AreEqual(expectedBytes, result.ToArray());
		}

		[TestMethod]
		public void ShouldThrowArgumentNullOnSerializeWriteMultipleHoldingRegisters()
		{
			// Arrange
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => protocol.SerializeWriteMultipleHoldingRegisters(UNIT_ID, null));
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(124)]
		public void ShouldThrowOutOfRangeForCountOnSerializeWriteMultipleHoldingRegisters(int count)
		{
			// Arrange
			var registers = new List<HoldingRegister>();
			for (int i = 0; i < count; i++)
				registers.Add(new() { Address = (ushort)i });

			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => protocol.SerializeWriteMultipleHoldingRegisters(UNIT_ID, registers));
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
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => protocol.SerializeWriteMultipleHoldingRegisters(UNIT_ID, registers));
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
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => protocol.SerializeWriteMultipleHoldingRegisters(UNIT_ID, registers));
		}

		[TestMethod]
		public void ShouldDeserializeWriteMultipleHoldingRegisters()
		{
			// Arrange
			string response = $":{UNIT_ID:X2}10020A000A";
			AddTrailer(ref response);
			byte[] bytes = Encoding.ASCII.GetBytes(response);

			var protocol = new AsciiProtocol();

			// Act
			var (firstAddress, numberOfCoils) = protocol.DeserializeWriteMultipleHoldingRegisters(bytes);

			// Assert
			Assert.AreEqual(522, firstAddress);
			Assert.AreEqual(10, numberOfCoils);
		}

		#endregion Write Multiple Holding Registers

		#region Validation

		[TestMethod]
		public void ShouldReturnTrueOnCheckResponseComplete()
		{
			// Arrange
			byte[] bytes = Encoding.ASCII.GetBytes($":{UNIT_ID:X2}0100050002XX\r\n");
			var protocol = new AsciiProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsTrue(complete);
		}

		[TestMethod]
		public void ShouldReturnFalseForLessBytesOnCheckResponseComplete()
		{
			// Arrange
			byte[] bytes = Encoding.ASCII.GetBytes(":\r");
			var protocol = new AsciiProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsFalse(complete);
		}

		[TestMethod]
		public void ShouldReturnFalseForMissingCrLfOnCheckResponseComplete()
		{
			// Arrange
			byte[] bytes = Encoding.ASCII.GetBytes($":{UNIT_ID:X2}0100050002XX");
			var protocol = new AsciiProtocol();

			// Act
			bool complete = protocol.CheckResponseComplete(bytes);

			// Assert
			Assert.IsFalse(complete);
		}

		[TestMethod]
		[DataRow(0x01)]
		[DataRow(0x02)]
		[DataRow(0x03)]
		[DataRow(0x04)]
		public void ShouldValidateReadResponse(int fn)
		{
			// Arrange
			string request = $":{UNIT_ID:X2}{fn:X2}00010001";
			string response = $":{UNIT_ID:X2}{fn:X2}0100";
			AddTrailer(ref response);
			var protocol = new AsciiProtocol();

			// Act
			protocol.ValidateResponse(Encoding.ASCII.GetBytes(request), Encoding.ASCII.GetBytes(response));
		}

		[TestMethod]
		[DataRow(0x05)]
		[DataRow(0x06)]
		[DataRow(0x0F)]
		[DataRow(0x10)]
		public void ShouldValidateWriteResponse(int fn)
		{
			// Arrange
			string request = $":{UNIT_ID:X2}{fn:X2}0001FF00";
			string response = $":{UNIT_ID:X2}{fn:X2}0001FF00";
			AddTrailer(ref response);
			var protocol = new AsciiProtocol();

			// Act
			protocol.ValidateResponse(Encoding.ASCII.GetBytes(request), Encoding.ASCII.GetBytes(response));
		}

		[TestMethod]
		public void ShouldThrowForMissingHeaderOnValidateResponse()
		{
			// Arrange
			string request = $":{UNIT_ID:X2}0100010001";
			string response = $"{UNIT_ID:X2}0101009";
			AddTrailer(ref response);
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ModbusException>(() => protocol.ValidateResponse(Encoding.ASCII.GetBytes(request), Encoding.ASCII.GetBytes(response)));
		}

		[TestMethod]
		public void ShouldThrowForMissingTrailerOnValidateResponse()
		{
			// Arrange
			string request = $":{UNIT_ID:X2}0100010001";
			string response = $":{UNIT_ID:X2}010100";
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ModbusException>(() => protocol.ValidateResponse(Encoding.ASCII.GetBytes(request), Encoding.ASCII.GetBytes(response)));
		}

		[TestMethod]
		public void ShouldThrowForUnitIdOnValidateResponse()
		{
			// Arrange
			string request = $":{UNIT_ID:X2}010001FF00";
			string response = $":{UNIT_ID + 1:X2}010001FF00";
			AddTrailer(ref response);
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ModbusException>(() => protocol.ValidateResponse(Encoding.ASCII.GetBytes(request), Encoding.ASCII.GetBytes(response)));
		}

		[TestMethod]
		public void ShouldThrowForLrcOnValidateResponse()
		{
			// Arrange
			string request = $":{UNIT_ID:X2}010001FF00";
			string response = $":{UNIT_ID:X2}010001FF00XX\r\n";
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ModbusException>(() => protocol.ValidateResponse(Encoding.ASCII.GetBytes(request), Encoding.ASCII.GetBytes(response)));
		}

		[TestMethod]
		public void ShouldThrowForFunctionCodeOnValidateResponse()
		{
			// Arrange
			string request = $":{UNIT_ID:X2}010001FF00";
			string response = $":{UNIT_ID:X2}020001FF00";
			AddTrailer(ref response);
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ModbusException>(() => protocol.ValidateResponse(Encoding.ASCII.GetBytes(request), Encoding.ASCII.GetBytes(response)));
		}

		[TestMethod]
		public void ShouldThrowForErrorOnValidateResponse()
		{
			// Arrange
			string request = $":{UNIT_ID:X2}010001FF00";
			string response = $":{UNIT_ID:X2}8101";
			AddTrailer(ref response);
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ModbusException>(() => protocol.ValidateResponse(Encoding.ASCII.GetBytes(request), Encoding.ASCII.GetBytes(response)));
		}

		[TestMethod]
		[DataRow(0x01)]
		[DataRow(0x02)]
		[DataRow(0x03)]
		[DataRow(0x04)]
		public void ShouldThrowForReadLengthOnValidateResponse(int fn)
		{
			// Arrange
			string request = $":{UNIT_ID:X2}{fn:X2}00010002";
			string response = $":{UNIT_ID:X2}{fn:X2}FF0000";
			AddTrailer(ref response);
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ModbusException>(() => protocol.ValidateResponse(Encoding.ASCII.GetBytes(request), Encoding.ASCII.GetBytes(response)));
		}

		[TestMethod]
		[DataRow(0x05)]
		[DataRow(0x06)]
		[DataRow(0x0F)]
		[DataRow(0x10)]
		public void ShouldThrowForWriteLengthOnValidateResponse(int fn)
		{
			// Arrange
			string request = $":{UNIT_ID:X2}{fn:X2}00010002";
			string response = $":{UNIT_ID:X2}{fn:X2}0013000200";
			AddTrailer(ref response);
			var protocol = new AsciiProtocol();

			// Act + Assert
			Assert.ThrowsExactly<ModbusException>(() => protocol.ValidateResponse(Encoding.ASCII.GetBytes(request), Encoding.ASCII.GetBytes(response)));
		}

		[TestMethod]
		public void ShouldReturnValidLrc()
		{
			// Arrange
			string msg = "0207";

			// Act
			string lrc = AsciiProtocol.LRC(msg, 0, 4);

			// Assert
			Assert.AreEqual("F7", lrc);
		}

		[TestMethod]
		[DataRow(null)]
		[DataRow("")]
		[DataRow("     ")]
		[DataRow("\t")]
		public void ShouldThrowArgumentNullExceptionForMessageOnLrc(string msg)
		{
			// Arrange

			// Act + Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => AsciiProtocol.LRC(msg));
		}

		[TestMethod]
		[DataRow(-1)]
		[DataRow(4)]
		public void ShouldThrowArgumentOutOfRangeExceptionForStartOnLrc(int start)
		{
			// Arrange
			string msg = "0207";

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AsciiProtocol.LRC(msg, start));
		}

		[TestMethod]
		[DataRow(0)]
		[DataRow(5)]
		public void ShouldThrowArgumentOutOfRangeExceptionForLengthOnLrc(int length)
		{
			// Arrange
			string msg = "0207";

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AsciiProtocol.LRC(msg, 0, length));
		}

		[TestMethod]
		public void ShouldThrowArgumentExceptionForMessageLengthOnLrc()
		{
			// Arrange
			string msg = "0207";

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => AsciiProtocol.LRC(msg));
		}

		#endregion Validation

		[TestMethod]
		public void ShouldNameAscii()
		{
			// Arrange
			var protocol = new AsciiProtocol();

			// Act
			string result = protocol.Name;

			// Assert
			Assert.AreEqual("ASCII", result);
		}

		private static void AddTrailer(ref string str)
		{
			string lrc = AsciiProtocol.LRC(str);
			str += lrc;
			str += "\r\n";
		}
	}
}
