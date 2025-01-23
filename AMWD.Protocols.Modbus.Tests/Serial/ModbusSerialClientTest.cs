using System.IO.Ports;
using AMWD.Protocols.Modbus.Serial;
using Moq;

namespace AMWD.Protocols.Modbus.Tests.Serial
{
	[TestClass]
	public class ModbusSerialClientTest
	{
		private Mock<IModbusConnection> _genericConnectionMock;
		private Mock<ModbusSerialConnection> _serialConnectionMock;

		[TestInitialize]
		public void Initialize()
		{
			string portName = "COM-42";

			_genericConnectionMock = new Mock<IModbusConnection>();
			_genericConnectionMock.Setup(c => c.IdleTimeout).Returns(TimeSpan.FromSeconds(40));
			_genericConnectionMock.Setup(c => c.ConnectTimeout).Returns(TimeSpan.FromSeconds(30));
			_genericConnectionMock.Setup(c => c.ReadTimeout).Returns(TimeSpan.FromSeconds(20));
			_genericConnectionMock.Setup(c => c.WriteTimeout).Returns(TimeSpan.FromSeconds(10));

			_serialConnectionMock = new Mock<ModbusSerialConnection>(portName);

			_serialConnectionMock.Setup(c => c.IdleTimeout).Returns(TimeSpan.FromSeconds(10));
			_serialConnectionMock.Setup(c => c.ConnectTimeout).Returns(TimeSpan.FromSeconds(20));
			_serialConnectionMock.Setup(c => c.ReadTimeout).Returns(TimeSpan.FromSeconds(30));
			_serialConnectionMock.Setup(c => c.WriteTimeout).Returns(TimeSpan.FromSeconds(40));

			_serialConnectionMock.Setup(c => c.DriverEnabledRS485).Returns(true);
			_serialConnectionMock.Setup(c => c.InterRequestDelay).Returns(TimeSpan.FromSeconds(50));
			_serialConnectionMock.Setup(c => c.PortName).Returns(portName);
			_serialConnectionMock.Setup(c => c.BaudRate).Returns(BaudRate.Baud2400);
			_serialConnectionMock.Setup(c => c.DataBits).Returns(7);
			_serialConnectionMock.Setup(c => c.Handshake).Returns(Handshake.XOnXOff);
			_serialConnectionMock.Setup(c => c.Parity).Returns(Parity.Space);
			_serialConnectionMock.Setup(c => c.RtsEnable).Returns(true);
			_serialConnectionMock.Setup(c => c.StopBits).Returns(StopBits.OnePointFive);
		}

