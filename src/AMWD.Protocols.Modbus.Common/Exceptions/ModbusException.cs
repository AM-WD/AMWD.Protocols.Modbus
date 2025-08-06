using System;
using System.Diagnostics.CodeAnalysis;
#if !NET8_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace AMWD.Protocols.Modbus.Common
{
	/// <summary>
	/// Represents errors that occurr during Modbus requests.
	/// </summary>
	[ExcludeFromCodeCoverage]
	public class ModbusException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusException"/> class.
		/// </summary>
		public ModbusException()
			: base()
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusException"/> class
		/// with a specified error message.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public ModbusException(string message)
			: base(message)
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusException"/> class
		/// with a specified error message and a reference to the inner exception
		/// that is the cause of this exception.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">
		/// The exception that is the cause of the current exception,
		/// or a null reference if no inner exception is specified.
		/// </param>
		public ModbusException(string message, Exception innerException)
			: base(message, innerException)
		{ }

#if !NET8_0_OR_GREATER

		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusException"/> class
		/// with serialized data.
		/// </summary>
		/// <param name="info">
		/// The <see cref="SerializationInfo"/> that holds the serialized
		/// object data about the exception being thrown.
		/// </param>
		/// <param name="context">
		/// The <see cref="StreamingContext"/> that contains contextual
		/// information about the source or destination.
		/// </param>
		protected ModbusException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }

#endif

		/// <summary>
		/// Gets the Modubs error code.
		/// </summary>
#if NET6_0_OR_GREATER
		public ModbusErrorCode ErrorCode { get; init; }
#else
		public ModbusErrorCode ErrorCode { get; set; }
#endif

		/// <summary>
		/// Gets the Modbus error message.
		/// </summary>
		public string ErrorMessage => ErrorCode.GetDescription();
	}
}
