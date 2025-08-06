namespace AMWD.Protocols.Modbus.Tests.Common.Extensions
{
	[TestClass]
	public class ModbusDecimalExtensionsTest
	{
		#region Modbus to value

		[TestMethod]
		public void ShouldGetSingle()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new(),
				new() { Address = 100, HighByte = 0x41, LowByte = 0x45 },
				new() { Address = 101, HighByte = 0x70, LowByte = 0xA4 }
			};

			// Act
			float f = registers.GetSingle(1);

			// Assert
			Assert.AreEqual(12.34f, f);
		}

		[TestMethod]
		public void ShouldGetSingleReversedRegisters()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 101, HighByte = 0x41, LowByte = 0x45 },
				new() { Address = 100, HighByte = 0x70, LowByte = 0xA4 }
			};

			// Act
			float f = registers.GetSingle(0, reverseRegisterOrder: true);

			// Assert
			Assert.AreEqual(12.34f, f);
		}

		[TestMethod]
		public void ShouldThrowNullOnGetSingle()
		{
			// Arrange
			HoldingRegister[] registers = null;

			// Act + Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => registers.GetSingle(0));
		}

		[TestMethod]
		public void ShouldThrowArgumentOnGetSingleForLength()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 101, HighByte = 0x01, LowByte = 0x02 }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => registers.GetSingle(0));
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(-1)]
		public void ShouldThrowArgumentOutOfRangeOnGetSingle(int startIndex)
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 101, HighByte = 0x01, LowByte = 0x02 },
				new() { Address = 100, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => registers.GetSingle(startIndex));
		}

		[TestMethod]
		public void ShouldThrowArgumentOnGetSingleForType()
		{
			// Arrange
			var registers = new ModbusObject[]
			{
				new HoldingRegister { Address = 100, HighByte = 0x01, LowByte = 0x02 },
				new InputRegister { Address = 101, HighByte = 0x03, LowByte = 0x04 }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => registers.GetSingle(0));
		}

		[TestMethod]
		public void ShouldGetDouble()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new(),
				new() { Address = 100, HighByte = 0x40, LowByte = 0x28 },
				new() { Address = 101, HighByte = 0xAE, LowByte = 0x14 },
				new() { Address = 102, HighByte = 0x7A, LowByte = 0xE1 },
				new() { Address = 103, HighByte = 0x47, LowByte = 0xAE }
			};

			// Act
			double d = registers.GetDouble(1);

			// Assert
			Assert.AreEqual(12.34, d);
		}

		[TestMethod]
		public void ShouldGetDoubleReversedRegisters()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 103, HighByte = 0x40, LowByte = 0x28 },
				new() { Address = 102, HighByte = 0xAE, LowByte = 0x14 },
				new() { Address = 101, HighByte = 0x7A, LowByte = 0xE1 },
				new() { Address = 100, HighByte = 0x47, LowByte = 0xAE }
			};

			// Act
			double d = registers.GetDouble(0, reverseRegisterOrder: true);

			// Assert
			Assert.AreEqual(12.34, d);
		}

		[TestMethod]
		public void ShouldThrowNullOnGetDouble()
		{
			// Arrange
			HoldingRegister[] registers = null;

			// Act + Assert
			Assert.ThrowsExactly<ArgumentNullException>(() => registers.GetDouble(0));
		}

		[TestMethod]
		public void ShouldThrowArgumentOnGetDoubleForLength()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 100, HighByte = 0x40, LowByte = 0x28 },
				new() { Address = 101, HighByte = 0xAE, LowByte = 0x14 },
				new() { Address = 102, HighByte = 0x7A, LowByte = 0xE1 }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => registers.GetDouble(0));
		}

		[TestMethod]
		[DataRow(1)]
		[DataRow(-1)]
		public void ShouldThrowArgumentOutOfRangeOnGetDouble(int startIndex)
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 100, HighByte = 0x40, LowByte = 0x28 },
				new() { Address = 101, HighByte = 0xAE, LowByte = 0x14 },
				new() { Address = 102, HighByte = 0x7A, LowByte = 0xE1 },
				new() { Address = 103, HighByte = 0x47, LowByte = 0xAE }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => registers.GetDouble(startIndex));
		}

		[TestMethod]
		public void ShouldThrowArgumentOnGetDoubleForType()
		{
			// Arrange
			var registers = new ModbusObject[]
			{
				new HoldingRegister { Address = 100, HighByte = 0x40, LowByte = 0x28 },
				new InputRegister { Address = 101, HighByte = 0xAE, LowByte = 0x14 },
				new HoldingRegister { Address = 102, HighByte = 0x7A, LowByte = 0xE1 },
				new InputRegister { Address = 103, HighByte = 0x47, LowByte = 0xAE }
			};

			// Act + Assert
			Assert.ThrowsExactly<ArgumentException>(() => registers.GetDouble(0));
		}

		#endregion Modbus to value

		#region Value to Modbus

		[TestMethod]
		public void ShouldConvertSingle()
		{
			// Arrange
			float f = 12.34f;

			// Act
			var registers = f.ToRegister(5).ToList();

			// Assert
			Assert.IsNotNull(registers);
			Assert.AreEqual(2, registers.Count);

			Assert.AreEqual(5, registers[0].Address);
			Assert.AreEqual(0x41, registers[0].HighByte);
			Assert.AreEqual(0x45, registers[0].LowByte);

			Assert.AreEqual(6, registers[1].Address);
			Assert.AreEqual(0x70, registers[1].HighByte);
			Assert.AreEqual(0xA4, registers[1].LowByte);
		}

		[TestMethod]
		public void ShouldConvertSingleReversed()
		{
			// Arrange
			float f = 12.34f;

			// Act
			var registers = f.ToRegister(5, reverseRegisterOrder: true).ToList();

			// Assert
			Assert.IsNotNull(registers);
			Assert.AreEqual(2, registers.Count);

			Assert.AreEqual(6, registers[0].Address);
			Assert.AreEqual(0x41, registers[0].HighByte);
			Assert.AreEqual(0x45, registers[0].LowByte);

			Assert.AreEqual(5, registers[1].Address);
			Assert.AreEqual(0x70, registers[1].HighByte);
			Assert.AreEqual(0xA4, registers[1].LowByte);
		}

		[TestMethod]
		public void ShouldConvertDouble()
		{
			// Arrange
			double d = 12.34;

			// Act
			var registers = d.ToRegister(5).ToList();

			// Assert
			Assert.IsNotNull(registers);
			Assert.AreEqual(4, registers.Count);

			Assert.AreEqual(5, registers[0].Address);
			Assert.AreEqual(0x40, registers[0].HighByte);
			Assert.AreEqual(0x28, registers[0].LowByte);

			Assert.AreEqual(6, registers[1].Address);
			Assert.AreEqual(0xAE, registers[1].HighByte);
			Assert.AreEqual(0x14, registers[1].LowByte);

			Assert.AreEqual(7, registers[2].Address);
			Assert.AreEqual(0x7A, registers[2].HighByte);
			Assert.AreEqual(0xE1, registers[2].LowByte);

			Assert.AreEqual(8, registers[3].Address);
			Assert.AreEqual(0x47, registers[3].HighByte);
			Assert.AreEqual(0xAE, registers[3].LowByte);
		}

		[TestMethod]
		public void ShouldConvertDoubleReversed()
		{
			// Arrange
			double d = 12.34;

			// Act
			var registers = d.ToRegister(5, reverseRegisterOrder: true).ToList();

			// Assert
			Assert.IsNotNull(registers);
			Assert.AreEqual(4, registers.Count);

			Assert.AreEqual(8, registers[0].Address);
			Assert.AreEqual(0x40, registers[0].HighByte);
			Assert.AreEqual(0x28, registers[0].LowByte);

			Assert.AreEqual(7, registers[1].Address);
			Assert.AreEqual(0xAE, registers[1].HighByte);
			Assert.AreEqual(0x14, registers[1].LowByte);

			Assert.AreEqual(6, registers[2].Address);
			Assert.AreEqual(0x7A, registers[2].HighByte);
			Assert.AreEqual(0xE1, registers[2].LowByte);

			Assert.AreEqual(5, registers[3].Address);
			Assert.AreEqual(0x47, registers[3].HighByte);
			Assert.AreEqual(0xAE, registers[3].LowByte);
		}

		#endregion Value to Modbus
	}
}
