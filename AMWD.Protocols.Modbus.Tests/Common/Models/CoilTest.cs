namespace AMWD.Protocols.Modbus.Tests.Common.Models
{
	[TestClass]
	public class CoilTest
	{
		[TestMethod]
		public void ShouldSuccessfulCompare()
		{
			// Arrange
			var coil1 = new Coil { Address = 123, Value = true };
			var coil2 = new Coil { Address = 123, Value = true };

			// Act
			bool success = coil1.Equals(coil2);

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
			var coil1 = new Coil { Address = 123, Value = true };
			var coil2 = new DiscreteInput { Address = 123, HighByte = 0xFF };

			// Act
			bool success = coil1.Equals(coil2);

			// Assert
			Assert.IsFalse(success);
		}

		[TestMethod]
		public void ShouldFailOnAddressComparing()
		{
			// Arrange
			var coil1 = new Coil { Address = 123, Value = true };
			var coil2 = new Coil { Address = 321, HighByte = 0xFF };

			// Act
			bool success = coil1.Equals(coil2);

			// Assert
			Assert.IsFalse(success);
		}

		[TestMethod]
		public void ShouldFailOnHighByteComparing()
		{
			// Arrange
			var coil1 = new Coil { Address = 123, Value = true };
			var coil2 = new Coil { Address = 123, HighByte = 0x00 };

			// Act
			bool success = coil1.Equals(coil2);

			// Assert
			Assert.IsFalse(success);
		}

		[TestMethod]
		public void ShouldFailOnLowByteComparing()
		{
			// Arrange
			var coil1 = new Coil { Address = 123, Value = true };
			var coil2 = new Coil { Address = 123, HighByte = 0xFF, LowByte = 0xFF };

			// Act
			bool success = coil1.Equals(coil2);

			// Assert
			Assert.IsFalse(success);
		}

		[DataTestMethod]
		[DataRow(0xFF)]
		[DataRow(0x00)]
		public void ShouldPrintPrettyString(int highByte)
		{
			// Arrange
			var coil = new Coil { Address = 123, HighByte = (byte)highByte, LowByte = 0x00 };

			// Act
			string str = coil.ToString();

			// Assert
			if (highByte > 0)
				Assert.AreEqual("Coil #123 | ON", str);
			else
				Assert.AreEqual("Coil #123 | OFF", str);
		}
	}
}
