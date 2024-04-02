using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AMWD.Protocols.Modbus.Common;
using AMWD.Protocols.Modbus.Common.Events;
using AMWD.Protocols.Modbus.Common.Models;
using AMWD.Protocols.Modbus.Common.Protocols;

namespace AMWD.Protocols.Modbus.Tcp
{
	/// <summary>
	/// A basic implementation of a Modbus TCP server.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class ModbusTcpServer : IDisposable
	{
		#region Fields

		private bool _isDisposed;

		private TcpListener _listener;
		private CancellationTokenSource _stopCts;
		private Task _clientConnectTask = Task.CompletedTask;

		private readonly SemaphoreSlim _clientListLock = new(1, 1);
		private readonly List<TcpClient> _clients = [];
		private readonly List<Task> _clientTasks = [];

		private readonly ReaderWriterLockSlim _deviceListLock = new();
		private readonly Dictionary<byte, ModbusDevice> _devices = [];

		#endregion Fields

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ModbusTcpServer"/> class.
		/// </summary>
		/// <param name="listenAddress">An <see cref="IPAddress"/> to listen on (Default: <see cref="IPAddress.Loopback"/>).</param>
		/// <param name="listenPort">A port to listen on (Default: 502).</param>
		public ModbusTcpServer(IPAddress listenAddress = null, int listenPort = 502)
		{
			ListenAddress = listenAddress ?? IPAddress.Loopback;

			if (ushort.MinValue < listenPort || listenPort < ushort.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(listenPort));

			try
			{
#if NET8_0_OR_GREATER
				using var testListener = new TcpListener(ListenAddress, listenPort);
#else
				var testListener = new TcpListener(ListenAddress, listenPort);
#endif
				testListener.Start(1);
				ListenPort = (testListener.LocalEndpoint as IPEndPoint).Port;
				testListener.Stop();
			}
			catch (Exception ex)
			{
				throw new ArgumentException($"{nameof(ListenPort)} ({listenPort}) is already in use.", ex);
			}
		}

		#endregion Constructors

		#region Events

		/// <summary>
		/// Occurs when a <see cref="Coil"/> is written.
		/// </summary>
		public event EventHandler<CoilWrittenEventArgs> CoilWritten;

		/// <summary>
		/// Occurs when a <see cref="HoldingRegister"/> is written.
		/// </summary>
		public event EventHandler<RegisterWrittenEventArgs> RegisterWritten;

		#endregion Events

		#region Properties

		/// <summary>
		/// Gets the <see cref="IPAddress"/> to listen on.
		/// </summary>
		public IPAddress ListenAddress { get; }

		/// <summary>
		/// Get the port to listen on.
		/// </summary>
		public int ListenPort { get; }

		/// <summary>
		/// Gets a value indicating whether the server is running.
		/// </summary>
		public bool IsRunning => _listener?.Server.IsBound ?? false;

		/// <summary>
		/// Gets or sets the read/write timeout.
		/// </summary>
		public TimeSpan ReadWriteTimeout { get; set; }

		#endregion Properties

		#region Control Methods

		/// <summary>
		/// Starts the server.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		public Task StartAsync(CancellationToken cancellationToken = default)
		{
			Assertions();

			_stopCts?.Cancel();

			_listener?.Stop();
#if NET8_0_OR_GREATER
			_listener?.Dispose();
#endif

			_stopCts?.Dispose();
			_stopCts = new CancellationTokenSource();

			_listener = new TcpListener(ListenAddress, ListenPort);
			if (ListenAddress.AddressFamily == AddressFamily.InterNetworkV6)
				_listener.Server.DualMode = true;

			_listener.Start();
			_clientConnectTask = WaitForClientAsync(_stopCts.Token);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Stops the server.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
		public Task StopAsync(CancellationToken cancellationToken = default)
		{
			Assertions();
			return StopAsyncInternal(cancellationToken);
		}

		private async Task StopAsyncInternal(CancellationToken cancellationToken = default)
		{
			_stopCts.Cancel();

			_listener.Stop();
#if NET8_0_OR_GREATER
			_listener.Dispose();
#endif
			try
			{
				await Task.WhenAny(_clientConnectTask, Task.Delay(Timeout.Infinite, cancellationToken));
			}
			catch (OperationCanceledException)
			{
				// Terminated
			}

			try
			{
				await Task.WhenAny(Task.WhenAll(_clientTasks), Task.Delay(Timeout.Infinite, cancellationToken));
			}
			catch (OperationCanceledException)
			{
				// Terminated
			}
		}

		/// <summary>
		/// Releases all managed and unmanaged resources used by the <see cref="ModbusTcpServer"/>.
		/// </summary>
		public void Dispose()
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			StopAsyncInternal(CancellationToken.None).Wait();

			_clientListLock.Dispose();
			_deviceListLock.Dispose();

			_clients.Clear();
			_devices.Clear();
		}

		private void Assertions()
		{
#if NET8_0_OR_GREATER
			ObjectDisposedException.ThrowIf(_isDisposed, this);
#else
			if (_isDisposed)
				throw new ObjectDisposedException(GetType().FullName);
#endif
		}

		#endregion Control Methods

		#region Client Handling

		private async Task WaitForClientAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
#if NET8_0_OR_GREATER
					var client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
#else
					var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
#endif
					await _clientListLock.WaitAsync(cancellationToken).ConfigureAwait(false);
					try
					{
						_clients.Add(client);
						_clientTasks.Add(HandleClientAsync(client, cancellationToken));
					}
					finally
					{
						_clientListLock.Release();
					}
				}
				catch
				{
					// There might be a failure here, that's ok, just keep it quiet
				}
			}
		}

		private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
		{
			try
			{
				var stream = client.GetStream();
				while (!cancellationToken.IsCancellationRequested)
				{
					var requestBytes = new List<byte>();

					using (var cts = new CancellationTokenSource(ReadWriteTimeout))
					using (cancellationToken.Register(cts.Cancel))
					{
						byte[] headerBytes = await stream.ReadExpectedBytesAsync(6, cts.Token).ConfigureAwait(false);
						requestBytes.AddRange(headerBytes);

						byte[] followingCountBytes = headerBytes.Skip(4).Take(2).ToArray();
						followingCountBytes.SwapBigEndian();
						int followingCount = BitConverter.ToUInt16(followingCountBytes, 0);

						byte[] bodyBytes = await stream.ReadExpectedBytesAsync(followingCount, cts.Token).ConfigureAwait(false);
						requestBytes.AddRange(bodyBytes);
					}

					byte[] responseBytes = HandleRequest([.. requestBytes]);
					if (responseBytes != null)
						await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken).ConfigureAwait(false);
				}
			}
			catch
			{
				// Keep client processing quiet
			}
			finally
			{
				await _clientListLock.WaitAsync(cancellationToken).ConfigureAwait(false);
				try
				{
					_clients.Remove(client);
					client.Dispose();
				}
				finally
				{
					_clientListLock.Release();
				}
			}
		}

		#endregion Client Handling

		#region Request Handling

		private byte[] HandleRequest(byte[] requestBytes)
		{
			using (_deviceListLock.GetReadLock())
			{
				// No response is sent, if the device is not known
				if (!_devices.TryGetValue(requestBytes[6], out var device))
					return null;

				switch ((ModbusFunctionCode)requestBytes[7])
				{
					case ModbusFunctionCode.ReadCoils:
						return HandleReadCoils(device, requestBytes);

					case ModbusFunctionCode.ReadDiscreteInputs:
						return HandleReadDiscreteInputs(device, requestBytes);

					case ModbusFunctionCode.ReadHoldingRegisters:
						return HandleReadHoldingRegisters(device, requestBytes);

					case ModbusFunctionCode.ReadInputRegisters:
						return HandleReadInputRegisters(device, requestBytes);

					case ModbusFunctionCode.WriteSingleCoil:
						return HandleWriteSingleCoil(device, requestBytes);

					case ModbusFunctionCode.WriteSingleRegister:
						return HandleWriteSingleRegister(device, requestBytes);

					case ModbusFunctionCode.WriteMultipleCoils:
						return HandleWriteMultipleCoils(device, requestBytes);

					case ModbusFunctionCode.WriteMultipleRegisters:
						return HandleWriteMultipleRegisters(device, requestBytes);

					case ModbusFunctionCode.EncapsulatedInterface:
						return HandleEncapsulatedInterface(requestBytes);

					default: // unknown function
						{
							byte[] responseBytes = new byte[9];
							Array.Copy(requestBytes, 0, responseBytes, 0, 8);

							// Mark as error
							responseBytes[7] |= 0x80;

							responseBytes[8] = (byte)ModbusErrorCode.IllegalFunction;
							return responseBytes;
						}
				}
			}
		}

		private static byte[] HandleReadCoils(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 12)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(8));

			ushort firstAddress = requestBytes.GetBigEndianUInt16(8);
			ushort count = requestBytes.GetBigEndianUInt16(10);

			if (TcpProtocol.MIN_READ_COUNT < count || count < TcpProtocol.MAX_DISCRETE_READ_COUNT)
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);
				return [.. responseBytes];
			}

			if (firstAddress + count > ushort.MaxValue)
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataAddress);
				return [.. responseBytes];
			}

			try
			{
				byte[] values = new byte[(int)Math.Ceiling(count / 8.0)];
				for (int i = 0; i < count; i++)
				{
					ushort address = (ushort)(firstAddress + i);
					if (device.GetCoil(address).Value)
					{
						int byteIndex = i / 8;
						int bitIndex = i % 8;

						values[byteIndex] |= (byte)(1 << bitIndex);
					}
				}

				responseBytes.Add((byte)values.Length);
				responseBytes.AddRange(values);
			}
			catch
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			return [.. responseBytes];
		}

		private static byte[] HandleReadDiscreteInputs(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 12)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(8));

			ushort firstAddress = requestBytes.GetBigEndianUInt16(8);
			ushort count = requestBytes.GetBigEndianUInt16(10);

			if (TcpProtocol.MIN_READ_COUNT < count || count < TcpProtocol.MAX_DISCRETE_READ_COUNT)
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);
				return [.. responseBytes];
			}

			if (firstAddress + count > ushort.MaxValue)
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataAddress);
				return [.. responseBytes];
			}

			try
			{
				byte[] values = new byte[(int)Math.Ceiling(count / 8.0)];
				for (int i = 0; i < count; i++)
				{
					ushort address = (ushort)(firstAddress + i);
					if (device.GetDiscreteInput(address).Value)
					{
						int byteIndex = i / 8;
						int bitIndex = i % 8;

						values[byteIndex] |= (byte)(1 << bitIndex);
					}
				}

				responseBytes.Add((byte)values.Length);
				responseBytes.AddRange(values);
			}
			catch
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			return [.. responseBytes];
		}

		private static byte[] HandleReadHoldingRegisters(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 12)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(8));

			ushort firstAddress = requestBytes.GetBigEndianUInt16(8);
			ushort count = requestBytes.GetBigEndianUInt16(10);

			if (TcpProtocol.MIN_READ_COUNT < count || count < TcpProtocol.MAX_REGISTER_READ_COUNT)
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);
				return [.. responseBytes];
			}

			if (firstAddress + count > ushort.MaxValue)
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataAddress);
				return [.. responseBytes];
			}

			try
			{
				byte[] values = new byte[count * 2];
				for (int i = 0; i < count; i++)
				{
					ushort address = (ushort)(firstAddress + i);
					var register = device.GetHoldingRegister(address);

					values[i * 2] = register.HighByte;
					values[i * 2 + 1] = register.LowByte;
				}

				responseBytes.Add((byte)values.Length);
				responseBytes.AddRange(values);
			}
			catch
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			return [.. responseBytes];
		}

		private static byte[] HandleReadInputRegisters(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 12)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(8));

			ushort firstAddress = requestBytes.GetBigEndianUInt16(8);
			ushort count = requestBytes.GetBigEndianUInt16(10);

			if (TcpProtocol.MIN_READ_COUNT < count || count < TcpProtocol.MAX_REGISTER_READ_COUNT)
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);
				return [.. responseBytes];
			}

			if (firstAddress + count > ushort.MaxValue)
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataAddress);
				return [.. responseBytes];
			}

			try
			{
				byte[] values = new byte[count * 2];
				for (int i = 0; i < count; i++)
				{
					ushort address = (ushort)(firstAddress + i);
					var register = device.GetInputRegister(address);

					values[i * 2] = register.HighByte;
					values[i * 2 + 1] = register.LowByte;
				}

				responseBytes.Add((byte)values.Length);
				responseBytes.AddRange(values);
			}
			catch
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			return [.. responseBytes];
		}

		private byte[] HandleWriteSingleCoil(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 12)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(8));

			ushort address = requestBytes.GetBigEndianUInt16(8);

			if (requestBytes[10] != 0x00 && requestBytes[10] != 0xFF)
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);
				return [.. responseBytes];
			}

			try
			{
				device.SetCoil(new Coil
				{
					Address = address,
					HighByte = requestBytes[10]
				});

				// Response is an echo of the request
				responseBytes.AddRange(requestBytes.Skip(8).Take(4));

				// Notify that the coil was written
				Task.Run(() =>
				{
					try
					{
						CoilWritten?.Invoke(this, new CoilWrittenEventArgs
						{
							UnitId = device.Id,
							Address = address,
							Value = requestBytes[10] == 0xFF
						});
					}
					catch
					{
						// keep everything quiet
					}
				});
			}
			catch
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			return [.. responseBytes];
		}

		private byte[] HandleWriteSingleRegister(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 12)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(8));

			ushort address = requestBytes.GetBigEndianUInt16(8);
			ushort value = requestBytes.GetBigEndianUInt16(10);

			try
			{
				device.SetHoldingRegister(new HoldingRegister
				{
					Address = address,
					HighByte = requestBytes[10],
					LowByte = requestBytes[11]
				});

				// Response is an echo of the request
				responseBytes.AddRange(requestBytes.Skip(8).Take(4));

				// Notify that the register was written
				Task.Run(() =>
				{
					try
					{
						RegisterWritten?.Invoke(this, new RegisterWrittenEventArgs
						{
							UnitId = device.Id,
							Address = address,
							Value = value,
							HighByte = requestBytes[10],
							LowByte = requestBytes[11]
						});
					}
					catch
					{
						// keep everything quiet
					}
				});
			}
			catch
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			return [.. responseBytes];
		}

		private byte[] HandleWriteMultipleCoils(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 13)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(8));

			ushort firstAddress = requestBytes.GetBigEndianUInt16(8);
			ushort count = requestBytes.GetBigEndianUInt16(10);

			int byteCount = (int)Math.Ceiling(count / 8.0);
			if (requestBytes.Length < 13 + byteCount)
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);
				return [.. responseBytes];
			}

			try
			{
				int baseOffset = 13;
				for (int i = 0; i < count; i++)
				{
					int bytePosition = i / 8;
					int bitPosition = i % 8;

					ushort address = (ushort)(firstAddress + i);
					bool value = (requestBytes[baseOffset + bytePosition] & (1 << bitPosition)) > 0;

					device.SetCoil(new Coil
					{
						Address = address,
						HighByte = value ? (byte)0xFF : (byte)0x00
					});

					// Notify that the coil was written
					Task.Run(() =>
					{
						try
						{
							CoilWritten?.Invoke(this, new CoilWrittenEventArgs
							{
								UnitId = device.Id,
								Address = address,
								Value = value
							});
						}
						catch
						{
							// keep everything quiet
						}
					});
				}

				responseBytes.AddRange(requestBytes.Skip(8).Take(4));
			}
			catch
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			return [.. responseBytes];
		}

		private byte[] HandleWriteMultipleRegisters(ModbusDevice device, byte[] requestBytes)
		{
			if (requestBytes.Length < 13)
				return null;

			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(8));

			ushort firstAddress = requestBytes.GetBigEndianUInt16(8);
			ushort count = requestBytes.GetBigEndianUInt16(10);

			int byteCount = count * 2;
			if (requestBytes.Length < 13 + byteCount)
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);
				return [.. responseBytes];
			}

			try
			{
				int baseOffset = 13;
				for (int i = 0; i < count; i++)
				{
					ushort address = (ushort)(firstAddress + i);

					device.SetHoldingRegister(new HoldingRegister
					{
						Address = address,
						HighByte = requestBytes[baseOffset + i * 2],
						LowByte = requestBytes[baseOffset + i * 2 + 1]
					});

					// Notify that the coil was written
					Task.Run(() =>
					{
						try
						{
							RegisterWritten?.Invoke(this, new RegisterWrittenEventArgs
							{
								UnitId = device.Id,
								Address = address,
								Value = requestBytes.GetBigEndianUInt16(baseOffset + i * 2),
								HighByte = requestBytes[baseOffset + i * 2],
								LowByte = requestBytes[baseOffset + i * 2 + 1]
							});
						}
						catch
						{
							// keep everything quiet
						}
					});
				}

				responseBytes.AddRange(requestBytes.Skip(8).Take(4));
			}
			catch
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
			}

			return [.. responseBytes];
		}

		private byte[] HandleEncapsulatedInterface(byte[] requestBytes)
		{
			var responseBytes = new List<byte>();
			responseBytes.AddRange(requestBytes.Take(8));

			if (requestBytes[8] != 0x0E)
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalFunction);
				return [.. responseBytes];
			}

			if (0x06 < requestBytes[10] && requestBytes[10] < 0x80)
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataAddress);
				return [.. responseBytes];
			}

			var category = (ModbusDeviceIdentificationCategory)requestBytes[9];
			if (!Enum.IsDefined(typeof(ModbusDeviceIdentificationCategory), category))
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.IllegalDataValue);
				return [.. responseBytes];
			}

			try
			{
				var bodyBytes = new List<byte>();
				// MEI, Category
				bodyBytes.AddRange(requestBytes.Skip(8).Take(2));
				// Conformity
				bodyBytes.Add((byte)(category + 0x80));
				// More, NextId, NumberOfObjects
				bodyBytes.AddRange(new byte[3]);

				int maxObjectId;
				switch (category)
				{
					case ModbusDeviceIdentificationCategory.Basic:
						maxObjectId = 0x02;
						break;

					case ModbusDeviceIdentificationCategory.Regular:
						maxObjectId = 0x06;
						break;

					case ModbusDeviceIdentificationCategory.Extended:
						maxObjectId = 0xFF;
						break;

					default: // Individual
						{
							if (requestBytes[10] < 0x03)
								bodyBytes[2] = 0x81;
							else if (requestBytes[10] < 0x80)
								bodyBytes[2] = 0x82;
							else
								bodyBytes[2] = 0x83;

							maxObjectId = requestBytes[10];
						}

						break;
				}

				byte numberOfObjects = 0;
				for (int i = requestBytes[10]; i <= maxObjectId; i++)
				{
					// Reserved
					if (0x07 <= i && i <= 0x7F)
						continue;

					byte[] objBytes = GetDeviceObject((byte)i);

					// We need to split the response if it would exceed the max ADU size
					if (responseBytes.Count + bodyBytes.Count + objBytes.Length > TcpProtocol.MAX_ADU_LENGTH)
					{
						bodyBytes[3] = 0xFF;
						bodyBytes[4] = (byte)i;

						bodyBytes[5] = numberOfObjects;
						responseBytes.AddRange(bodyBytes);
						return [.. responseBytes];
					}

					bodyBytes.AddRange(objBytes);
					numberOfObjects++;
				}

				bodyBytes[5] = numberOfObjects;
				responseBytes.AddRange(bodyBytes);
				return [.. responseBytes];
			}
			catch
			{
				responseBytes[7] |= 0x80;
				responseBytes.Add((byte)ModbusErrorCode.SlaveDeviceFailure);
				return [.. responseBytes];
			}
		}

		private byte[] GetDeviceObject(byte objectId)
		{
			var result = new List<byte> { objectId };
			switch ((ModbusDeviceIdentificationObject)objectId)
			{
				case ModbusDeviceIdentificationObject.VendorName:
					{
						byte[] bytes = Encoding.UTF8.GetBytes("AMWD");
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.ProductCode:
					{
						byte[] bytes = Encoding.UTF8.GetBytes("AMWD-MBS-TCP");
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.MajorMinorRevision:
					{
						string version = GetType().Assembly
							.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
							.InformationalVersion;

						byte[] bytes = Encoding.UTF8.GetBytes(version);
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.VendorUrl:
					{
						byte[] bytes = Encoding.UTF8.GetBytes("https://github.com/AM-WD/AMWD.Protocols.Modbus");
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.ProductName:
					{
						byte[] bytes = Encoding.UTF8.GetBytes("AM.WD Modbus Library");
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.ModelName:
					{
						byte[] bytes = Encoding.UTF8.GetBytes("TCP Server");
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				case ModbusDeviceIdentificationObject.UserApplicationName:
					{
						byte[] bytes = Encoding.UTF8.GetBytes("Modbus TCP Server");
						result.Add((byte)bytes.Length);
						result.AddRange(bytes);
					}
					break;

				default:
					result.Add(0x00);
					break;
			}

			return [.. result];
		}

		#endregion Request Handling

		#region Device Handling

		/// <summary>
		/// Adds a new device to the server.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <returns><see langword="true"/> if the device was added, <see langword="false"/> otherwise.</returns>
		public bool AddDevice(byte unitId)
		{
			Assertions();

			using (_deviceListLock.GetWriteLock())
			{
				if (_devices.ContainsKey(unitId))
					return false;

				_devices.Add(unitId, new ModbusDevice(unitId));
				return true;
			}
		}

		/// <summary>
		/// Removes a device from the server.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <returns><see langword="true"/> if the device was removed, <see langword="false"/> otherwise.</returns>
		public bool RemoveDevice(byte unitId)
		{
			Assertions();

			using (_deviceListLock.GetWriteLock())
			{
				if (_devices.TryGetValue(unitId, out var device))
					device.Dispose();

				return _devices.Remove(unitId);
			}
		}

		/// <summary>
		/// Gets a <see cref="Coil"/> from the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="address">The address of the coil.</param>
		public Coil GetCoil(byte unitId, ushort address)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return null;

				return device.GetCoil(address);
			}
		}

		/// <summary>
		/// Sets a <see cref="Coil"/> to the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="coil">The <see cref="Coil"/> to set.</param>
		public void SetCoil(byte unitId, Coil coil)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return;

				device.SetCoil(coil);
			}
		}

		/// <summary>
		/// Gets a <see cref="DiscreteInput"/> from the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="address">The address of the <see cref="DiscreteInput"/>.</param>
		public DiscreteInput GetDiscreteInput(byte unitId, ushort address)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return null;

				return device.GetDiscreteInput(address);
			}
		}

		/// <summary>
		/// Sets a <see cref="DiscreteInput"/> to the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="discreteInput">The <see cref="DiscreteInput"/> to set.</param>
		public void SetDiscreteInput(byte unitId, DiscreteInput discreteInput)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return;

				device.SetDiscreteInput(discreteInput);
			}
		}

		/// <summary>
		/// Gets a <see cref="HoldingRegister"/> from the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="address">The address of the <see cref="HoldingRegister"/>.</param>
		public HoldingRegister GetHoldingRegister(byte unitId, ushort address)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return null;

				return device.GetHoldingRegister(address);
			}
		}

		/// <summary>
		/// Sets a <see cref="HoldingRegister"/> to the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="holdingRegister">The <see cref="HoldingRegister"/> to set.</param>
		public void SetHoldingRegister(byte unitId, HoldingRegister holdingRegister)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return;

				device.SetHoldingRegister(holdingRegister);
			}
		}

		/// <summary>
		/// Gets a <see cref="InputRegister"/> from the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="address">The address of the <see cref="InputRegister"/>.</param>
		public InputRegister GetInputRegister(byte unitId, ushort address)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return null;

				return device.GetInputRegister(address);
			}
		}

		/// <summary>
		/// Sets a <see cref="InputRegister"/> to the specified <see cref="ModbusDevice"/>.
		/// </summary>
		/// <param name="unitId">The unit ID of the device.</param>
		/// <param name="inputRegister">The <see cref="InputRegister"/> to set.</param>
		public void SetInputRegister(byte unitId, InputRegister inputRegister)
		{
			Assertions();

			using (_deviceListLock.GetReadLock())
			{
				if (!_devices.TryGetValue(unitId, out var device))
					return;

				device.SetInputRegister(inputRegister);
			}
		}

		#endregion Device Handling
	}
}
