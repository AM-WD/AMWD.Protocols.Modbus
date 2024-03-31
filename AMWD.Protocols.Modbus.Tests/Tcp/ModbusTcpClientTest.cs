using AMWD.Protocols.Modbus.Common.Contracts;
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
			_tcpConnectionMock = new Mock<ModbusTcpConnection>();
			_tcpConnectionMock.Setup(c => c.Hostname).Returns("127.0.0.1");
			_tcpConnectionMock.Setup(c => c.Port).Returns(502);
			_tcpConnectionMock.Setup(c => c.ReadTimeout).Returns(TimeSpan.FromSeconds(10));
			_tcpConnectionMock.Setup(c => c.WriteTimeout).Returns(TimeSpan.FromSeconds(20));
			_tcpConnectionMock.Setup(c => c.ConnectTimeout).Returns(TimeSpan.FromSeconds(30));
			_tcpConnectionMock.Setup(c => c.IdleTimeout).Returns(TimeSpan.FromSeconds(40));
		}

		[TestMethod]
		public void ShouldReturnDefaultValuesForGenericConnection()
		{
			// Arrange
			var client = new ModbusTcpClient(_genericConnectionMock.Object);

			// Act
			string hostname = client.Hostname;
			int port = client.Port;
			TimeSpan readTimeout = client.ReadTimeout;
			TimeSpan writeTimeout = client.WriteTimeout;
			TimeSpan reconnectTimeout = client.ReconnectTimeout;
			TimeSpan idleTimeout = client.IdleTimeout;

			// Assert
			Assert.IsNull(hostname);
			Assert.AreEqual(0, port);
			Assert.AreEqual(TimeSpan.Zero, readTimeout);
			Assert.AreEqual(TimeSpan.Zero, writeTimeout);
			Assert.AreEqual(TimeSpan.Zero, reconnectTimeout);
			Assert.AreEqual(TimeSpan.Zero, idleTimeout);

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
			client.ReadTimeout = TimeSpan.FromSeconds(123);
			client.WriteTimeout = TimeSpan.FromSeconds(456);
			client.ReconnectTimeout = TimeSpan.FromSeconds(789);
			client.IdleTimeout = TimeSpan.FromSeconds(321);

			// Assert
			_genericConnectionMock.VerifyNoOtherCalls();
		}

		[TestMethod]
		public void ShouldReturnValuesForTcpConnection()
		{
			// Arrange
			var client = new ModbusTcpClient(_tcpConnectionMock.Object);

			// Act
			string hostname = client.Hostname;
			int port = client.Port;
			TimeSpan readTimeout = client.ReadTimeout;
			TimeSpan writeTimeout = client.WriteTimeout;
			TimeSpan reconnectTimeout = client.ReconnectTimeout;
			TimeSpan keepAliveInterval = client.IdleTimeout;

			// Assert
			Assert.AreEqual("127.0.0.1", hostname);
			Assert.AreEqual(502, port);
			Assert.AreEqual(10, readTimeout.TotalSeconds);
			Assert.AreEqual(20, writeTimeout.TotalSeconds);
			Assert.AreEqual(30, reconnectTimeout.TotalSeconds);
			Assert.AreEqual(40, keepAliveInterval.TotalSeconds);

			_tcpConnectionMock.VerifyGet(c => c.Hostname, Times.Once);
			_tcpConnectionMock.VerifyGet(c => c.Port, Times.Once);
			_tcpConnectionMock.VerifyGet(c => c.ReadTimeout, Times.Once);
			_tcpConnectionMock.VerifyGet(c => c.WriteTimeout, Times.Once);
			_tcpConnectionMock.VerifyGet(c => c.ConnectTimeout, Times.Once);
			_tcpConnectionMock.VerifyGet(c => c.IdleTimeout, Times.Once);
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
			client.ReadTimeout = TimeSpan.FromSeconds(123);
			client.WriteTimeout = TimeSpan.FromSeconds(456);
			client.ReconnectTimeout = TimeSpan.FromSeconds(789);
			client.IdleTimeout = TimeSpan.FromSeconds(321);

			// Assert
			_tcpConnectionMock.VerifySet(c => c.Hostname = "localhost", Times.Once);
			_tcpConnectionMock.VerifySet(c => c.Port = 205, Times.Once);
			_tcpConnectionMock.VerifySet(c => c.ReadTimeout = TimeSpan.FromSeconds(123), Times.Once);
			_tcpConnectionMock.VerifySet(c => c.WriteTimeout = TimeSpan.FromSeconds(456), Times.Once);
			_tcpConnectionMock.VerifySet(c => c.ConnectTimeout = TimeSpan.FromSeconds(789), Times.Once);
			_tcpConnectionMock.VerifySet(c => c.IdleTimeout = TimeSpan.FromSeconds(321), Times.Once);
			_tcpConnectionMock.VerifyNoOtherCalls();
		}
	}
}
