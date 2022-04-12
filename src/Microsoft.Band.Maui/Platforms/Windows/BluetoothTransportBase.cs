using Microsoft.Band.Windows;
using System;
using System.Diagnostics;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;

namespace Microsoft.Band
{
    partial class BluetoothTransportBase
    {
        protected RfcommDeviceService rfcommService;

        protected BluetoothTransportBase(RfcommDeviceService service, ILoggerProvider loggerProvider)
          : this(loggerProvider)
        {
            rfcommService = service;
        }

        public void Connect(RfcommDeviceService service, ushort maxConnectAttempts = 1)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));
            CheckIfDisposed();
            if (IsConnected)
                return;
            if (maxConnectAttempts == 0)
                throw new ArgumentOutOfRangeException(nameof(maxConnectAttempts));
            loggerProvider.Log(ProviderLogLevel.Info, $"Socket.ConnectAsync()... Max Attempts: {maxConnectAttempts}, ConnectionServiceName: {service.ConnectionServiceName}");
            ushort num = 0;
            Exception innerException = null;
            Stopwatch stopwatch1 = Stopwatch.StartNew();
            StreamSocket socket;
            do
            {
                ++num;
                Stopwatch stopwatch2 = Stopwatch.StartNew();
                socket = new StreamSocket();
                try
                {
                    loggerProvider.Log(ProviderLogLevel.Info, $"Socket.ConnectAsync()... Attempt: {num}/{maxConnectAttempts}");
                    socket.ConnectAsync(service.ConnectionHostName, service.ConnectionServiceName).AsTask().Wait();
                    isConnected = true;
                    loggerProvider.Log(ProviderLogLevel.Info, $"Socket.ConnectAsync() succeeded: Attempt: {num}/{maxConnectAttempts}, Elapsed: {stopwatch2.Elapsed}");
                }
                catch (Exception ex)
                {
                    socket.Dispose();
                    if (innerException == null)
                        innerException = ex;
                    loggerProvider.LogException(ProviderLogLevel.Warning, ex, $"Socket.ConnectAsync() failed: Attempt: {num}/{maxConnectAttempts}, Elapsed: {stopwatch2.Elapsed}");
                }
            }
            while (!isConnected && num < maxConnectAttempts);
            if (isConnected)
            {
                cargoStream = new StreamSocketStream(socket);
                cargoReader = new CargoStreamReader(cargoStream, BufferServer.Pool_8192);
                loggerProvider.Log(ProviderLogLevel.Info, $"Socket.ConnectAsync() succeeded: Elapsed: {stopwatch1.Elapsed}, ConnectionServiceName: {service.ConnectionServiceName}");
            }
            else
            {
                loggerProvider.Log(ProviderLogLevel.Error, $"Socket.ConnectAsync() failed: Attempts: {num}, Elapsed: {stopwatch1.Elapsed}, ConnectionServiceName: {service.ConnectionServiceName}");
                throw new BandIOException(BandResources.ConnectionAttemptFailed, innerException);
            }
        }
    }
}
