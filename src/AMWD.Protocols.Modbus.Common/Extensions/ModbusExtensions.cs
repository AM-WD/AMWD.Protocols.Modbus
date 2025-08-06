using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// Custom extensions for <see cref="ModbusObject"/>s.
	/// </summary>
	public static class ModbusExtensions
	{
		/// <summary>
		/// Converts a <see cref="ModbusObject"/> into a <see cref="bool"/> value.
		/// </summary>
		/// <param name="obj">The Modbus object.</param>
		/// <returns>The objects bool value.</returns>
		/// <exception cref="ArgumentNullException">when the object is null.</exception>
		public static bool GetBoolean(this ModbusObject obj)
		{
#if NET8_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(obj);
#else
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));
#endif

			if (obj is Coil coil)
				return coil.Value;

			if (obj is DiscreteInput discreteInput)
				return discreteInput.Value;

			return obj.HighByte > 0 || obj.LowByte > 0;
		}

		/// <summary>
		/// Converts multiple <see cref="ModbusObject"/>s into a <see cref="string"/> value.
		/// </summary>
		/// <param name="list">The list of Modbus objects.</param>
		/// <param name="length">The number of registers to use.</param>
		/// <param name="startIndex">The first index to use.</param>
		/// <param name="encoding">The encoding used to convert the text. (Default: <see cref="Encoding.ASCII"/>)</param>
		/// <param name="reverseRegisterOrder">Indicates whehter the taken registers should be reversed.</param>
		/// <param name="reverseByteOrderPerRegister">Indicates whether to reverse high and low byte per register.</param>
		/// <returns>The objects text value.</returns>
		/// <exception cref="ArgumentNullException">when the list is null.</exception>
		/// <exception cref="ArgumentException">when the list is too short or the list contains mixed/incompatible objects.</exception>
		/// <exception cref="ArgumentOutOfRangeException">when the <paramref name="startIndex"/> is too high.</exception>
		public static string GetString(this IEnumerable<ModbusObject> list, int length, int startIndex = 0, Encoding encoding = null, bool reverseRegisterOrder = false, bool reverseByteOrderPerRegister = false)
		{
#if NET8_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(list);
#else
			if (list == null)
				throw new ArgumentNullException(nameof(list));
#endif

			int count = list.Count();
			if (count < length)
				throw new ArgumentException($"At least {length} registers required", nameof(list));

			if (startIndex < 0 || startIndex + length > count)
				throw new ArgumentOutOfRangeException(nameof(startIndex));

			if (!list.All(o => o.Type == ModbusObjectType.HoldingRegister) && !list.All(o => o.Type == ModbusObjectType.InputRegister))
				throw new ArgumentException("Mixed object types found", nameof(list));

			var registers = list.OrderBy(o => o.Address).Skip(startIndex).Take(length).ToArray();
			if (reverseRegisterOrder)
				Array.Reverse(registers);

			byte[] blob = new byte[registers.Length * 2];
			for (int i = 0; i < registers.Length; i++)
			{
				blob[i * 2] = reverseByteOrderPerRegister
					? registers[i].LowByte
					: registers[i].HighByte;

				blob[i * 2 + 1] = reverseByteOrderPerRegister
					? registers[i].HighByte
					: registers[i].LowByte;
			}

			string text = (encoding ?? Encoding.ASCII).GetString(blob).Trim([' ', '\t', '\0', '\r', '\n']);
			int nullIndex = text.IndexOf('\0');

			if (nullIndex > 0)
			{
#if NET6_0_OR_GREATER
				return text[..nullIndex];
#else
				return text.Substring(0, nullIndex);
#endif
			}

			return text;
		}

		/// <summary>
		/// Converts a <see cref="bool"/> value to a <see cref="Coil"/>.
		/// </summary>
		/// <param name="value">The bool value.</param>
		/// <param name="address">The coil address.</param>
		/// <returns>The coil.</returns>
		public static Coil ToCoil(this bool value, ushort address)
		{
			return new Coil
			{
				Address = address,
				Value = value
			};
		}

		/// <summary>
		/// Converts a <see cref="bool"/> value to a <see cref="HoldingRegister"/>.
		/// </summary>
		/// <param name="value">The bool value.</param>
		/// <param name="address">The register address.</param>
		/// <returns>The register.</returns>
		public static HoldingRegister ToRegister(this bool value, ushort address)
		{
			return new HoldingRegister
			{
				Address = address,
				Value = (ushort)(value ? 1 : 0)
			};
		}

		/// <summary>
		/// Converts a <see cref="string"/> value to a <see cref="HoldingRegister"/>.
		/// </summary>
		/// <param name="value">The text.</param>
		/// <param name="address">The address of the text.</param>
		/// <param name="encoding">The encoding used to convert the text. (Default: <see cref="Encoding.ASCII"/>)</param>
		/// <param name="reverseRegisterOrder">Indicates whehter the taken registers should be reversed.</param>
		/// <param name="reverseByteOrderPerRegister">Indicates whether to reverse high and low byte per register.</param>
		/// <returns>The registers.</returns>
		/// <exception cref="ArgumentNullException">when the text is null.</exception>
		public static IEnumerable<HoldingRegister> ToRegisters(this string value, ushort address, Encoding encoding = null, bool reverseRegisterOrder = false, bool reverseByteOrderPerRegister = false)
		{
#if NET8_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(value);
#else
			if (value == null)
				throw new ArgumentNullException(nameof(value));
#endif

			byte[] blob = (encoding ?? Encoding.ASCII).GetBytes(value);
			int numRegisters = (int)Math.Ceiling(blob.Length / 2.0);

			for (int i = 0; i < numRegisters; i++)
			{
				int addr = reverseRegisterOrder
					? address + numRegisters - 1 - i
					: address + i;

				var register = new HoldingRegister
				{
					Address = (ushort)addr,

					HighByte = reverseByteOrderPerRegister
						? (i * 2 + 1 < blob.Length ? blob[i * 2 + 1] : (byte)0)
						: blob[i * 2],

					LowByte = reverseByteOrderPerRegister
						? blob[i * 2]
						: (i * 2 + 1 < blob.Length ? blob[i * 2 + 1] : (byte)0)
				};

				yield return register;
			}
		}
	}
}
