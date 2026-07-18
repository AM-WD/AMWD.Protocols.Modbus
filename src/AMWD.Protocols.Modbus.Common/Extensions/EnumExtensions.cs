using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace System
{
	[Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal static class EnumExtensions
	{
		private static IEnumerable<TAttribute> GetAttributes<TAttribute>(this Enum value)
			where TAttribute : Attribute
		{
			var fieldInfo = value.GetType().GetField(value.ToString());
			if (fieldInfo == null)
				return Array.Empty<TAttribute>();

			return fieldInfo.GetCustomAttributes(typeof(TAttribute), inherit: false).Cast<TAttribute>();
		}

		private static TAttribute GetAttribute<TAttribute>(this Enum value)
			where TAttribute : Attribute
			=> value.GetAttributes<TAttribute>().FirstOrDefault();

		public static string GetDescription(this Enum value)
			=> value.GetAttribute<DescriptionAttribute>()?.Description ?? value.ToString();
	}
}