		[TestMethod]
		public void ShouldReturnDefaultValuesForGenericConnection()
		{
			// Arrange
			var client = new ModbusSerialClient(_genericConnectionMock.Object);

			// Act
			bool driverEnabled = client.DriverEnabledRS485;
			var requestDelay = client.InterRequestDelay;
			string portName = client.PortName;
			var baudRate = client.BaudRate;
			int dataBits = client.DataBits;
			var handshake = client.Handshake;
			var parity = client.Parity;
			bool rtsEnable = client.RtsEnable;
			var stopBits = client.StopBits;

			// Assert
			Assert.IsFalse(driverEnabled);
			Assert.AreEqual(TimeSpan.Zero, requestDelay);
			Assert.IsNull(portName);
			Assert.AreEqual(0, (int)baudRate);
			Assert.AreEqual(0, dataBits);
			Assert.AreEqual(0, (int)handshake);
			Assert.AreEqual(0, (int)parity);
			Assert.IsFalse(rtsEnable);
			Assert.AreEqual(0, (int)stopBits);

			_genericConnectionMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldNotSetValuesForGenericConnection()
		{
			// Arrange
			var client = new ModbusSerialClient(_genericConnectionMock.Object);

			// Act
			client.DriverEnabledRS485 = true;
			client.InterRequestDelay = TimeSpan.FromSeconds(123);
			client.PortName = "COM-42";
			client.BaudRate = BaudRate.Baud2400;
			client.DataBits = 7;
			client.Handshake = Handshake.XOnXOff;
			client.Parity = Parity.Space;
			client.RtsEnable = true;
			client.StopBits = StopBits.OnePointFive;

			// Assert
			_genericConnectionMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldReturnValuesForGenericConnection()
		{
			// Arrange
			var client = new ModbusSerialClient(_genericConnectionMock.Object);

			// Act
			var idleTimeout = client.IdleTimeout;
			var connectTimeout = client.ConnectTimeout;
			var readTimeout = client.ReadTimeout;
			var writeTimeout = client.WriteTimeout;

			// Assert
			Assert.AreEqual(TimeSpan.FromSeconds(40), idleTimeout);
			Assert.AreEqual(TimeSpan.FromSeconds(30), connectTimeout);
			Assert.AreEqual(TimeSpan.FromSeconds(20), readTimeout);
			Assert.AreEqual(TimeSpan.FromSeconds(10), writeTimeout);

			_genericConnectionMock.VerifyGet(c => c.IdleTimeout, Times.Once);
			_genericConnectionMock.VerifyGet(c => c.ConnectTimeout, Times.Once);
			_genericConnectionMock.VerifyGet(c => c.ReadTimeout, Times.Once);
			_genericConnectionMock.VerifyGet(c => c.WriteTimeout, Times.Once);
			_genericConnectionMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldSetValuesForGenericConnection()
		{
			// Arrange
			var client = new ModbusSerialClient(_genericConnectionMock.Object);

			// Act
			client.IdleTimeout = TimeSpan.FromSeconds(10);
			client.ConnectTimeout = TimeSpan.FromSeconds(20);
			client.ReadTimeout = TimeSpan.FromSeconds(30);
			client.WriteTimeout = TimeSpan.FromSeconds(40);

			// Assert
			_genericConnectionMock.VerifySet(c => c.IdleTimeout = TimeSpan.FromSeconds(10), Times.Once);
			_genericConnectionMock.VerifySet(c => c.ConnectTimeout = TimeSpan.FromSeconds(20), Times.Once);
			_genericConnectionMock.VerifySet(c => c.ReadTimeout = TimeSpan.FromSeconds(30), Times.Once);
			_genericConnectionMock.VerifySet(c => c.WriteTimeout = TimeSpan.FromSeconds(40), Times.Once);

			_genericConnectionMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldGetValuesForSerialConnection()
		{
			// Arrange
			var client = new ModbusSerialClient(_serialConnectionMock.Object);

			// Act
			bool driverEnabled = client.DriverEnabledRS485;
			var requestDelay = client.InterRequestDelay;
			string portName = client.PortName;
			var baudRate = client.BaudRate;
			int dataBits = client.DataBits;
			var handshake = client.Handshake;
			var parity = client.Parity;
			bool rtsEnable = client.RtsEnable;
			var stopBits = client.StopBits;

			var idleTimeout = client.IdleTimeout;
			var connectTimeout = client.ConnectTimeout;
			var readTimeout = client.ReadTimeout;
			var writeTimeout = client.WriteTimeout;

			// Assert
			Assert.IsTrue(driverEnabled);
			Assert.AreEqual(TimeSpan.FromSeconds(50), requestDelay);
			Assert.AreEqual("COM-42", portName);
			Assert.AreEqual(BaudRate.Baud2400, baudRate);
			Assert.AreEqual(7, dataBits);
			Assert.AreEqual(Handshake.XOnXOff, handshake);
			Assert.AreEqual(Parity.Space, parity);
			Assert.IsTrue(rtsEnable);
			Assert.AreEqual(StopBits.OnePointFive, stopBits);

			Assert.AreEqual(TimeSpan.FromSeconds(10), idleTimeout);
			Assert.AreEqual(TimeSpan.FromSeconds(20), connectTimeout);
			Assert.AreEqual(TimeSpan.FromSeconds(30), readTimeout);
			Assert.AreEqual(TimeSpan.FromSeconds(40), writeTimeout);

			_serialConnectionMock.VerifyGet(c => c.DriverEnabledRS485, Times.Once);
			_serialConnectionMock.VerifyGet(c => c.InterRequestDelay, Times.Once);
			_serialConnectionMock.VerifyGet(c => c.PortName, Times.Once);
			_serialConnectionMock.VerifyGet(c => c.BaudRate, Times.Once);
			_serialConnectionMock.VerifyGet(c => c.DataBits, Times.Once);
			_serialConnectionMock.VerifyGet(c => c.Handshake, Times.Once);
			_serialConnectionMock.VerifyGet(c => c.Parity, Times.Once);
			_serialConnectionMock.VerifyGet(c => c.RtsEnable, Times.Once);
			_serialConnectionMock.VerifyGet(c => c.StopBits, Times.Once);

			_serialConnectionMock.VerifyGet(c => c.IdleTimeout, Times.Once);
			_serialConnectionMock.VerifyGet(c => c.ConnectTimeout, Times.Once);
			_serialConnectionMock.VerifyGet(c => c.ReadTimeout, Times.Once);
			_serialConnectionMock.VerifyGet(c => c.WriteTimeout, Times.Once);

			_serialConnectionMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldSetValuesForSerialConnection()
		{
			// Arrange
			var client = new ModbusSerialClient(_serialConnectionMock.Object);

			// Act
			client.DriverEnabledRS485 = true;
			client.InterRequestDelay = TimeSpan.FromSeconds(123);
			client.PortName = "COM-42";
			client.BaudRate = BaudRate.Baud2400;
			client.DataBits = 7;
			client.Handshake = Handshake.XOnXOff;
			client.Parity = Parity.Space;
			client.RtsEnable = true;
			client.StopBits = StopBits.OnePointFive;

			client.IdleTimeout = TimeSpan.FromSeconds(40);
			client.ConnectTimeout = TimeSpan.FromSeconds(30);
			client.ReadTimeout = TimeSpan.FromSeconds(20);
			client.WriteTimeout = TimeSpan.FromSeconds(10);

			// Assert
			_serialConnectionMock.VerifySet(c => c.DriverEnabledRS485 = true, Times.Once);
			_serialConnectionMock.VerifySet(c => c.InterRequestDelay = TimeSpan.FromSeconds(123), Times.Once);
			_serialConnectionMock.VerifySet(c => c.PortName = "COM-42", Times.Once);
			_serialConnectionMock.VerifySet(c => c.BaudRate = BaudRate.Baud2400, Times.Once);
			_serialConnectionMock.VerifySet(c => c.DataBits = 7, Times.Once);
			_serialConnectionMock.VerifySet(c => c.Handshake = Handshake.XOnXOff, Times.Once);
			_serialConnectionMock.VerifySet(c => c.Parity = Parity.Space, Times.Once);
			_serialConnectionMock.VerifySet(c => c.RtsEnable = true, Times.Once);
			_serialConnectionMock.VerifySet(c => c.StopBits = StopBits.OnePointFive, Times.Once);

			_serialConnectionMock.VerifySet(c => c.IdleTimeout = TimeSpan.FromSeconds(40), Times.Once);
			_serialConnectionMock.VerifySet(c => c.ConnectTimeout = TimeSpan.FromSeconds(30), Times.Once);
			_serialConnectionMock.VerifySet(c => c.ReadTimeout = TimeSpan.FromSeconds(20), Times.Once);
			_serialConnectionMock.VerifySet(c => c.WriteTimeout = TimeSpan.FromSeconds(10), Times.Once);

			_serialConnectionMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldPrintCleanString()
		{
			// Arrange
			using var client = new ModbusSerialClient(_serialConnectionMock.Object);

			// Act
			string str = client.ToString();

			// Assert
			SnapshotAssert.AreEqual(str);
		}
	}
}
