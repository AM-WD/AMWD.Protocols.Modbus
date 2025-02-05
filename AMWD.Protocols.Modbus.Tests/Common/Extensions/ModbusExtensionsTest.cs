using System.Text;

namespace AMWD.Protocols.Modbus.Tests.Common.Extensions
{
	[TestClass]
	public class ModbusExtensionsTest
	{
		#region Modbus to value

		[TestMethod]
		public void ShouldConvertToBoolean()
		{
			// Arrange
			var coil = new Coil { HighByte = 0x00 };
			var discreteInput = new DiscreteInput { HighByte = 0xFF };
			var holdingRegister = new HoldingRegister { HighByte = 0x01 };
			var inputRegister = new InputRegister { LowByte = 0x10 };

			// Act
			bool coilResult = coil.GetBoolean();
			bool discreteInputResult = discreteInput.GetBoolean();
			bool holdingRegisterResult = holdingRegister.GetBoolean();
			bool inputRegisterResult = inputRegister.GetBoolean();

			// Assert
			Assert.IsFalse(coilResult);
			Assert.IsTrue(discreteInputResult);
			Assert.IsTrue(holdingRegisterResult);
			Assert.IsTrue(inputRegisterResult);
		}

		[TestMethod]
		public void ShouldThrowNullOnGetBoolean()
		{
			// Arrange
			Coil coil = null;

			// Act + Assert
			Assert.ThrowsException<ArgumentNullException>(() => coil.GetBoolean());
		}

		[TestMethod]
		public void ShouldConvertToString()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 1, HighByte = 65, LowByte = 66 },
				new() { Address = 2, HighByte = 67, LowByte = 0 },
				new() { Address = 3, HighByte = 95, LowByte = 96 }
			};

			// Act
			string text = registers.GetString(3);

			// Assert
			Assert.AreEqual("ABC", text);
		}

		[TestMethod]
		public void ShouldConvertToStringReversedBytes()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 1, HighByte = 66, LowByte = 65 },
				new() { Address = 2, HighByte = 0, LowByte = 67 }
			};

			// Act
			string text = registers.GetString(2, reverseByteOrderPerRegister: true);

			// Assert
			Assert.AreEqual("ABC", text);
		}

		[TestMethod]
		public void ShouldConvertToStringReversedRegisters()
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 2, HighByte = 65, LowByte = 66 },
				new() { Address = 1, HighByte = 67, LowByte = 0 },
			};

			// Act
			string text = registers.GetString(2, reverseRegisterOrder: true);

			// Assert
			Assert.AreEqual("ABC", text);
		}

		[TestMethod]
		public void ShouldThrowNullOnString()
		{
			// Arrange
			HoldingRegister[] list = null;

			// Act + Assert
			Assert.ThrowsException<ArgumentNullException>(() => list.GetString(2));
		}

		[TestMethod]
		public void ShouldThrowArgumentOnStringForEmptyList()
		{
			// Arrange
			var registers = Array.Empty<HoldingRegister>();

			// Act + Assert
			Assert.ThrowsException<ArgumentException>(() => registers.GetString(2));
		}

		[DataTestMethod]
		[DataRow(1)]
		[DataRow(-1)]
		public void ShouldThrowArgumentOutOfRangeOnString(int startIndex)
		{
			// Arrange
			var registers = new HoldingRegister[]
			{
				new() { Address = 1, HighByte = 65, LowByte = 66 },
				new() { Address = 2, HighByte = 67, LowByte = 0 }
			};

			// Act + Assert
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => registers.GetString(2, startIndex));
		}

		[TestMethod]
		public void ShouldThrowArgumentOnStringForMixedTypes()
		{
			// Arrange
			var registers = new ModbusObject[]
			{
				new HoldingRegister { Address = 1, HighByte = 65, LowByte = 66 },
				new InputRegister { Address = 2, HighByte = 67, LowByte = 0 }
			};

			// Act + Assert
			Assert.ThrowsException<ArgumentException>(() => registers.GetString(2));
		}

		#endregion Modbus to value

		#region Value to Modbus

		[TestMethod]
		public void ShouldGetBooleanCoil()
		{
			// Arrange
			bool value = false;

			// Act
			var coil = value.ToCoil(123);

			// Assert
			Assert.IsNotNull(coil);
			Assert.AreEqual(123, coil.Address);
			Assert.IsFalse(coil.Value);
		}

		[TestMethod]
		public void ShouldGetBooleanRegisterTrue()
		{
			// Arrange
			bool value = true;

			// Act
			var register = value.ToRegister(321);

			// Assert
			Assert.IsNotNull(register);
			Assert.AreEqual(321, register.Address);
			Assert.IsTrue(register.Value > 0);
		}

		[TestMethod]
		public void ShouldGetBooleanRegisterFalse()
		{
			// Arrange
			bool value = false;

			// Act
			var register = value.ToRegister(321);

			// Assert
			Assert.IsNotNull(register);
			Assert.AreEqual(321, register.Address);
			Assert.IsTrue(register.Value == 0);
		}

		[TestMethod]
		public void ShouldGetString()
		{
			// Arrange
			string str = "abc";

			// Act
			var registers = str.ToRegisters(100).ToList();

			// Assert
			Assert.IsNotNull(registers);
			Assert.AreEqual(2, registers.Count);

			Assert.AreEqual(100, registers[0].Address);
			Assert.AreEqual(97, registers[0].HighByte);
			Assert.AreEqual(98, registers[0].LowByte);

			Assert.AreEqual(101, registers[1].Address);
			Assert.AreEqual(99, registers[1].HighByte);
			Assert.AreEqual(0, registers[1].LowByte);
		}

		[TestMethod]
		public void ShouldGetStringReversedRegisters()
		{
			// Arrange
			string str = "abc";

			// Act
			var registers = str.ToRegisters(100, reverseRegisterOrder: true).ToList();

			// Assert
			Assert.IsNotNull(registers);
			Assert.AreEqual(2, registers.Count);

			Assert.AreEqual(101, registers[0].Address);
			Assert.AreEqual(97, registers[0].HighByte);
			Assert.AreEqual(98, registers[0].LowByte);

			Assert.AreEqual(100, registers[1].Address);
			Assert.AreEqual(99, registers[1].HighByte);
			Assert.AreEqual(0, registers[1].LowByte);
		}

		[TestMethod]
		public void ShouldGetStringReversedBytes()
		{
			// Arrange
			string str = "abc";

			// Act
			var registers = str.ToRegisters(100, reverseByteOrderPerRegister: true).ToList();

			// Assert
			Assert.IsNotNull(registers);
			Assert.AreEqual(2, registers.Count);

			Assert.AreEqual(100, registers[0].Address);
			Assert.AreEqual(97, registers[0].LowByte);
			Assert.AreEqual(98, registers[0].HighByte);

			Assert.AreEqual(101, registers[1].Address);
			Assert.AreEqual(99, registers[1].LowByte);
			Assert.AreEqual(0, registers[1].HighByte);
		}

		[TestMethod]
		public void ShouldThrowNullOnGetString()
		{
			// Arrange
			string str = null;

			// Act + Assert
			Assert.ThrowsException<ArgumentNullException>(() => str.ToRegisters(100).ToArray());
		}

		#endregion Value to Modbus
	}
}
