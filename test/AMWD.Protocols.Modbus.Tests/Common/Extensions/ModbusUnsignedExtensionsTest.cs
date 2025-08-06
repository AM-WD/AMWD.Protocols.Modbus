namespace AMWD.Protocols.Modbus.Tests.Common.Extensions
{
	[TestClass]
	public class ModbusUnsignedExtensionsTest
	{
		#region Modbus to value

		[TestMethod]
		public void ShouldGetByteOnHoldingRegister()
		{
			// Arrange
			var register = new HoldingRegister { Address = 1, HighByte = 0x02, LowByte = 0x10 };

			// Act
			byte b = register.GetByte();

			// Assert
			Assert.AreEqual(16, b);
		}

		[TestMethod]
		public void ShouldGetByteOnInputRegister()
		{
			// Arrange
			var register = new InputRegister { Address = 1, HighByte = 0x02, LowByte = 0x10 };

			// Act
			byte b = register.GetByte();

			// Assert
			Assert.AreEqual(16, b);
		}

		[TestMethod]
		public void ShouldThrowNullForGetByte()
		{
			// Arrange
			HoldingRegister register = null;

			// Act + Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => register.GetByte());
		}

		[TestMethod]
		public void ShouldThrowArgumentForGetByte()
		{
			// Arrange
			var obj = new Coil();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => obj.GetByte());
		}

		[TestMethod]
		public void ShouldGetUInt16OnHoldingRegister()
		{
			// Arrange
			var register = new HoldingRegister { Address = 1, HighByte = 0x02, LowByte = 0x10 };

			// Act
			ushort us = register.GetUInt16();

			// Assert
			Assert.AreEqual(528, us);
		}

		[TestMethod]
		public void ShouldGetUInt16OnInputRegister()
		{
			// Arrange
			var register = new InputRegister { Address = 1, HighByte = 0x02, LowByte = 0x10 };

			// Act
			ushort us = register.GetUInt16();

			// Assert
			Assert.AreEqual(528, us);
		}

		[TestMethod]
		public void ShouldThrowNullForGetUInt16()
		{
			// Arrange
			HoldingRegister register = null;

			// Act + Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => register.GetUInt16());
		}

		[TestMethod]
		public void ShouldThrowArgumentForGetUInt16()
		{
			// Arrange
			var obj = new Coil();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => obj.GetUInt16());
		}

		[TestMethod]
		public void ShouldGetUInt32()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new(),
				new() { Address = 100, HighByte = 0x01, LowByte = 0x02 },
				new() { Address = 101, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act
			uint ui = registers.GetUInt32(1);

			// Assert
			Assert.AreEqual(16909060u, ui);
		}

		[TestMethod]
		public void ShouldGetUInt32ReversedRegisters()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 101, HighByte = 0x01, LowByte = 0x02 },
				new() { Address = 100, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act
			uint ui = registers.GetUInt32(0, reverseRegisterOrder: true);

			// Assert
			Assert.AreEqual(16909060u, ui);
		}

		[TestMethod]
		public void ShouldThrowNullOnGetUInt32()
		{
			// Arrange
			HoldingRegister[] registers = null;

			// Act + Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => registers.GetUInt32(0));
		}

		[TestMethod]
		public void ShouldThrowArgumentOnGetUInt32ForLength()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 101, HighByte = 0x01, LowByte = 0x02 }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => registers.GetUInt32(1));
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(-1)]
		public void ShouldThrowArgumentOutOfRangeOnGetUInt32(int startIndex)
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 101, HighByte = 0x01, LowByte = 0x02 },
				new() { Address = 100, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => registers.GetUInt32(startIndex));
		}

		[TestMethod]
		public void ShouldThrowArgumentOnGetUInt32ForType()
		{
			// Arrange
			var registers = new ModbusObject[]
			{
				new HoldingRegister { Address = 100, HighByte = 0x01, LowByte = 0x02 },
				new InputRegister { Address = 101, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => registers.GetUInt32(0));
		}

		[TestMethod]
		public void ShouldGetUInt64()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new(),
				new() { Address = 100, HighByte = 0x00, LowByte = 0x00 },
				new() { Address = 101, HighByte = 0x00, LowByte = 0x00 },
				new() { Address = 102, HighByte = 0x01, LowByte = 0x02 },
				new() { Address = 103, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act
			ulong ul = registers.GetUInt64(1);

			// Assert
			Assert.AreEqual(16909060ul, ul);
		}

		[TestMethod]
		public void ShouldGetUInt64ReversedRegisters()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 103, HighByte = 0x00, LowByte = 0x00 },
				new() { Address = 102, HighByte = 0x00, LowByte = 0x00 },
				new() { Address = 101, HighByte = 0x01, LowByte = 0x02 },
				new() { Address = 100, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act
			ulong ul = registers.GetUInt64(0, reverseRegisterOrder: true);

			// Assert
			Assert.AreEqual(16909060ul, ul);
		}

		[TestMethod]
		public void ShouldThrowNullOnGetUInt64()
		{
			// Arrange
			HoldingRegister[] registers = null;

			// Act + Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => registers.GetUInt64(0));
		}

		[TestMethod]
		public void ShouldThrowArgumentOnGetUInt64ForLength()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 101, HighByte = 0x00, LowByte = 0x00 },
				new() { Address = 102, HighByte = 0x01, LowByte = 0x02 },
				new() { Address = 103, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => registers.GetUInt64(0));
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(-1)]
		public void ShouldThrowArgumentOutOfRangeOnGetUInt64(int startIndex)
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 100, HighByte = 0x00, LowByte = 0x00 },
				new() { Address = 101, HighByte = 0x00, LowByte = 0x00 },
				new() { Address = 102, HighByte = 0x01, LowByte = 0x02 },
				new() { Address = 103, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => registers.GetUInt64(startIndex));
		}

		[TestMethod]
		public void ShouldThrowArgumentOnGetUInt64ForType()
		{
			// Arrange
			var registers = new ModbusObject[]
			{
				new HoldingRegister { Address = 100, HighByte = 0x00, LowByte = 0x00 },
				new InputRegister { Address = 101, HighByte = 0x00, LowByte = 0x00 },
				new HoldingRegister { Address = 102, HighByte = 0x01, LowByte = 0x02 },
				new InputRegister { Address = 103, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => registers.GetUInt64(0));
		}

		#endregion Modbus to value

		#region Value to Modbus

		[TestMethod]
		public void ShouldConvertByte()
		{
			// Arrange
			byte b = 123;

			// Act
			var register = b.ToRegister(321);

			// Assert
			Assert.IsNotNull(register);
			Assert.AreEqual(321, register.Address);
			Assert.AreEqual(0, register.HighByte);
			Assert.AreEqual(123, register.LowByte);
		}

		[TestMethod]
		public void ShouldConvertUInt16()
		{
			// Arrange
			ushort us = 1000;

			// Act
			var register = us.ToRegister(123);

			// Assert
			Assert.IsNotNull(register);
			Assert.AreEqual(123, register.Address);
			Assert.AreEqual(0x03, register.HighByte);
			Assert.AreEqual(0xE8, register.LowByte);
		}

		[TestMethod]
		public void ShouldConvertUInt32()
		{
			// Arrange
			uint ui = 75000;

			// Act
			var registers = ui.ToRegister(5).ToList();

			// Assert
			Assert.IsNotNull(registers);
			Assert.AreEqual(2, registers.Count);

			Assert.AreEqual(5, registers[0].Address);
			Assert.AreEqual(0x00, registers[0].HighByte);
			Assert.AreEqual(0x01, registers[0].LowByte);

			Assert.AreEqual(6, registers[1].Address);
			Assert.AreEqual(0x24, registers[1].HighByte);
			Assert.AreEqual(0xF8, registers[1].LowByte);
		}

		[TestMethod]
		public void ShouldConvertUInt32Reversed()
		{
			// Arrange
			uint ui = 75000;

			// Act
			var registers = ui.ToRegister(5, reverseRegisterOrder: true).ToList();

			// Assert
			Assert.IsNotNull(registers);
			Assert.AreEqual(2, registers.Count);

			Assert.AreEqual(6, registers[0].Address);
			Assert.AreEqual(0x00, registers[0].HighByte);
			Assert.AreEqual(0x01, registers[0].LowByte);

			Assert.AreEqual(5, registers[1].Address);
			Assert.AreEqual(0x24, registers[1].HighByte);
			Assert.AreEqual(0xF8, registers[1].LowByte);
		}

		[TestMethod]
		public void ShouldConvertUInt64()
		{
			// Arrange
			ulong ul = 75000;

			// Act
			var registers = ul.ToRegister(10).ToList();

			// Assert
			Assert.IsNotNull(registers);
			Assert.AreEqual(4, registers.Count);

			Assert.AreEqual(10, registers[0].Address);
			Assert.AreEqual(0x00, registers[0].HighByte);
			Assert.AreEqual(0x00, registers[0].LowByte);

			Assert.AreEqual(11, registers[1].Address);
			Assert.AreEqual(0x00, registers[1].HighByte);
			Assert.AreEqual(0x00, registers[1].LowByte);

			Assert.AreEqual(12, registers[2].Address);
			Assert.AreEqual(0x00, registers[2].HighByte);
			Assert.AreEqual(0x01, registers[2].LowByte);

			Assert.AreEqual(13, registers[3].Address);
			Assert.AreEqual(0x24, registers[3].HighByte);
			Assert.AreEqual(0xF8, registers[3].LowByte);
		}

		[TestMethod]
		public void ShouldConvertUInt64Reversed()
		{
			// Arrange
			ulong ul = 75000;

			// Act
			var registers = ul.ToRegister(10, reverseRegisterOrder: true).ToList();

			// Assert
			Assert.IsNotNull(registers);
			Assert.AreEqual(4, registers.Count);

			Assert.AreEqual(13, registers[0].Address);
			Assert.AreEqual(0x00, registers[0].HighByte);
			Assert.AreEqual(0x00, registers[0].LowByte);

			Assert.AreEqual(12, registers[1].Address);
			Assert.AreEqual(0x00, registers[1].HighByte);
			Assert.AreEqual(0x00, registers[1].LowByte);

			Assert.AreEqual(11, registers[2].Address);
			Assert.AreEqual(0x00, registers[2].HighByte);
			Assert.AreEqual(0x01, registers[2].LowByte);

			Assert.AreEqual(10, registers[3].Address);
			Assert.AreEqual(0x24, registers[3].HighByte);
			Assert.AreEqual(0xF8, registers[3].LowByte);
		}

		#endregion Value to Modbus
	}
}
