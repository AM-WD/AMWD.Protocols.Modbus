using System.Threading.Tasks;

namespace AMWD.Protocols.Modbus.Tcp.Extensions
{
	internal static class TaskExtensions
	{
		public static async void Forget(this Task task)
		{
			try
			{
				await task;
			}
			catch
			{ /* keep it quiet */ }
		}
	}
}
