using AMWD.Protocols.Modbus.Tcp;
using Moq;

namespace AMWD.Protocols.Modbus.Tests.Tcp
{
	[TestClass]
	public class ModbusTcpClientTest
	{
		private Mock<IModbusConnection> _genericConnectionMock;
		private Mock<ModbusTcpConnection> _tcpConnectionMock;

		[TestInitialize]
		public void Initialize()
		{
			_genericConnectionMock = new Mock<IModbusConnection>();
			_genericConnectionMock.Setup(c => c.IdleTimeout).Returns(TimeSpan.FromSeconds(40));
			_genericConnectionMock.Setup(c => c.ConnectTimeout).Returns(TimeSpan.FromSeconds(30));
			_genericConnectionMock.Setup(c => c.ReadTimeout).Returns(TimeSpan.FromSeconds(20));
			_genericConnectionMock.Setup(c => c.WriteTimeout).Returns(TimeSpan.FromSeconds(10));

			_tcpConnectionMock = new Mock<ModbusTcpConnection>();

			_tcpConnectionMock.Setup(c => c.IdleTimeout).Returns(TimeSpan.FromSeconds(10));
			_tcpConnectionMock.Setup(c => c.ConnectTimeout).Returns(TimeSpan.FromSeconds(20));
			_tcpConnectionMock.Setup(c => c.ReadTimeout).Returns(TimeSpan.FromSeconds(30));
			_tcpConnectionMock.Setup(c => c.WriteTimeout).Returns(TimeSpan.FromSeconds(40));

			_tcpConnectionMock.Setup(c => c.Hostname).Returns("127.0.0.1");
			_tcpConnectionMock.Setup(c => c.Port).Returns(502);
		}

		[TestMethod]
		public void ShouldReturnDefaultValuesForGenericConnection()
		{
			// Arrange
			var client = new ModbusTcpClient(_genericConnectionMock.Object);

			// Act
			string hostname = client.Hostname;
			int port = client.Port;

			// Assert
			Assert.IsNull(hostname);
			Assert.AreEqual(0, port);

			_genericConnectionMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldNotSetValuesForGenericConnection()
		{
			// Arrange
			var client = new ModbusTcpClient(_genericConnectionMock.Object);

			// Act
			client.Hostname = "localhost";
			client.Port = 205;

			// Assert
			_genericConnectionMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldReturnValuesForGenericConnection()
		{
			// Arrange
			var client = new ModbusTcpClient(_genericConnectionMock.Object);

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
			var client = new ModbusTcpClient(_genericConnectionMock.Object);

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
		public void ShouldGetValuesForTcpConnection()
		{
			// Arrange
			var client = new ModbusTcpClient(_tcpConnectionMock.Object);

			// Act
			string hostname = client.Hostname;
			int port = client.Port;
			var idleTimeout = client.IdleTimeout;
			var connectTimeout = client.ConnectTimeout;
			var readTimeout = client.ReadTimeout;
			var writeTimeout = client.WriteTimeout;

			// Assert
			Assert.AreEqual("127.0.0.1", hostname);
			Assert.AreEqual(502, port);
			Assert.AreEqual(TimeSpan.FromSeconds(10), idleTimeout);
			Assert.AreEqual(TimeSpan.FromSeconds(20), connectTimeout);
			Assert.AreEqual(TimeSpan.FromSeconds(30), readTimeout);
			Assert.AreEqual(TimeSpan.FromSeconds(40), writeTimeout);

			_tcpConnectionMock.VerifyGet(c => c.Hostname, Times.Once);
			_tcpConnectionMock.VerifyGet(c => c.Port, Times.Once);
			_tcpConnectionMock.VerifyGet(c => c.IdleTimeout, Times.Once);
			_tcpConnectionMock.VerifyGet(c => c.ConnectTimeout, Times.Once);
			_tcpConnectionMock.VerifyGet(c => c.ReadTimeout, Times.Once);
			_tcpConnectionMock.VerifyGet(c => c.WriteTimeout, Times.Once);

			_tcpConnectionMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldSetValuesForTcpConnection()
		{
			// Arrange
			var client = new ModbusTcpClient(_tcpConnectionMock.Object);

			// Act
			client.Hostname = "localhost";
			client.Port = 205;
			client.IdleTimeout = TimeSpan.FromSeconds(40);
			client.ConnectTimeout = TimeSpan.FromSeconds(30);
			client.ReadTimeout = TimeSpan.FromSeconds(20);
			client.WriteTimeout = TimeSpan.FromSeconds(10);

			// Assert
			_tcpConnectionMock.VerifySet(c => c.Hostname = "localhost", Times.Once);
			_tcpConnectionMock.VerifySet(c => c.Port = 205, Times.Once);
			_tcpConnectionMock.VerifySet(c => c.IdleTimeout = TimeSpan.FromSeconds(40), Times.Once);
			_tcpConnectionMock.VerifySet(c => c.ConnectTimeout = TimeSpan.FromSeconds(30), Times.Once);
			_tcpConnectionMock.VerifySet(c => c.ReadTimeout = TimeSpan.FromSeconds(20), Times.Once);
			_tcpConnectionMock.VerifySet(c => c.WriteTimeout = TimeSpan.FromSeconds(10), Times.Once);

			_tcpConnectionMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldPrintCleanString()
		{
			// Arrange
			using var client = new ModbusTcpClient(_tcpConnectionMock.Object);

			// Act
			string str = client.ToString();

			// Assert
			SnapshotAssert.AreEqual(str);
		}
	}
}
