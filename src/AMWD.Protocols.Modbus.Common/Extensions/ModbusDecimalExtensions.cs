using System;
using System.Collections.Generic;
using System.Linq;

namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// Custom extensions for <see cref="ModbusObject"/>s.
	/// </summary>
	public static class ModbusDecimalExtensions
	{
		/// <summary>
		/// Converts multiple <see cref="ModbusObject"/>s into a <see cref="float"/> value.
		/// </summary>
		/// <param name="list">The list of Modbus objects.</param>
		/// <param name="startIndex">The first index to use.</param>
		/// <param name="reverseRegisterOrder">Indicates whehter the taken registers should be reversed.</param>
		/// <returns>The objects float value.</returns>
		/// <exception cref="ArgumentNullException">when the list is null.</exception>
		/// <exception cref="ArgumentException">when the list is too short or the list contains mixed/incompatible objects.</exception>
		/// <exception cref="ArgumentOutOfRangeException">when the <paramref name="startIndex"/> is too high.</exception>
		public static float GetSingle(this IEnumerable<ModbusObject> list, int startIndex = 0, bool reverseRegisterOrder = false)
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
			return BitConverter.ToSingle(blob, 0);
		}

		/// <summary>
		/// Converts multiple <see cref="ModbusObject"/>s into a <see cref="double"/> value.
		/// </summary>
		/// <param name="list">The list of Modbus objects.</param>
		/// <param name="startIndex">The first index to use.</param>
		/// <param name="reverseRegisterOrder">Indicates whehter the taken registers should be reversed.</param>
		/// <returns>The objects double value.</returns>
		/// <exception cref="ArgumentNullException">when the list is null.</exception>
		/// <exception cref="ArgumentException">when the list is too short or the list contains mixed/incompatible objects.</exception>
		/// <exception cref="ArgumentOutOfRangeException">when the <paramref name="startIndex"/> is too high.</exception>
		public static double GetDouble(this IEnumerable<ModbusObject> list, int startIndex = 0, bool reverseRegisterOrder = false)
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
			return BitConverter.ToDouble(blob, 0);
		}

		/// <summary>
		/// Converts a <see cref="float"/> value to a list of <see cref="HoldingRegister"/>s.
		/// </summary>
		/// <param name="value">The float value.</param>
		/// <param name="address">The first register address.</param>
		/// <param name="reverseRegisterOrder">Indicates whehter the taken registers should be reversed.</param>
		/// <returns>The list of registers.</returns>
		public static IEnumerable<HoldingRegister> ToRegister(this float value, ushort address, bool reverseRegisterOrder = false)
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
		/// Converts a <see cref="double"/> value to a list of <see cref="HoldingRegister"/>s.
		/// </summary>
		/// <param name="value">The double value.</param>
		/// <param name="address">The first register address.</param>
		/// <param name="reverseRegisterOrder">Indicates whehter the taken registers should be reversed.</param>
		/// <returns>The list of registers.</returns>
		public static IEnumerable<HoldingRegister> ToRegister(this double value, ushort address, bool reverseRegisterOrder = false)
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
