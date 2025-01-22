#pragma warning disable 8600, 8622, 8618, 8601

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization.Serializers;
using Serilog;

namespace Gaos.wsrv
{
    public class WsrConnectionPoolService : IHostedService
    {
        public static string CLASS_NAME = typeof(WsrConnectionPoolService).Name;

        private readonly string ipAddress;
        private readonly int port;
        private readonly int poolSize;
        private readonly ConcurrentBag<Socket> clientPool;
        private readonly SemaphoreSlim poolSemaphore;
        private readonly SemaphoreSlim initSemaphore;
        private readonly SemaphoreSlim ensureConnectionsSemathore;
        private bool isInitialized;
        private Timer connectionCheckTimer;

        private bool isConnectToGaow = false;

        public WsrConnectionPoolService(IConfiguration configuration, string ipAddress = "127.0.0.1", int port = 3010, int poolSize = 5)
        {
            const string METHOD_NAME = "WsrConnectionPoolService()";
            this.ipAddress = ipAddress;
            this.port = port;
            this.poolSize = poolSize;
            this.clientPool = new ConcurrentBag<Socket>();
            this.poolSemaphore = new SemaphoreSlim(poolSize);
            this.initSemaphore = new SemaphoreSlim(1);
            this.ensureConnectionsSemathore = new SemaphoreSlim(1);
            this.isInitialized = false;


            if (configuration["gaow_connect"] == null)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: missing configuration value: gaow_connect");
                throw new Exception("missing configuration value: gaow_connect");
            }
            if (configuration["gaow_connect"] == "true")
            {
                isConnectToGaow = true;
                if (configuration["gaow_port"] != null)
                {
                    string portStr = configuration["gaow_port"];
                    if (!int.TryParse(portStr, out this.port))
                    {
                        Log.Error($"{CLASS_NAME}:{METHOD_NAME}: invalid configuration value: gaow_port");
                        throw new Exception("invalid configuration value: gaow_port");
                    }
                }
                if (configuration["gaow_ip"] != null)
                {
                    this.ipAddress = configuration["gaow_ip"];
                }

            }
            else
            {
                isConnectToGaow = false;
            }
        }

        public async Task Init()
        {
            const string METHOD_NAME = "Init()";
            await initSemaphore.WaitAsync();
            try
            {
                if (!isInitialized)
                {
                    // Initialize the client pool
                    var tasks = new List<Task<bool>>();
                    for (int i = 0; i < poolSize; i++)
                    {
                        tasks.Add(CreateAndAddClientAsync());
                    }
                    await Task.WhenAll(tasks);
                    isInitialized = true;
                    Log.Information($"{CLASS_NAME}:{METHOD_NAME}: Client pool initialized, current pool size {clientPool.Count}");
                }
            }
            catch (Exception e)
            {
                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: Error initializing client pool: {e.Message}");
            }
            finally
            {
                initSemaphore.Release();
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!isConnectToGaow)
            {
                return;
            }

            await Init();


            // Start the periodic connection check
            connectionCheckTimer = new Timer(CheckConnections, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop the timer
            connectionCheckTimer?.Change(Timeout.Infinite, 0);
            connectionCheckTimer?.Dispose();

            // Clean up resources if needed
            foreach (var client in clientPool)
            {
                client.Close();
            }
            return Task.CompletedTask;
        }

        private void CheckConnections(object state)
        {
            _ = EnsureConnections();
        }

        public async Task EnsureConnections()
        {
            const string METHOD_NAME = "EnsureConnections()";
            await ensureConnectionsSemathore.WaitAsync();
            try
            {

                EvictConnections();

                var missingConnections = poolSize - clientPool.Count;
                if (missingConnections <= 0)
                {
                    return;
                }


                var tasks = new List<Task>();

                for (int i = 0; i < missingConnections; i++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        await CreateAndAddClientAsync();
                    }));
                }

                await Task.WhenAll(tasks);

