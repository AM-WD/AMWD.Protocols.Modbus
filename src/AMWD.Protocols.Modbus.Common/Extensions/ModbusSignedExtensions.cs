using System;
using System.Collections.Generic;
using System.Linq;

namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// Custom extensions for <see cref="ModbusObject"/>s.
	/// </summary>
	public static class ModbusSignedExtensions
	{
		/// <summary>
		/// Converts a <see cref="ModbusObject"/> into a <see cref="sbyte"/> value.
		/// </summary>
		/// <param name="obj">The Modbus object.</param>
		/// <returns>The objects signed byte value.</returns>
		/// <exception cref="ArgumentNullException">when the object is null.</exception>
		/// <exception cref="ArgumentException">when the wrong types are provided.</exception>
		public static sbyte GetSByte(this ModbusObject obj)
		{
#if NET8_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(obj);
#else
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));
#endif

			if (obj is HoldingRegister holdingRegister)
				return (sbyte)holdingRegister.Value;

			if (obj is InputRegister inputRegister)
				return (sbyte)inputRegister.Value;

			throw new ArgumentException($"The object type '{obj.GetType()}' is invalid", nameof(obj));
		}

		/// <summary>
		/// Converts a <see cref="ModbusObject"/> into a <see cref="short"/> value.
		/// </summary>
		/// <param name="obj">The Modbus object.</param>
		/// <returns>The objects short value.</returns>
		/// <exception cref="ArgumentNullException">when the object is null.</exception>
		/// <exception cref="ArgumentException">when the wrong types are provided.</exception>
		public static short GetInt16(this ModbusObject obj)
		{
#if NET8_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(obj);
#else
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));
#endif

			if (obj is HoldingRegister holdingRegister)
				return (short)holdingRegister.Value;

			if (obj is InputRegister inputRegister)
				return (short)inputRegister.Value;

			throw new ArgumentException($"The object type '{obj.GetType()}' is invalid", nameof(obj));
		}

		/// <summary>
		/// Converts multiple <see cref="ModbusObject"/>s into a <see cref="int"/> value.
		/// </summary>
		/// <param name="list">The list of Modbus objects.</param>
		/// <param name="startIndex">The first index to use.</param>
		/// <param name="reverseRegisterOrder">Indicates whehter the taken registers should be reversed.</param>
		/// <returns>The objects int value.</returns>
		/// <exception cref="ArgumentNullException">when the list is null.</exception>
		/// <exception cref="ArgumentException">when the list is too short or the list contains mixed/incompatible objects.</exception>
		/// <exception cref="ArgumentOutOfRangeException">when the <paramref name="startIndex"/> is too high.</exception>
		public static int GetInt32(this IEnumerable<ModbusObject> list, int startIndex = 0, bool reverseRegisterOrder = false)
		{
#if NET8_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(list);
#else
			if (list == null)
				throw new ArgumentNullException(nameof(list));
#endif

			int count = list.Count();
			if (count < 2)
				throw new ArgumentException("At least two registers required", nameof(list));

			if (startIndex < 0 || startIndex + 2 > count)
				throw new ArgumentOutOfRangeException(nameof(startIndex));

			if (!list.All(o => o.Type == ModbusObjectType.HoldingRegister) && !list.All(o => o.Type == ModbusObjectType.InputRegister))
				throw new ArgumentException("Mixed object typs found", nameof(list));

			var registers = list.OrderBy(o => o.Address).Skip(startIndex).Take(2).ToArray();
			if (reverseRegisterOrder)
				Array.Reverse(registers);

			byte[] blob = new byte[registers.Length * 2];
			for (int i = 0; i < registers.Length; i++)
			{
				blob[i * 2] = registers[i].HighByte;
				blob[i * 2 + 1] = registers[i].LowByte;
			}

			blob.SwapBigEndian();
			return BitConverter.ToInt32(blob, 0);
		}

		/// <summary>
		/// Converts multiple <see cref="ModbusObject"/>s into a <see cref="long"/> value.
		/// </summary>
		/// <param name="list">The list of Modbus objects.</param>
		/// <param name="startIndex">The first index to use.</param>
		/// <param name="reverseRegisterOrder">Indicates whehter the taken registers should be reversed.</param>
		/// <returns>The objects long value.</returns>
		/// <exception cref="ArgumentNullException">when the list is null.</exception>
		/// <exception cref="ArgumentException">when the list is too short or the list contains mixed/incompatible objects.</exception>
		/// <exception cref="ArgumentOutOfRangeException">when the <paramref name="startIndex"/> is too high.</exception>
		public static long GetInt64(this IEnumerable<ModbusObject> list, int startIndex = 0, bool reverseRegisterOrder = false)
		{
#if NET8_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(list);
#else
			if (list == null)
				throw new ArgumentNullException(nameof(list));
#endif

			int count = list.Count();
			if (count < 4)
				throw new ArgumentException("At least four registers required", nameof(list));

			if (startIndex < 0 || startIndex + 4 > count)
				throw new ArgumentOutOfRangeException(nameof(startIndex));

			if (!list.All(o => o.Type == ModbusObjectType.HoldingRegister) && !list.All(o => o.Type == ModbusObjectType.InputRegister))
				throw new ArgumentException("Mixed object typs found", nameof(list));

			var registers = list.OrderBy(o => o.Address).Skip(startIndex).Take(4).ToArray();
			if (reverseRegisterOrder)
				Array.Reverse(registers);

			byte[] blob = new byte[registers.Length * 2];
			for (int i = 0; i < registers.Length; i++)
			{
				blob[i * 2] = registers[i].HighByte;
				blob[i * 2 + 1] = registers[i].LowByte;
			}

			blob.SwapBigEndian();
			return BitConverter.ToInt64(blob, 0);
		}

		/// <summary>
		/// Converts a <see cref="sbyte"/> value to a <see cref="HoldingRegister"/>.
		/// </summary>
		/// <param name="value">The signed byte value.</param>
		/// <param name="address">The register address.</param>
		/// <returns>The register.</returns>
		public static HoldingRegister ToRegister(this sbyte value, ushort address)
		{
			return new HoldingRegister
			{
				Address = address,
				LowByte = (byte)value
			};
		}

		/// <summary>
		/// Converts a <see cref="short"/> value to a <see cref="HoldingRegister"/>.
		/// </summary>
		/// <param name="value">The short value.</param>
		/// <param name="address">The register address.</param>
		/// <returns>The register.</returns>
		public static HoldingRegister ToRegister(this short value, ushort address)
		{
			byte[] blob = BitConverter.GetBytes(value);
			blob.SwapBigEndian();

			return new HoldingRegister
			{
				Address = address,
				HighByte = blob[0],
				LowByte = blob[1]
			};
		}

		/// <summary>
		/// Converts a <see cref="int"/> value to a list of <see cref="HoldingRegister"/>s.
		/// </summary>
		/// <param name="value">The int value.</param>
		/// <param name="address">The first register address.</param>
		/// <param name="reverseRegisterOrder">Indicates whehter the taken registers should be reversed.</param>
		/// <returns>The list of registers.</returns>
		public static IEnumerable<HoldingRegister> ToRegister(this int value, ushort address, bool reverseRegisterOrder = false)
		{
			byte[] blob = BitConverter.GetBytes(value);
			blob.SwapBigEndian();

			int numRegisters = blob.Length / 2;
			for (int i = 0; i < numRegisters; i++)
			{
				int addr = reverseRegisterOrder
					? address + numRegisters - 1 - i
					: address + i;

				yield return new HoldingRegister
				{
					Address = (ushort)addr,
					HighByte = blob[i * 2],
					LowByte = blob[i * 2 + 1]
				};
			}
		}

		/// <summary>
		/// Converts a <see cref="long"/> value to a list of <see cref="HoldingRegister"/>s.
		/// </summary>
		/// <param name="value">The long value.</param>
		/// <param name="address">The first register address.</param>
		/// <param name="reverseRegisterOrder">Indicates whehter the taken registers should be reversed.</param>
		/// <returns>The list of registers.</returns>
		public static IEnumerable<HoldingRegister> ToRegister(this long value, ushort address, bool reverseRegisterOrder = false)
		{
			byte[] blob = BitConverter.GetBytes(value);
			blob.SwapBigEndian();

			int numRegisters = blob.Length / 2;
			for (int i = 0; i < numRegisters; i++)
			{
				int addr = reverseRegisterOrder
					? address + numRegisters - 1 - i
					: address + i;

				yield return new HoldingRegister
				{
					Address = (ushort)addr,
					HighByte = blob[i * 2],
					LowByte = blob[i * 2 + 1]
				};
			}
		}
	}
}
