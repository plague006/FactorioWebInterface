using FactorioWrapperInterface;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FactorioWrapper
{
    class FactorioWrapper
    {
        //private static readonly string url = "https://localhost:44303/ServerHub";
        private static readonly string url = "http://54.38.76.175/ServerHub";

        // This is to stop multiple threads writing to the factorio process concurrently.
        private static SemaphoreSlim factorioProcessLock = new SemaphoreSlim(1, 1);

        private static volatile bool exit = false;
        private static volatile HubConnection connection;
        private static volatile bool connected = false;
        private static volatile Process factorioProcess;
        private static string factorioFileName;
        private static string factorioArguments;
        private static string serverId;

        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            path = Path.Combine(path, "logs/log.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Async(a => a.File(path, rollingInterval: RollingInterval.Day))
                .CreateLogger();

            if (args.Length < 2)
            {
                Log.Fatal("Missing arguments");
                return;
            }

            serverId = args[0];
            factorioFileName = args[1];
            factorioArguments = string.Join(" ", args, 2, args.Length - 2);

            Log.Information("Starting wrapper serverId: {serverId} factorioFileName: {factorioFileName} factorioArguments: {factorioArguments}", serverId, factorioFileName, factorioArguments);

            while (!exit)
            {
                try
                {
                    await RestartWrapperAsync();
                }
                catch (Exception e)
                {
                    Log.Error(e, "Wrapper Exception");
                }
            }

            SendWrapperData("Exiting wrapper");
            Log.Information("Exiting wrapper");

            Log.CloseAndFlush();
        }

        private static async Task RestartWrapperAsync()
        {
            if (connection == null)
            {
                BuildConenction();
            }

            await Reconnect();

            if (factorioProcess == null)
            {
                StartFactorioProcess();
            }

            while (!exit)
            {
                if (factorioProcess.HasExited)
                {
                    Log.Information("Factorio process exited");
                    exit = true;
                    return;
                }
                await Task.Delay(1000);
            }
        }

        private static void BuildConenction()
        {
            connection = new HubConnectionBuilder()
                .WithUrl(url)
                .Build();

            connection.Closed += async (error) =>
            {
                connected = false;
                Log.Information("Lost connection");
                await Reconnect();
            };

            connection.On<string>(nameof(IFactorioProcessClientMethods.SendToFactorio), async data =>
            {
                try
                {
                    await factorioProcessLock.WaitAsync();

                    var p = factorioProcess;
                    if (p != null && !p.HasExited)
                    {
                        await p.StandardInput.WriteLineAsync(data);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error sending data to factorio process");
                }
                finally
                {
                    factorioProcessLock.Release();
                }
            });

            connection.On(nameof(IFactorioProcessClientMethods.Stop), async () =>
            {
                try
                {
                    Process.Start("kill", $"-2 {factorioProcess.Id}");
                    await Task.Delay(2000);
                    exit = true;
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error stopping factorio process");
                }
            });

            connection.On(nameof(IFactorioProcessClientMethods.ForceStop), () =>
            {
                try
                {
                    var p = factorioProcess;
                    if (p != null && !p.HasExited)
                    {
                        p.Kill();
                    }
                    exit = true;
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error force stopping factorio process");
                }
            });
        }

        private static void StartFactorioProcess()
        {
            Log.Information("Starting factorio process factorioFileName: {factorioFileName} factorioArguments: {factorioArguments}", factorioFileName, factorioArguments);

            factorioProcess = new Process();
            var startInfo = factorioProcess.StartInfo;
            startInfo.FileName = factorioFileName;
            startInfo.Arguments = factorioArguments;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            factorioProcess.OutputDataReceived += (s, e) =>
            {
                SendFactorioOutputData(e.Data);
            };

            factorioProcess.ErrorDataReceived += (s, e) =>
            {
                SendFactorioOutputData("Error " + e.Data);
            };

            try
            {
                factorioProcess.Start();
            }
            catch (Exception e)
            {
                Log.Error(e, "Error starting factorio process");
                exit = true;
                return;
            }

            factorioProcess.BeginOutputReadLine();
            factorioProcess.BeginErrorReadLine();

            factorioProcess.StandardInput.AutoFlush = true;

            Log.Information("Started factorio process");
        }

        private static void SendFactorioOutputData(string data)
        {
            if (!connected)
            {
                return;
            }

            try
            {
                connection.SendAsync(nameof(IFactorioProcessServerMethods.SendFactorioOutputData), data);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error sending factorio output data");
            }
        }

        private static void SendWrapperData(string data)
        {
            if (!connected)
            {
                return;
            }

            try
            {
                connection.SendAsync(nameof(IFactorioProcessServerMethods.SendWrapperData), data);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error sending wrapper output data");
            }
        }

        private static async Task<bool> TryConnectAsync(HubConnection hubConnection)
        {
            try
            {
                await hubConnection.StartAsync();
                await hubConnection.InvokeAsync(nameof(IFactorioProcessServerMethods.RegisterServerId), serverId);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static async Task Reconnect()
        {
            while (!await TryConnectAsync(connection))
            {
                await Task.Delay(1000);
            }
            connected = true;
            Log.Information("Connected");
        }
    }
}
