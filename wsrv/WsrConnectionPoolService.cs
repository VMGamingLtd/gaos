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

        public WsrConnectionPoolService(string ipAddress = "127.0.0.1", int port = 3000, int poolSize = 5)
        {
            this.ipAddress = ipAddress;
            this.port = port;
            this.poolSize = poolSize;
            this.clientPool = new ConcurrentBag<Socket>();
            this.poolSemaphore = new SemaphoreSlim(poolSize);
            this.initSemaphore = new SemaphoreSlim(1);
            this.ensureConnectionsSemathore = new SemaphoreSlim(1);
            this.isInitialized = false;
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
            finally
            {
                initSemaphore.Release();
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
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
            Log.Debug($"{CLASS_NAME}:{METHOD_NAME}: Checking connections. Current pool size: {clientPool.Count}");
            await ensureConnectionsSemathore.WaitAsync();
            try
            {

                EvictConnections();

                var missingConnections = poolSize - clientPool.Count;
                if (missingConnections <= 0) return;

                Log.Information($"{CLASS_NAME}:{METHOD_NAME}: Adding {missingConnections} missing connections");

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
            Log.Debug($"{CLASS_NAME}:{METHOD_NAME}: Evicting closed connections");

            var activeSockets = new ConcurrentBag<Socket>();
            int evictedCount = 0;
            int checkedCount = 0;

            while (clientPool.TryTake(out Socket socket))
            {
                checkedCount++;
                try
                {
                    if (socket.Poll(50, SelectMode.SelectRead))
                    {
                        byte[] buff = new byte[1];
                        if (socket.Receive(buff, SocketFlags.Peek) == 0)
                        {
                            // Connection closed
                            socket.Close();
                            evictedCount++;
                            Log.Debug($"{CLASS_NAME}:{METHOD_NAME}: Evicted closed connection");
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

            Log.Information($"{CLASS_NAME}:{METHOD_NAME}: Eviction complete. Checked {checkedCount} connections. Evicted {evictedCount} connections. Current pool size: {clientPool.Count}");
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
                    Log.Information($"{CLASS_NAME}:{METHOD_NAME}: Client connected and added to pool, pool size {clientPool.Count}");
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

        public async Task SendDataAsync(byte[] message, int timeoutMilliseconds = 5000)
        {
            const string METHOD_NAME = "SendDataAsync()";
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


            await poolSemaphore.WaitAsync();
            try
            {
                int retries = poolSize;
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
            }
            finally
            {
                poolSemaphore.Release();
            }
        }


    }
}
