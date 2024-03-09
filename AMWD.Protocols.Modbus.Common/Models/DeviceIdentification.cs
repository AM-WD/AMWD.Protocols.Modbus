using System.Collections.Generic;

namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// Represents the device identification.
	/// </summary>
	public class DeviceIdentification
	{
		/// <summary>
		/// Gets or sets the vendor name.
		/// </summary>
		/// <remarks>
		/// Category: Basic
		/// <br/>
		/// Kind: Mandatory
		/// </remarks>
		public string VendorName { get; set; }

		/// <summary>
		/// Gets or sets the product code.
		/// </summary>
		/// <remarks>
		/// Category: Basic
		/// <br/>
		/// Kind: Mandatory
		/// </remarks>
		public string ProductCode { get; set; }

		/// <summary>
		/// Gets or sets the version in major, minor and revision.
		/// </summary>
		/// <remarks>
		/// Category: Basic
		/// <br/>
		/// Kind: Mandatory
		/// </remarks>
		public string MajorMinorRevision { get; set; }

		/// <summary>
		/// Gets or sets the vendor URL.
		/// </summary>
		/// <remarks>
		/// Category: Regular
		/// <br/>
		/// Kind: Optional
		/// </remarks>
		public string VendorUrl { get; set; }

		/// <summary>
		/// Gets or sets the product name.
		/// </summary>
		/// <remarks>
		/// Category: Regular
		/// <br/>
		/// Kind: Optional
		/// </remarks>
		public string ProductName { get; set; }

		/// <summary>
		/// Gets or sets the model name.
		/// </summary>
		/// <remarks>
		/// Category: Regular
		/// <br/>
		/// Kind: Optional
		/// </remarks>
		public string ModelName { get; set; }

		/// <summary>
		/// Gets or sets the user application name.
		/// </summary>
		/// <remarks>
		/// Category: Regular
		/// <br/>
		/// Kind: Optional
		/// </remarks>
		public string UserApplicationName { get; set; }

		/// <summary>
		/// Gets or sets the extended objects.
		/// </summary>
		/// <remarks>
		/// Category: Extended
		/// <br/>
		/// Kind: Optional
		/// </remarks>
		public Dictionary<byte, byte[]> ExtendedObjects { get; set; } = [];

		/// <summary>
		/// Gets or sets a value indicating whether individual access (<see cref="ModbusDeviceIdentificationCategory.Individual"/>) is allowed.
		/// </summary>
		public bool IsIndividualAccessAllowed { get; set; }
	}
}
