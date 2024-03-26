using System;
using System.Linq;

namespace AMWD.Protocols.Modbus.Common
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal static class ArrayExtensions
	{
		public static void SwapBigEndian(this byte[] bytes)
		{
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
		}

		public static ushort GetBigEndianUInt16(this byte[] bytes, int offset = 0)
		{
			byte[] b = bytes.Skip(offset).Take(2).ToArray();
			b.SwapBigEndian();
			return BitConverter.ToUInt16(b, 0);
		}

		public static byte[] ToBigEndianBytes(this ushort value)
		{
			byte[] b = BitConverter.GetBytes(value);
			b.SwapBigEndian();
			return b;
		}
	}
}
