namespace AMWD.Protocols.Modbus.Tests.Common.Models
{
	[TestClass]
	public class HoldingRegisterTests
	{
		[TestMethod]
		public void ShouldSuccessfulCompare()
		{
			// Arrange
			var register1 = new HoldingRegister { Address = 123, HighByte = 0xBE, LowByte = 0xEF };
			var register2 = new HoldingRegister { Address = 123, HighByte = 0xBE, LowByte = 0xEF };

			// Act
			bool success = register1.Equals(register2);

			// Assert
			Assert.IsTrue(success);
		}

		[TestMethod]
		public void ShouldFailOnInstanceComparing()
		{
			// Arrange
			var register1 = new HoldingRegister { Address = 123, HighByte = 0xBE, LowByte = 0xEF };
			var register2 = new { Address = 123, HighByte = 0xBE, LowByte = 0xEF };

			// Act
			bool success = register1.Equals(register2);

			// Assert
			Assert.IsFalse(success);
		}

		[TestMethod]
		public void ShouldFailOnTypeComparing()
		{
			// Arrange
			var register1 = new HoldingRegister { Address = 123, HighByte = 0xBE, LowByte = 0xEF };
			var register2 = new InputRegister { Address = 123, HighByte = 0xBE, LowByte = 0xEF };

			// Act
			bool success = register1.Equals(register2);

			// Assert
			Assert.IsFalse(success);
		}

		[TestMethod]
		public void ShouldFailOnAddressComparing()
		{
			// Arrange
			var register1 = new HoldingRegister { Address = 123, HighByte = 0xBE, LowByte = 0xEF };
			var register2 = new HoldingRegister { Address = 321, HighByte = 0xBE, LowByte = 0xEF };

			// Act
			bool success = register1.Equals(register2);

			// Assert
			Assert.IsFalse(success);
		}

		[TestMethod]
		public void ShouldFailOnHighByteComparing()
		{
			// Arrange
			var register1 = new HoldingRegister { Address = 123, HighByte = 0xBE, LowByte = 0xEF };
			var register2 = new HoldingRegister { Address = 123, HighByte = 0xBD, LowByte = 0xEF };

			// Act
			bool success = register1.Equals(register2);

			// Assert
			Assert.IsFalse(success);
		}

		[TestMethod]
		public void ShouldFailOnLowByteComparing()
		{
			// Arrange
			var register1 = new HoldingRegister { Address = 123, HighByte = 0xBE, LowByte = 0xEF };
			var register2 = new HoldingRegister { Address = 123, HighByte = 0xBE, LowByte = 0xEE };

			// Act
			bool success = register1.Equals(register2);

			// Assert
			Assert.IsFalse(success);
		}

		[TestMethod]
		public void ShouldPrintPrettyString()
		{
			// Arrange
			var register = new HoldingRegister { Address = 123, HighByte = 0xBE, LowByte = 0xEF };

			// Act
			string str = register.ToString();

			// Assert
			Assert.AreEqual("Holding Register #123 | 48879 | HI: BE, LO: EF", str);
		}

		[TestMethod]
		public void ShouldSetByValue()
		{
			// Arrange
			var register = new HoldingRegister { Address = 123 };

			// Act
			register.Value = 48879;

			// Assert
			Assert.AreEqual(0xBE, register.HighByte);
			Assert.AreEqual(0xEF, register.LowByte);
		}
	}
}
