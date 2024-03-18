using System;
using System.Linq;

namespace AMWD.Protocols.Modbus.Common
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal static class ArrayExtensions
	{
		public static void SwapNetworkOrder(this byte[] bytes)
		{
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
		}

		public static ushort NetworkUInt16(this byte[] bytes, int offset = 0)
		{
			byte[] b = bytes.Skip(offset).Take(2).ToArray();
			b.SwapNetworkOrder();
			return BitConverter.ToUInt16(b, 0);
		}

		public static byte[] ToNetworkBytes(this ushort value)
		{
			byte[] b = BitConverter.GetBytes(value);
			b.SwapNetworkOrder();
			return b;
		}
	}
}
