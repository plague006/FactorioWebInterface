using FactorioWrapperInterface;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FactorioWrapper
{
    class FactorioWrapper
    {
        private static volatile bool exit = false;
        private static HubConnection connection;
        private static volatile bool connected = false;
        private static Process factorioProcess;
        private static string factorioFileName;
        private static string factorioArguments;
        private static string serverId;

        static void Main(string[] args)
        {
            args = new string[]
            {
                "1",
                "C:/factorio/Factorio1/bin/x64/factorio.exe",
                "--start-server",
                "C:/factorio/Factorio1/bin/x64/test.zip",
                "--server-settings",
                "C:/factorio/Factorio1/bin/x64/server-settings.json",
                "--console-log",
                "C:/factorio/Factorio1/bin/x64/log1.log",
                "--no-log-rotation"
            };


            if (args.Length < 2)
            {
                Log("Missing arguments");
                return;
            }

            serverId = args[0];
            factorioFileName = args[1];
            factorioArguments = string.Join(" ", args, 2, args.Length - 2);

            Log($"Starting wrapper {factorioFileName} {factorioArguments}");

            while (!exit)
            {
                try
                {
                    RestartWrapperAsync().GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    var message = $"Wrapper Exception\n{e.ToString()}\n";
                    Log(message);
                }
            }

            SendWrapperData("Exiting wrapper");
            Log("Exiting wrapper");
        }

        private static async Task RestartWrapperAsync()
        {
            connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:44303/ServerHub")
                .Build();

            connection.Closed += async (error) =>
            {
                connected = false;
                Log("Lost connection");
                await Reconnect();
            };

            connection.On<string>(nameof(IClientMethods.SendToFactorio), async data =>
             {
                 try
                 {
                     var p = factorioProcess;
                     if (p != null && !p.HasExited)
                     {
                         await p.StandardInput.WriteLineAsync(data);
                     }
                 }
                 catch (Exception e)
                 {
                     Log($"Error sending data to factorio process\n{e.ToString()}");
                 }
             });

            connection.On(nameof(IClientMethods.Stop), async () =>
            {
                try
                {
                    Process.Start("kill", $"-2 {factorioProcess.Id}");
                }
                catch (Exception)
                {

                }
                await Task.Delay(2000);
                exit = true;
            });

            connection.On(nameof(IClientMethods.ForceStop), () =>
            {
                var p = factorioProcess;
                if (p != null && !p.HasExited)
                {
                    try
                    {
                        p.Kill();
                    }
                    catch (Exception)
                    {

                    }
                }
                exit = true;
            });

            await Reconnect();

            if (factorioProcess == null)
            {
                StartFactorioProcess();
            }

            while (!exit)
            {
                if (factorioProcess.HasExited)
                {
                    Log("Factorio process exited");
                    exit = true;
                    return;
                }
                await Task.Delay(1000);
            }
        }

        private static void StartFactorioProcess()
        {
            factorioProcess = new Process();
            var startInfo = factorioProcess.StartInfo;
            //startInfo.Arguments = "--start-server-load-latest --server-settings /factorio/factorio/server-settings.json";
            //startInfo.FileName = "/factorio/factorio/bin/x64/factorio";
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
                Log(e.ToString());
                exit = true;
                return;
            }

            factorioProcess.BeginOutputReadLine();
            factorioProcess.BeginErrorReadLine();
        }

        private static void SendFactorioOutputData(string data)
        {
            if (!connected)
            {
                return;
            }

            try
            {
                connection.SendAsync(nameof(IServerMethods.SendFactorioOutputData), serverId, data);
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
                connection.SendAsync(nameof(IServerMethods.SendWrapperData), serverId, data);
            }
            catch (Exception)
            {

            }
        }

        private static void Log(string data)
        {
            var time = DateTime.UtcNow;
            File.AppendAllTextAsync("log.txt", $"{time}: {data}\n");
        }

        private static async Task<bool> TryConnectAsync(HubConnection hubConnection)
        {
            try
            {
                await hubConnection.StartAsync();
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
            Log("Connected");
        }
    }
}
