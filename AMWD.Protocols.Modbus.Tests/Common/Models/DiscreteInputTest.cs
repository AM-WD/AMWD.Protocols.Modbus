namespace AMWD.Protocols.Modbus.Tests.Common.Models
{
	[TestClass]
	public class DiscreteInputTest
	{
		[TestMethod]
		public void ShouldSuccessfulCompare()
		{
			// Arrange
			var input1 = new DiscreteInput { Address = 123, HighByte = 0xFF, LowByte = 0x00 };
			var input2 = new DiscreteInput { Address = 123, HighByte = 0xFF, LowByte = 0x00 };

			// Act
			bool success = input1.Equals(input2);

			// Assert
			Assert.IsTrue(success);
		}

		[TestMethod]
		public void ShouldFailOnInstanceComparing()
		{
			// Arrange
			var coil1 = new Coil { Address = 123, Value = true };
			var coil2 = new { Address = 123, HighByte = 0xFF };

			// Act
			bool success = coil1.Equals(coil2);

			// Assert
			Assert.IsFalse(success);
		}

		[TestMethod]
		public void ShouldFailOnTypeComparing()
		{
			// Arrange
			var input1 = new DiscreteInput { Address = 123, HighByte = 0xFF, LowByte = 0x00 };
			var input2 = new Coil { Address = 123, HighByte = 0xFF, LowByte = 0x00 };

			// Act
			bool success = input1.Equals(input2);

			// Assert
			Assert.IsFalse(success);
		}

		[TestMethod]
		public void ShouldFailOnAddressComparing()
		{
			// Arrange
			var input1 = new DiscreteInput { Address = 123, HighByte = 0xFF, LowByte = 0x00 };
			var input2 = new DiscreteInput { Address = 321, HighByte = 0xFF, LowByte = 0x00 };

			// Act
			bool success = input1.Equals(input2);

			// Assert
			Assert.IsFalse(success);
		}

		[TestMethod]
		public void ShouldFailOnHighByteComparing()
		{
			// Arrange
			var input1 = new DiscreteInput { Address = 123, HighByte = 0xFF, LowByte = 0x00 };
			var input2 = new DiscreteInput { Address = 123, HighByte = 0x00, LowByte = 0x00 };

			// Act
			bool success = input1.Equals(input2);

			// Assert
			Assert.IsFalse(success);
		}

		[TestMethod]
		public void ShouldFailOnLowByteComparing()
		{
			// Arrange
			var input1 = new DiscreteInput { Address = 123, HighByte = 0xFF, LowByte = 0x00 };
			var input2 = new DiscreteInput { Address = 123, HighByte = 0xFF, LowByte = 0xFF };

			// Act
			bool success = input1.Equals(input2);

			// Assert
			Assert.IsFalse(success);
		}

		[DataTestMethod]
		[DataRow(0xFF)]
		[DataRow(0x00)]
		public void ShouldPrintPrettyString(int highByte)
		{
			// Arrange
			var input = new DiscreteInput { Address = 123, HighByte = (byte)highByte, LowByte = 0x00 };

			// Act
			string str = input.ToString();

			// Assert
			if (highByte > 0)
				Assert.AreEqual("Discrete Input #123 | ON", str);
			else
				Assert.AreEqual("Discrete Input #123 | OFF", str);
		}
	}
}
