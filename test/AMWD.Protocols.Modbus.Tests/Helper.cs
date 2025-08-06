using System.Reflection;

namespace AMWD.Protocols.Modbus.Tests
{
	internal static class Helper
	{
		public static T CreateInstance<T>(params object[] args)
		{
			var type = typeof(T);

			object instance = type.Assembly.CreateInstance(
				typeName: type.FullName,
				ignoreCase: false,
				bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic,
				binder: null,
				args: args,
				culture: null,
				activationAttributes: null);

			return (T)instance;
		}
	}
}
