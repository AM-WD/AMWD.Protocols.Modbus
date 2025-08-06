using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace AMWD.Protocols.Modbus.Tests
{
	// ================================================================================================================================ //
	// Source: https://git.am-wd.de/am-wd/common/-/blob/fb26e441a48214aaae72003c4a5ac33d5c7b929a/src/AMWD.Common.Test/SnapshotAssert.cs //
	// ================================================================================================================================ //
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	internal sealed class SnapshotAssert
	{
		/// <summary>
		/// Tests whether the specified string is equal to the saved snapshot.
		/// </summary>
		/// <param name="actual">The current aggregated content string.</param>
		/// <param name="message">An optional message to display if the assertion fails.</param>
		/// <param name="callerFilePath">The absolute file path of the calling file (filled automatically on compile time).</param>
		/// <param name="callerMemberName">The name of the calling method (filled automatically on compile time).</param>
		public static void AreEqual(string actual, string message = null, [CallerFilePath] string callerFilePath = null, [CallerMemberName] string callerMemberName = null)
		{
			string cleanLineEnding = actual
				.Replace("\r\n", "\n") // Windows
				.Replace("\r", "\n");  // MacOS
			AreEqual(Encoding.UTF8.GetBytes(cleanLineEnding), message, callerFilePath, callerMemberName);
		}

		/// <summary>
		/// Tests whether the specified byte array is equal to the saved snapshot.
		/// </summary>
		/// <param name="actual">The current aggregated content bytes.</param>
		/// <param name="message">An optional message to display if the assertion fails.</param>
		/// <param name="callerFilePath">The absolute file path of the calling file (filled automatically on compile time).</param>
		/// <param name="callerMemberName">The name of the calling method (filled automatically on compile time).</param>
		public static void AreEqual(byte[] actual, string message = null, [CallerFilePath] string callerFilePath = null, [CallerMemberName] string callerMemberName = null)
			=> AreEqual(actual, null, message, callerFilePath, callerMemberName);

		/// <summary>
		/// Tests whether the specified byte array is equal to the saved snapshot.
		/// </summary>
		/// <remarks>
		/// The past has shown, that e.g. wkhtmltopdf prints the current timestamp at the beginning of the PDF file.
		/// Therefore you can specify which sequences of bytes should be excluded from the comparison.
		/// </remarks>
		/// <param name="actual">The current aggregated content bytes.</param>
		/// <param name="excludedSequences">The excluded sequences.</param>
		/// <param name="message">An optional message to display if the assertion fails.</param>
		/// <param name="callerFilePath">The absolute file path of the calling file (filled automatically on compile time).</param>
		/// <param name="callerMemberName">The name of the calling method (filled automatically on compile time).</param>
		public static void AreEqual(byte[] actual, List<(int Start, int Length)> excludedSequences = null, string message = null, [CallerFilePath] string callerFilePath = null, [CallerMemberName] string callerMemberName = null)
		{
			string callerDirectory = Path.GetDirectoryName(callerFilePath);
			string callerFileName = Path.GetFileNameWithoutExtension(callerFilePath);

			string snapshotDirectory = Path.Combine(callerDirectory, "Snapshots", callerFileName);
			string snapshotFilePath = Path.Combine(snapshotDirectory, $"{callerMemberName}.snap.bin");

			if (File.Exists(snapshotFilePath))
			{
				byte[] expected = File.ReadAllBytes(snapshotFilePath);
				if (actual.Length != expected.Length)
					Assert.Fail(message);

				for (int i = 0; i < actual.Length; i++)
				{
					if (excludedSequences?.Any(s => s.Start <= i && i < s.Start + s.Length) == true)
						continue;

					if (actual[i] != expected[i])
						Assert.Fail(message);
				}
			}
			else
			{
				if (!Directory.Exists(snapshotDirectory))
					Directory.CreateDirectory(snapshotDirectory);

				File.WriteAllBytes(snapshotFilePath, actual);
			}
		}
	}
}
