using System.Collections.Generic;

namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// The raw device identification data as returned by the device (as spec defines).
	/// </summary>
	public class DeviceIdentificationRaw
	{
		/// <summary>
		/// Gets or sets a value indicating whether the conformity level allowes an idividual access.
		/// </summary>
		public bool AllowsIndividualAccess { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether more requests are needed.
		/// </summary>
		public bool MoreRequestsNeeded { get; set; }

		/// <summary>
		/// Gets or sets the next object id to request (if <see cref="MoreRequestsNeeded"/> is <see langword="true"/>).
		/// </summary>
		public byte NextObjectIdToRequest { get; set; }

		/// <summary>
		/// Gets or sets the objects with raw bytes.
		/// </summary>
		public Dictionary<byte, byte[]> Objects { get; set; } = [];
	}
}
