using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.Modbus.Common.Contracts;
using AMWD.Protocols.Modbus.Common.Events;
using AMWD.Protocols.Modbus.Common.Models;

namespace AMWD.Protocols.Modbus.Common.Protocols
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal class VirtualProtocol : IModbusProtocol, IDisposable
	{
		#region Fields

		private bool _isDisposed;

		private readonly ReaderWriterLockSlim _deviceListLock = new();
		private readonly Dictionary<byte, ModbusDevice> _devices = [];

		#endregion Fields

		public void Dispose()
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			_deviceListLock.Dispose();

			foreach (var device in _devices.Values)
				device.Dispose();

			_devices.Clear();
		}

		#region Events

		public event EventHandler<CoilWrittenEventArgs> CoilWritten;

		public event EventHandler<RegisterWrittenEventArgs> RegisterWritten;

		#endregion Events

		#region Properties

		public string Name => nameof(VirtualProtocol);

		#endregion Properties

		#region Protocol

		public bool CheckResponseComplete(IReadOnlyList<byte> responseBytes) => true;

		public IReadOnlyList<Coil> DeserializeReadCoils(IReadOnlyList<byte> response)
		{
			if (!_devices.TryGetValue(response[0], out var device))
				throw new TimeoutException("Device not found.");

			ushort start = response.GetBigEndianUInt16(1);
			ushort count = response.GetBigEndianUInt16(3);

			return Enumerable.Range(0, count)
				.Select(i => device.GetCoil((ushort)(start + i)))
				.ToList();
		}

		public DeviceIdentificationRaw DeserializeReadDeviceIdentification(IReadOnlyList<byte> response)
		{
			if (!_devices.TryGetValue(response[0], out var _))
				throw new TimeoutException("Device not found.");

			var result = new DeviceIdentificationRaw
			{
				AllowsIndividualAccess = false,
				MoreRequestsNeeded = false,
				Objects = []
			};

			if (response[1] >= 1)
			{
				string version = GetType().Assembly
					.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
					.InformationalVersion;

				result.Objects.Add(0, Encoding.UTF8.GetBytes("AM.WD"));
				result.Objects.Add(1, Encoding.UTF8.GetBytes("AMWD.Protocols.Modbus"));
				result.Objects.Add(2, Encoding.UTF8.GetBytes(version));
			}

			if (response[1] >= 2)
			{
				result.Objects.Add(3, Encoding.UTF8.GetBytes("https://github.com/AM-WD/AMWD.Protocols.Modbus"));
				result.Objects.Add(4, Encoding.UTF8.GetBytes("Modbus Protocol for .NET"));
				result.Objects.Add(5, Encoding.UTF8.GetBytes("Virtual Device"));
				result.Objects.Add(6, Encoding.UTF8.GetBytes("Virtual Modbus Client"));
			}

			if (response[1] >= 3)
			{
				for (int i = 128; i < 256; i++)
					result.Objects.Add((byte)i, []);
			}

			return result;
		}

		public IReadOnlyList<DiscreteInput> DeserializeReadDiscreteInputs(IReadOnlyList<byte> response)
		{
			if (!_devices.TryGetValue(response[0], out var device))
				throw new TimeoutException("Device not found.");

			ushort start = response.GetBigEndianUInt16(1);
			ushort count = response.GetBigEndianUInt16(3);

			return Enumerable.Range(0, count)
				.Select(i => device.GetDiscreteInput((ushort)(start + i)))
				.ToList();
		}

		public IReadOnlyList<HoldingRegister> DeserializeReadHoldingRegisters(IReadOnlyList<byte> response)
		{
			if (!_devices.TryGetValue(response[0], out var device))
				throw new TimeoutException("Device not found.");

			ushort start = response.GetBigEndianUInt16(1);
			ushort count = response.GetBigEndianUInt16(3);

			return Enumerable.Range(0, count)
				.Select(i => device.GetHoldingRegister((ushort)(start + i)))
				.ToList();
		}

		public IReadOnlyList<InputRegister> DeserializeReadInputRegisters(IReadOnlyList<byte> response)
		{
			if (!_devices.TryGetValue(response[0], out var device))
				throw new TimeoutException("Device not found.");

			ushort start = response.GetBigEndianUInt16(1);
			ushort count = response.GetBigEndianUInt16(3);

			return Enumerable.Range(0, count)
				.Select(i => device.GetInputRegister((ushort)(start + i)))
				.ToList();
		}

		public (ushort FirstAddress, ushort NumberOfCoils) DeserializeWriteMultipleCoils(IReadOnlyList<byte> response)
		{
			if (!_devices.TryGetValue(response[0], out var device))
				throw new TimeoutException("Device not found.");

			ushort start = response.GetBigEndianUInt16(1);
			ushort count = response.GetBigEndianUInt16(3);

			for (int i = 0; i < count; i++)
			{
				var coil = new Coil
				{
					Address = (ushort)(start + i),
					HighByte = response[5 + i]
				};
				device.SetCoil(coil);

				Task.Run(() =>
				{
					try
					{
						CoilWritten?.Invoke(this, new CoilWrittenEventArgs(
							unitId: response[0],
							address: coil.Address,
							value: coil.Value));
					}
					catch
					{
						// ignore
					}
				});
			}

			return (start, count);
		}

		public (ushort FirstAddress, ushort NumberOfRegisters) DeserializeWriteMultipleHoldingRegisters(IReadOnlyList<byte> response)
		{
			if (!_devices.TryGetValue(response[0], out var device))
				throw new TimeoutException("Device not found.");

			ushort start = response.GetBigEndianUInt16(1);
			ushort count = response.GetBigEndianUInt16(3);

			for (int i = 0; i < count; i++)
			{
				var register = new HoldingRegister
				{
					Address = (ushort)(start + i),
					HighByte = response[5 + i * 2],
					LowByte = response[5 + i * 2 + 1]
				};
				device.SetHoldingRegister(register);

				Task.Run(() =>
				{
					try
					{
						RegisterWritten?.Invoke(this, new RegisterWrittenEventArgs(
							unitId: response[0],
							address: register.Address,
							highByte: register.HighByte,
							lowByte: register.LowByte));
					}
					catch
					{
						// ignore
					}
				});
			}

			return (start, count);
		}

		public Coil DeserializeWriteSingleCoil(IReadOnlyList<byte> response)
		{
			if (!_devices.TryGetValue(response[0], out var device))
				throw new TimeoutException("Device not found.");

			var coil = new Coil
			{
				Address = response.GetBigEndianUInt16(1),
				HighByte = response[3]
			};
			device.SetCoil(coil);

			Task.Run(() =>
			{
				try
				{
					CoilWritten?.Invoke(this, new CoilWrittenEventArgs(
						unitId: response[0],
						address: coil.Address,
						value: coil.Value));
				}
				catch
				{
					// ignore
				}
			});

			return coil;
		}

		public HoldingRegister DeserializeWriteSingleHoldingRegister(IReadOnlyList<byte> response)
		{
			if (!_devices.TryGetValue(response[0], out var device))
				throw new TimeoutException("Device not found.");

			var register = new HoldingRegister
			{
				Address = response.GetBigEndianUInt16(1),
				HighByte = response[3],
				LowByte = response[4]
			};
			device.SetHoldingRegister(register);

			Task.Run(() =>
			{
				try
				{
					RegisterWritten?.Invoke(this, new RegisterWrittenEventArgs(
						unitId: response[0],
						address: register.Address,
						highByte: register.HighByte,
						lowByte: register.LowByte));
				}
				catch
				{
					// ignore
				}
			});

			return register;
		}

		public IReadOnlyList<byte> SerializeReadCoils(byte unitId, ushort startAddress, ushort count)
		{
			return [unitId, .. startAddress.ToBigEndianBytes(), .. count.ToBigEndianBytes()];
		}

		public IReadOnlyList<byte> SerializeReadDeviceIdentification(byte unitId, ModbusDeviceIdentificationCategory category, ModbusDeviceIdentificationObject objectId)
		{
			if (!Enum.IsDefined(typeof(ModbusDeviceIdentificationCategory), category))
				throw new ArgumentOutOfRangeException(nameof(category));

			return [unitId, (byte)category];
		}

		public IReadOnlyList<byte> SerializeReadDiscreteInputs(byte unitId, ushort startAddress, ushort count)
		{
			return [unitId, .. startAddress.ToBigEndianBytes(), .. count.ToBigEndianBytes()];
		}

		public IReadOnlyList<byte> SerializeReadHoldingRegisters(byte unitId, ushort startAddress, ushort count)
		{
			return [unitId, .. startAddress.ToBigEndianBytes(), .. count.ToBigEndianBytes()];
		}

		public IReadOnlyList<byte> SerializeReadInputRegisters(byte unitId, ushort startAddress, ushort count)
		{
			return [unitId, .. startAddress.ToBigEndianBytes(), .. count.ToBigEndianBytes()];
		}

		public IReadOnlyList<byte> SerializeWriteMultipleCoils(byte unitId, IReadOnlyList<Coil> coils)
		{
			ushort start = coils.OrderBy(c => c.Address).First().Address;
			ushort count = (ushort)coils.Count;
			byte[] values = coils.Select(c => c.HighByte).ToArray();

			return [unitId, .. start.ToBigEndianBytes(), .. count.ToBigEndianBytes(), .. values];
		}

		public IReadOnlyList<byte> SerializeWriteMultipleHoldingRegisters(byte unitId, IReadOnlyList<HoldingRegister> registers)
		{
			ushort start = registers.OrderBy(c => c.Address).First().Address;
			ushort count = (ushort)registers.Count;
			byte[] values = registers.SelectMany(r => new[] { r.HighByte, r.LowByte }).ToArray();

			return [unitId, .. start.ToBigEndianBytes(), .. count.ToBigEndianBytes(), .. values];
		}

		public IReadOnlyList<byte> SerializeWriteSingleCoil(byte unitId, Coil coil)
		{
			return [unitId, .. coil.Address.ToBigEndianBytes(), coil.HighByte];
		}

		public IReadOnlyList<byte> SerializeWriteSingleHoldingRegister(byte unitId, HoldingRegister register)
		{
			return [unitId, .. register.Address.ToBigEndianBytes(), register.HighByte, register.LowByte];
		}

		public void ValidateResponse(IReadOnlyList<byte> request, IReadOnlyList<byte> response)
		{
			if (!request.SequenceEqual(response))
				throw new InvalidOperationException("Request and response have to be the same on virtual protocol.");
		}

		#endregion Protocol

		#region Device Handling

		public bool AddDevice(byte unitId)
		{
			Assertions();
			using (_deviceListLock.GetWriteLock())
			{
				if (_devices.ContainsKey(unitId))
					return false;

				_devices.Add(unitId, new ModbusDevice(unitId));
				return true;
			}
		}

		public bool RemoveDevice(byte unitId)
		{
			Assertions();
			using (_deviceListLock.GetWriteLock())
			{
				if (_devices.ContainsKey(unitId))
					return false;

				return _devices.Remove(unitId);
			}
		}

		#endregion Device Handling

		#region Entity Handling

		public Coil GetCoil(byte unitId, ushort address)
		{
			Assertions();
			using (_deviceListLock.GetReadLock())
			{
				return _devices.TryGetValue(unitId, out var device)
					? device.GetCoil(address)
					: null;
			}
		}

		public void SetCoil(byte unitId, Coil coil)
		{
			Assertions();
			using (_deviceListLock.GetWriteLock())
			{
				if (_devices.TryGetValue(unitId, out var device))
					device.SetCoil(coil);
			}
		}

		public DiscreteInput GetDiscreteInput(byte unitId, ushort address)
		{
			Assertions();
			using (_deviceListLock.GetReadLock())
			{
				return _devices.TryGetValue(unitId, out var device)
					? device.GetDiscreteInput(address)
					: null;
			}
		}

		public void SetDiscreteInput(byte unitId, DiscreteInput discreteInput)
		{
			Assertions();
			using (_deviceListLock.GetWriteLock())
			{
				if (_devices.TryGetValue(unitId, out var device))
					device.SetDiscreteInput(discreteInput);
			}
		}

		public HoldingRegister GetHoldingRegister(byte unitId, ushort address)
		{
			Assertions();
			using (_deviceListLock.GetReadLock())
			{
				return _devices.TryGetValue(unitId, out var device)
					? device.GetHoldingRegister(address)
					: null;
			}
		}

		public void SetHoldingRegister(byte unitId, HoldingRegister holdingRegister)
		{
			Assertions();
			using (_deviceListLock.GetWriteLock())
			{
				if (_devices.TryGetValue(unitId, out var device))
					device.SetHoldingRegister(holdingRegister);
			}
		}

		public InputRegister GetInputRegister(byte unitId, ushort address)
		{
			Assertions();
			using (_deviceListLock.GetReadLock())
			{
				return _devices.TryGetValue(unitId, out var device)
					? device.GetInputRegister(address)
					: null;
			}
		}

		public void SetInputRegister(byte unitId, InputRegister inputRegister)
		{
			Assertions();
			using (_deviceListLock.GetWriteLock())
			{
				if (_devices.TryGetValue(unitId, out var device))
					device.SetInputRegister(inputRegister);
			}
		}

		#endregion Entity Handling

		private void Assertions()
		{
#if NET8_0_OR_GREATER
			ObjectDisposedException.ThrowIf(_isDisposed, this);
#else
			if (_isDisposed)
				throw new ObjectDisposedException(GetType().Name);
#endif
		}
	}
}
