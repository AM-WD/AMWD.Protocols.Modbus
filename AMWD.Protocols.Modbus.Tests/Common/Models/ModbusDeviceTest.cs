using System.Collections.Generic;
using System.Reflection;
using AMWD.Protocols.Modbus.Common.Models;

namespace AMWD.Protocols.Modbus.Tests.Common.Models
{
	[TestClass]
	public class ModbusDeviceTest
	{
		[TestMethod]
		public void ShouldAllowMultipleDispose()
		{
			// Arrange
			var device = new ModbusDevice(123);

			// Act
			device.Dispose();
			device.Dispose();

			// Assert - no exception
		}

		[TestMethod]
		public void ShouldAssertDisposed()
		{
			// Arrange
			var device = new ModbusDevice(123);
			device.Dispose();

			// Act
			try
			{
				device.GetCoil(111);
				Assert.Fail();
			}
			catch (ObjectDisposedException)
			{ }

			try
			{
				device.SetCoil(new Coil { Address = 222 });
				Assert.Fail();
			}
			catch (ObjectDisposedException)
			{ }

			try
			{
				device.GetDiscreteInput(111);
				Assert.Fail();
			}
			catch (ObjectDisposedException)
			{ }

			try
			{
				device.SetDiscreteInput(new DiscreteInput { Address = 222 });
				Assert.Fail();
			}
			catch (ObjectDisposedException)
			{ }

			try
			{
				device.GetHoldingRegister(111);
				Assert.Fail();
			}
			catch (ObjectDisposedException)
			{ }

			try
			{
				device.SetHoldingRegister(new HoldingRegister { Address = 222 });
				Assert.Fail();
			}
			catch (ObjectDisposedException)
			{ }

			try
			{
				device.GetInputRegister(111);
				Assert.Fail();
			}
			catch (ObjectDisposedException)
			{ }

			try
			{
				device.SetInputRegister(new InputRegister { Address = 222 });
				Assert.Fail();
			}
			catch (ObjectDisposedException)
			{ }
		}

