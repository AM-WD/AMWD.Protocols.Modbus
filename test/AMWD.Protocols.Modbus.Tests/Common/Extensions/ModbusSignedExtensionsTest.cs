namespace AMWD.Protocols.Modbus.Tests.Common.Extensions
{
	[TestClass]
	public class ModbusSignedExtensionsTest
	{
		#region Modbus to value

		[TestMethod]
		public void ShouldGetSByteOnHoldingRegister()
		{
			// Arrange
			var register = new HoldingRegister { Address = 1, HighByte = 0x02, LowByte = 0xFE };

			// Act
			sbyte sb = register.GetSByte();

			// Assert
			Assert.AreEqual(-2, sb);
		}

		[TestMethod]
		public void ShouldGetSByteOnInputRegister()
		{
			// Arrange
			var register = new InputRegister { Address = 1, HighByte = 0x02, LowByte = 0xFE };

			// Act
			sbyte sb = register.GetSByte();

			// Assert
			Assert.AreEqual(-2, sb);
		}

		[TestMethod]
		public void ShouldThrowNullForGetSByte()
		{
			// Arrange
			HoldingRegister register = null;

			// Act + Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => register.GetSByte());
		}

		[TestMethod]
		public void ShouldThrowArgumentForGetSByte()
		{
			// Arrange
			var obj = new Coil();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => obj.GetSByte());
		}

		[TestMethod]
		public void ShouldGetInt16OnHoldingRegister()
		{
			// Arrange
			var register = new HoldingRegister { Address = 1, HighByte = 0x02, LowByte = 0x10 };

			// Act
			short s = register.GetInt16();

			// Assert
			Assert.AreEqual(528, s);
		}

		[TestMethod]
		public void ShouldGetInt16OnInputRegister()
		{
			// Arrange
			var register = new InputRegister { Address = 1, HighByte = 0x02, LowByte = 0x10 };

			// Act
			short s = register.GetInt16();

			// Assert
			Assert.AreEqual(528, s);
		}

		[TestMethod]
		public void ShouldThrowNullForGetInt16()
		{
			// Arrange
			HoldingRegister register = null;

			// Act + Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => register.GetInt16());
		}

		[TestMethod]
		public void ShouldThrowArgumentForGetInt16()
		{
			// Arrange
			var obj = new Coil();

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => obj.GetInt16());
		}

		[TestMethod]
		public void ShouldGetInt32()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new HoldingRegister(),
				new HoldingRegister { Address = 100, HighByte = 0x01, LowByte = 0x02 },
				new HoldingRegister { Address = 101, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act
			int i = registers.GetInt32(1);

			// Assert
			Assert.AreEqual(16909060, i);
		}

		[TestMethod]
		public void ShouldGetInt32ReversedRegisters()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new HoldingRegister { Address = 101, HighByte = 0x01, LowByte = 0x02 },
				new HoldingRegister { Address = 100, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act
			int i = registers.GetInt32(0, reverseRegisterOrder: true);

			// Assert
			Assert.AreEqual(16909060, i);
		}

		[TestMethod]
		public void ShouldThrowNullOnGetInt32()
		{
			// Arrange
			HoldingRegister[] registers = null;

			// Act + Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => registers.GetInt32(0));
		}

		[TestMethod]
		public void ShouldThrowArgumentOnGetInt32ForLength()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new HoldingRegister { Address = 101, HighByte = 0x01, LowByte = 0x02 }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => registers.GetInt32(0));
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(-1)]
		public void ShouldThrowArgumentOutOfRangeOnGetInt32(int startIndex)
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new HoldingRegister { Address = 101, HighByte = 0x01, LowByte = 0x02 },
				new HoldingRegister { Address = 100, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => registers.GetInt32(startIndex));
		}

		[TestMethod]
		public void ShouldThrowArgumentOnGetInt32ForType()
		{
			// Arrange
			var registers = new ModbusObject[]
			{
				new HoldingRegister { Address = 100, HighByte = 0x01, LowByte = 0x02 },
				new InputRegister { Address = 101, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => registers.GetInt32(0));
		}

		[TestMethod]
		public void ShouldGetInt64()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new HoldingRegister(),
				new HoldingRegister { Address = 100, HighByte = 0x00, LowByte = 0x00 },
				new HoldingRegister { Address = 101, HighByte = 0x00, LowByte = 0x00 },
				new HoldingRegister { Address = 102, HighByte = 0x01, LowByte = 0x02 },
				new HoldingRegister { Address = 103, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act
			long l = registers.GetInt64(1);

			// Assert
			Assert.AreEqual(16909060L, l);
		}

		[TestMethod]
		public void ShouldGetInt64ReversedRegisters()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new HoldingRegister { Address = 103, HighByte = 0x00, LowByte = 0x00 },
				new HoldingRegister { Address = 102, HighByte = 0x00, LowByte = 0x00 },
				new HoldingRegister { Address = 101, HighByte = 0x01, LowByte = 0x02 },
				new HoldingRegister { Address = 100, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act
			long l = registers.GetInt64(0, reverseRegisterOrder: true);

			// Assert
			Assert.AreEqual(16909060L, l);
		}

		[TestMethod]
		public void ShouldThrowNullOnGetInt64()
		{
			// Arrange
			HoldingRegister[] registers = null;

			// Act + Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => registers.GetInt64(0));
		}

		[TestMethod]
		public void ShouldThrowArgumentOnGetInt64ForLength()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new HoldingRegister { Address = 101, HighByte = 0x00, LowByte = 0x00 },
				new HoldingRegister { Address = 102, HighByte = 0x01, LowByte = 0x02 },
				new HoldingRegister { Address = 103, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => registers.GetInt64(0));
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(-1)]
		public void ShouldThrowArgumentOutOfRangeOnGetInt64(int startIndex)
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new HoldingRegister { Address = 100, HighByte = 0x00, LowByte = 0x00 },
				new HoldingRegister { Address = 101, HighByte = 0x00, LowByte = 0x00 },
				new HoldingRegister { Address = 102, HighByte = 0x01, LowByte = 0x02 },
				new HoldingRegister { Address = 103, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => registers.GetInt64(startIndex));
		}

		[TestMethod]
		public void ShouldThrowArgumentOnGetInt64ForType()
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
			Assert.ThrowsExactly<ArgumentException>(() => registers.GetInt64(0));
		}

		#endregion Modbus to value

		#region Value to Modbus

		[TestMethod]
		public void ShouldConvertSByte()
		{
			// Arrange
			sbyte sb = -2;

			// Act
			var register = sb.ToRegister(24);

			// Assert
			Assert.IsNotNull(register);
			Assert.AreEqual(24, register.Address);
			Assert.AreEqual(0x00, register.HighByte);
			Assert.AreEqual(0xFE, register.LowByte);
		}

		[TestMethod]
		public void ShouldConvertInt16()
		{
			// Arrange
			short s = 1000;

			// Act
			var register = s.ToRegister(123);

			// Assert
			Assert.IsNotNull(register);
			Assert.AreEqual(123, register.Address);
			Assert.AreEqual(0x03, register.HighByte);
			Assert.AreEqual(0xE8, register.LowByte);
		}

		[TestMethod]
		public void ShouldConvertInt32()
		{
			// Arrange
			int i = 75000;

			// Act
			var registers = i.ToRegister(5).ToList();

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
		public void ShouldConvertInt32Reversed()
		{
			// Arrange
			int i = 75000;

			// Act
			var registers = i.ToRegister(5, reverseRegisterOrder: true).ToList();

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
		public void ShouldConvertInt64()
		{
			// Arrange
			long l = 75000;

			// Act
			var registers = l.ToRegister(10).ToList();

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
		public void ShouldConvertInt64Reversed()
		{
			// Arrange
			long l = 75000;

			// Act
			var registers = l.ToRegister(10, reverseRegisterOrder: true).ToList();

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
