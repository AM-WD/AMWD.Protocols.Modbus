using System;

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
	}
}