		[TestMethod]
		public void ShouldGetCoil()
		{
			// Arrange
			var device = new ModbusDevice(123);
			((HashSet<ushort>)device.GetType()
					.GetField("_coils", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(device))
				.Add(333);

			// Act
			var coilFalse = device.GetCoil(111);
			var coilTrue = device.GetCoil(333);

			// Assert
			Assert.AreEqual(111, coilFalse.Address);
			Assert.IsFalse(coilFalse.Value);

			Assert.AreEqual(333, coilTrue.Address);
			Assert.IsTrue(coilTrue.Value);
		}

		[TestMethod]
		public void ShouldSetCoil()
		{
			// Arrange
			var device = new ModbusDevice(123);
			((HashSet<ushort>)device.GetType()
					.GetField("_coils", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(device))
				.Add(333);

			// Act
			device.SetCoil(new Coil { Address = 111, Value = true });
			device.SetCoil(new Coil { Address = 333, Value = false });

			// Assert
			ushort[] coils = ((HashSet<ushort>)device.GetType()
					.GetField("_coils", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(device)).ToArray();

			Assert.AreEqual(1, coils.Length);
			Assert.AreEqual(111, coils.First());
		}

		[TestMethod]
		public void ShouldGetDiscreteInput()
		{
			// Arrange
			var device = new ModbusDevice(123);
			((HashSet<ushort>)device.GetType()
					.GetField("_discreteInputs", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(device))
				.Add(333);

			// Act
			var inputFalse = device.GetDiscreteInput(111);
			var inputTrue = device.GetDiscreteInput(333);

			// Assert
			Assert.AreEqual(111, inputFalse.Address);
			Assert.IsFalse(inputFalse.Value);

			Assert.AreEqual(333, inputTrue.Address);
			Assert.IsTrue(inputTrue.Value);
		}

		[TestMethod]
		public void ShouldSetDiscreteInput()
		{
			// Arrange
			var device = new ModbusDevice(123);
			((HashSet<ushort>)device.GetType()
					.GetField("_discreteInputs", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(device))
				.Add(333);

			// Act
			device.SetDiscreteInput(new DiscreteInput { Address = 111, HighByte = 0xFF });
			device.SetDiscreteInput(new DiscreteInput { Address = 333, HighByte = 0x00 });

			// Assert
			ushort[] discreteInputs = ((HashSet<ushort>)device.GetType()
					.GetField("_discreteInputs", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(device)).ToArray();

			Assert.AreEqual(1, discreteInputs.Length);
			Assert.AreEqual(111, discreteInputs.First());
		}

		[TestMethod]
		public void ShouldGetHoldingRegister()
		{
			// Arrange
			var device = new ModbusDevice(123);
			((Dictionary<ushort, ushort>)device.GetType()
					.GetField("_holdingRegisters", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(device))
				.Add(333, 42);

			// Act
			var zeroRegister = device.GetHoldingRegister(111);
			var valueRegister = device.GetHoldingRegister(333);

			// Assert
			Assert.AreEqual(111, zeroRegister.Address);
			Assert.AreEqual(0, zeroRegister.Value);
			Assert.AreEqual(0x00, zeroRegister.HighByte);
			Assert.AreEqual(0x00, zeroRegister.LowByte);

			Assert.AreEqual(333, valueRegister.Address);
			Assert.AreEqual(42, valueRegister.Value);
			Assert.AreEqual(0x00, valueRegister.HighByte);
			Assert.AreEqual(0x2A, valueRegister.LowByte);
		}

		[TestMethod]
		public void ShouldSetHoldingRegister()
		{
			// Arrange
			var device = new ModbusDevice(123);
			((Dictionary<ushort, ushort>)device.GetType()
					.GetField("_holdingRegisters", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(device))
				.Add(333, 42);

			// Act
			device.SetHoldingRegister(new HoldingRegister { Address = 333, Value = 0 });
			device.SetHoldingRegister(new HoldingRegister { Address = 111, Value = 42 });

			// Assert
			var registers = ((Dictionary<ushort, ushort>)device.GetType()
					.GetField("_holdingRegisters", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(device))
					.ToDictionary(x => x.Key, x => x.Value);

			Assert.AreEqual(1, registers.Count);
			Assert.AreEqual(111, registers.First().Key);
			Assert.AreEqual(42, registers.First().Value);
		}

		[TestMethod]
		public void ShouldGetInputRegister()
		{
			// Arrange
			var device = new ModbusDevice(123);
			((Dictionary<ushort, ushort>)device.GetType()
					.GetField("_inputRegisters", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(device))
				.Add(333, 42);

			// Act
			var zeroRegister = device.GetInputRegister(111);
			var valueRegister = device.GetInputRegister(333);

			// Assert
			Assert.AreEqual(111, zeroRegister.Address);
			Assert.AreEqual(0, zeroRegister.Value);
			Assert.AreEqual(0x00, zeroRegister.HighByte);
			Assert.AreEqual(0x00, zeroRegister.LowByte);

			Assert.AreEqual(333, valueRegister.Address);
			Assert.AreEqual(42, valueRegister.Value);
			Assert.AreEqual(0x00, valueRegister.HighByte);
			Assert.AreEqual(0x2A, valueRegister.LowByte);
		}

		[TestMethod]
		public void ShouldSetInputRegister()
		{
			// Arrange
			var device = new ModbusDevice(123);
			((Dictionary<ushort, ushort>)device.GetType()
					.GetField("_inputRegisters", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(device))
				.Add(333, 42);

			// Act
			device.SetInputRegister(new InputRegister { Address = 333, LowByte = 0 });
			device.SetInputRegister(new InputRegister { Address = 111, LowByte = 42 });

			// Assert
			var registers = ((Dictionary<ushort, ushort>)device.GetType()
					.GetField("_inputRegisters", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(device))
					.ToDictionary(x => x.Key, x => x.Value);

			Assert.AreEqual(1, registers.Count);
			Assert.AreEqual(111, registers.First().Key);
			Assert.AreEqual(42, registers.First().Value);
		}
	}
}
