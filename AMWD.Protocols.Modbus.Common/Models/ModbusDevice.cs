using System;
using System.Collections.Generic;
using System.Threading;

namespace AMWD.Protocols.Modbus.Common.Models
{
	/// <summary>
	/// Represents a Modbus device used in a Modbus server implementation.
	/// </summary>
	/// <remarks>
	/// Initializes a new instance of the <see cref="ModbusDevice"/> class.
	/// </remarks>
	/// <param name="id">The <see cref="ModbusDevice"/> ID.</param>
	public class ModbusDevice(byte id) : IDisposable
	{
		private readonly ReaderWriterLockSlim _rwLockCoils = new();
		private readonly ReaderWriterLockSlim _rwLockDiscreteInputs = new();
		private readonly ReaderWriterLockSlim _rwLockHoldingRegisters = new();
		private readonly ReaderWriterLockSlim _rwLockInputRegisters = new();

		private readonly HashSet<ushort> _coils = [];
		private readonly HashSet<ushort> _discreteInputs = [];
		private readonly Dictionary<ushort, ushort> _holdingRegisters = [];
		private readonly Dictionary<ushort, ushort> _inputRegisters = [];

		private bool _isDisposed;

		/// <summary>
		/// Gets the ID of the <see cref="ModbusDevice"/>.
		/// </summary>
		public byte Id { get; } = id;

		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="ModbusDevice"/>
		/// and optionally also discards the managed resources.
		/// </summary>
		public void Dispose()
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			_rwLockCoils.Dispose();
			_rwLockDiscreteInputs.Dispose();
			_rwLockHoldingRegisters.Dispose();
			_rwLockInputRegisters.Dispose();

			_coils.Clear();
			_discreteInputs.Clear();
			_holdingRegisters.Clear();
			_inputRegisters.Clear();
		}

		/// <summary>
		/// Gets a <see cref="Coil"/> from the <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="address">The address of the <see cref="Coil"/>.</param>
		public Coil GetCoil(ushort address)
		{
			Assertions();
			using (_rwLockCoils.GetReadLock())
			{
				return new Coil
				{
					Address = address,
					HighByte = (byte)(_coils.Contains(address) ? 0xFF : 0x00)
				};
			}
		}

		/// <summary>
		/// Sets a <see cref="Coil"/> to the <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="coil">The <see cref="Coil"/> to set.</param>
		public void SetCoil(Coil coil)
		{
			Assertions();
			using (_rwLockCoils.GetWriteLock())
			{
				if (coil.Value)
					_coils.Add(coil.Address);
				else
					_coils.Remove(coil.Address);
			}
		}

		/// <summary>
		/// Gets a <see cref="DiscreteInput"/> from the <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="address">The address of the <see cref="DiscreteInput"/>.</param>
		public DiscreteInput GetDiscreteInput(ushort address)
		{
			Assertions();
			using (_rwLockDiscreteInputs.GetReadLock())
			{
				return new DiscreteInput
				{
					Address = address,
					HighByte = (byte)(_discreteInputs.Contains(address) ? 0xFF : 0x00)
				};
			}
		}

		/// <summary>
		/// Sets a <see cref="DiscreteInput"/> to the <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="input">The <see cref="DiscreteInput"/> to set.</param>
		public void SetDiscreteInput(DiscreteInput input)
		{
			using (_rwLockDiscreteInputs.GetWriteLock())
			{
				if (input.Value)
					_discreteInputs.Add(input.Address);
				else
					_discreteInputs.Remove(input.Address);
			}
		}

		/// <summary>
		/// Gets a <see cref="HoldingRegister"/> from the <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="address">The address of the <see cref="HoldingRegister"/>.</param>
		public HoldingRegister GetHoldingRegister(ushort address)
		{
			Assertions();
			using (_rwLockHoldingRegisters.GetReadLock())
			{
				if (!_holdingRegisters.TryGetValue(address, out ushort value))
					value = 0x0000;

				byte[] blob = BitConverter.GetBytes(value);
				blob.SwapBigEndian();

				return new HoldingRegister
				{
					Address = address,
					HighByte = blob[0],
					LowByte = blob[1]
				};
			}
		}

		/// <summary>
		/// Sets a <see cref="HoldingRegister"/> to the <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="register">The <see cref="HoldingRegister"/> to set.</param>
		public void SetHoldingRegister(HoldingRegister register)
		{
			Assertions();
			using (_rwLockHoldingRegisters.GetWriteLock())
			{
				if (register.Value == 0)
				{
					_holdingRegisters.Remove(register.Address);
					return;
				}

				byte[] blob = [register.HighByte, register.LowByte];
				blob.SwapBigEndian();
				_holdingRegisters[register.Address] = BitConverter.ToUInt16(blob, 0);
			}
		}

		/// <summary>
		/// Gets a <see cref="InputRegister"/> from the <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="address">The address of the <see cref="InputRegister"/>.</param>
		public InputRegister GetInputRegister(ushort address)
		{
			Assertions();
			using (_rwLockInputRegisters.GetReadLock())
			{
				if (!_inputRegisters.TryGetValue(address, out ushort value))
					value = 0x0000;

				byte[] blob = BitConverter.GetBytes(value);
				blob.SwapBigEndian();

				return new InputRegister
				{
					Address = address,
					HighByte = blob[0],
					LowByte = blob[1]
				};
			}
		}

		/// <summary>
		/// Sets a <see cref="InputRegister"/> to the <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="register">The <see cref="InputRegister"/> to set.</param>
		public void SetInputRegister(InputRegister register)
		{
			Assertions();
			using (_rwLockInputRegisters.GetWriteLock())
			{
				if (register.Value == 0)
				{
					_inputRegisters.Remove(register.Address);
					return;
				}

				byte[] blob = [register.HighByte, register.LowByte];
				blob.SwapBigEndian();
				_inputRegisters[register.Address] = BitConverter.ToUInt16(blob, 0);
			}
		}

		private void Assertions()
		{
#if NET8_0_OR_GREATER
			ObjectDisposedException.ThrowIf(_isDisposed, this);
#else
			if (_isDisposed)
				throw new ObjectDisposedException(GetType().FullName);
#endif
		}
	}
}