                Log.Information($"{CLASS_NAME}:{METHOD_NAME}: Finished adding connections. New pool size: {clientPool.Count}");
            }
            finally
            {
                ensureConnectionsSemathore.Release();
            }
        }

        public async void EvictConnections()
        {
            const string METHOD_NAME = "EvictConnections()";

            var activeSockets = new ConcurrentBag<Socket>();
            int evictedCount = 0;
            int checkedCount = 0;

            while (clientPool.TryTake(out Socket socket))
            {
                checkedCount++;
                try
                {
                    if (socket.Poll(1, SelectMode.SelectRead))
                    {
                        byte[] buff = new byte[1];
                        if (socket.Receive(buff, SocketFlags.Peek) == 0)
                        {
                            // Connection closed
                            socket.Close();
                            evictedCount++;
                        }
                        else
                        {
                            // Connection still active
                            activeSockets.Add(socket);
                        }
                    }
                    else
                    {
                        // No data available, connection is still good
                        activeSockets.Add(socket);
                    }
                }
                catch (SocketException ex)
                {
                    // Handle socket errors
                    Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: Socket error during eviction");
                    socket.Close();
                    evictedCount++;
                }
            }

            // Return active sockets to the pool
            foreach (var activeSocket in activeSockets)
            {
                clientPool.Add(activeSocket);
            }

            if (evictedCount > 0)
            {
                Log.Information($"{CLASS_NAME}:{METHOD_NAME}: Eviction complete. Evicted {evictedCount} connections. Current pool size: {clientPool.Count}");
            }
        }

        private async Task<bool> CreateAndAddClientAsync()
        {
            const string METHOD_NAME = "CreateAndAddClientAsync()";
            const int CONNECTION_TIMEOUT = 5000;
            Socket client = null;
            try
            {
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);

                // Set the connection timeout
                var connectTask = client.ConnectAsync(endPoint);
                if (await Task.WhenAny(connectTask, Task.Delay(CONNECTION_TIMEOUT)) == connectTask)
                {
                    // Connection successful
                    await connectTask; // Ensure the task is completed
                    clientPool.Add(client);
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME}: INFO: Client connected and added to pool, pool size {clientPool.Count}");
                    return true;
                }
                else
                {
                    // Connection timed out
                    Log.Error($"{CLASS_NAME}:{METHOD_NAME}: Connection attempt timed out after {CONNECTION_TIMEOUT} ms, pool size {clientPool.Count}");
                    client.Close();
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (client != null)
                {
                    client.Close();
                }
                Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: client creation failed, pool size {clientPool.Count}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendDataAsync(byte[] message, int timeoutMilliseconds = 5000)
        {
            if (!isConnectToGaow)
            {
                return false;
            }
            const string METHOD_NAME = "SendDataAsync()";
            /*
            await initSemaphore.WaitAsync();
            try
            {
                if (!isInitialized || clientPool.IsEmpty)
                {
                    await Init();
                }
            }
            finally
            {
                initSemaphore.Release();
            }
            */


            await poolSemaphore.WaitAsync();
            try
            {
                int retries = poolSize;
                bool wasSent = false;
                while (retries-- > 0)
                {
                    if (clientPool.TryTake(out Socket client))
                    {
                        var cts = new CancellationTokenSource(timeoutMilliseconds);
                        try
                        {
                            using (var networkStream = new NetworkStream(client))
                            {
                                var sendTask = networkStream.WriteAsync(message, 0, message.Length, cts.Token);
                                await sendTask;
                            }
                            wasSent = true;

                            // Return client to pool
                            clientPool.Add(client);
                            retries = 0;
                        }
                        catch (OperationCanceledException)
                        {
                            Log.Error($"{CLASS_NAME}:{METHOD_NAME}: Data send operation timed out.");
                            client.Close();
                            retries = 0;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"{CLASS_NAME}:{METHOD_NAME}: error while sending data (remaining retries {retries}): {ex.Message}");
                            client.Close();
                        }
                    }
                    else
                    {
                        Log.Error($"{CLASS_NAME}:{METHOD_NAME}: error while sending data: no available client in the pool.");
                    }
                }
                return wasSent;
            }
            finally
            {
                poolSemaphore.Release();
            }
        }


    }
}
