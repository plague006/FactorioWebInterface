using FactorioWrapperInterface;
using Microsoft.AspNetCore.SignalR.Client;
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

        private static SemaphoreSlim LogLock = new SemaphoreSlim(1, 1);

        private static volatile bool exit = false;
        private static volatile HubConnection connection;
        private static volatile bool connected = false;
        private static volatile Process factorioProcess;
        private static string factorioFileName;
        private static string factorioArguments;
        private static string serverId;

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            if (args.Length < 2)
            {
                await LogAsync("Missing arguments");
                return;
            }

            serverId = args[0];
            factorioFileName = args[1];
            factorioArguments = string.Join(" ", args, 2, args.Length - 2);

            await LogAsync($"Starting wrapper {serverId} {factorioFileName} {factorioArguments}");

            while (!exit)
            {
                try
                {
                    await RestartWrapperAsync();
                }
                catch (Exception e)
                {
                    var message = $"Wrapper Exception\n{e.ToString()}\n";
                    await LogAsync(message);
                }
            }

            SendWrapperData("Exiting wrapper");
            await LogAsync("Exiting wrapper");
        }

        private static async Task RestartWrapperAsync()
        {
            connection = new HubConnectionBuilder()
                .WithUrl(url)
                .Build();

            connection.Closed += async (error) =>
            {
                connected = false;
                await LogAsync("Lost connection");
                await Reconnect();
            };

            connection.On<string>(nameof(IClientMethods.SendToFactorio), async data =>
             {
                 try
                 {
                     var p = factorioProcess;
                     if (p != null && !p.HasExited)
                     {
                         await LogAsync(data);
                         p.StandardInput.WriteLine(data);
                     }
                 }
                 catch (Exception e)
                 {
                     await LogAsync($"Error sending data to factorio process\n{e.ToString()}");
                 }
             });

            connection.On(nameof(IClientMethods.Stop), async () =>
            {
                try
                {
                    Process.Start("kill", $"-2 {factorioProcess.Id}");
                    await Task.Delay(2000);
                    exit = true;
                }
                catch (Exception e)
                {
                    await LogAsync(e.ToString());
                }
            });

            connection.On(nameof(IClientMethods.ForceStop), async () =>
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
                    await LogAsync(e.ToString());
                }
            });

            await Reconnect();

            if (factorioProcess == null)
            {
                await StartFactorioProcess();
            }

            while (!exit)
            {
                if (factorioProcess.HasExited)
                {
                    await LogAsync("Factorio process exited");
                    exit = true;
                    return;
                }
                await Task.Delay(1000);
            }
        }

        private static async Task StartFactorioProcess()
        {
            await LogAsync($"Starting factorio process\nfileName: {factorioFileName} arguments: {factorioArguments} ");

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
                await LogAsync(e.ToString());
                exit = true;
                return;
            }

            factorioProcess.BeginOutputReadLine();
            factorioProcess.BeginErrorReadLine();

            await LogAsync("Started factorio process");
        }

        private static void SendFactorioOutputData(string data)
        {
            if (!connected)
            {
                return;
            }

            try
            {
                connection.SendAsync(nameof(IServerMethods.SendFactorioOutputData), data);
            }
            catch (Exception)
            {

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
                connection.SendAsync(nameof(IServerMethods.SendWrapperData), data);
            }
            catch (Exception)
            {

            }
        }

        private static async Task LogAsync(string data)
        {
            await LogLock.WaitAsync();
            try
            {
                var time = DateTime.UtcNow;
                await File.AppendAllTextAsync("wrapperawait LogAsync.txt", $"{time}: {data}\n");
            }
            finally
            {
                LogLock.Release();
            }
        }

        private static async Task<bool> TryConnectAsync(HubConnection hubConnection)
        {
            try
            {
                await hubConnection.StartAsync();
                await hubConnection.InvokeAsync(nameof(IServerMethods.RegisterServerId), serverId);
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
            await LogAsync("Connected");
        }
    }
}
