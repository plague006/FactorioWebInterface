using DSharpPlus.Entities;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class FactorioServer
    {
        public enum FactorioServerStatus
        {
            Stopped,
            Crashed,
            Stopping,
            Starting,
            Killing,
            Killed,
            Updating,
            Updated,
            Running,
        }

        private Process process;
        private IDiscordBot bot;
        private DiscordChannel channel;

        public int ServerId { get; private set; }
        public FactorioServerStatus Status { get; private set; }

        public event EventHandler<FactorioServerStatus> StatusChanged;
        public event EventHandler<string> MessageReceived;

        public FactorioServer(int serverId)
        {
            ServerId = serverId;
        }

        public async Task Init(IDiscordBot bot)
        {
            channel = await bot.DiscordClient.GetChannelAsync(487652968221376531);

            bot.DiscordClient.MessageCreated += async e =>
            {
                if (e.Author.IsBot)
                {
                    return;
                }

                if (e.Message.ChannelId == 487652968221376531)
                {
                    SendMessage(e.Message.Content);
                }
            };
        }

        public void InitFromProcessId(int processId)
        {

        }

        public void SendMessage(string message)
        {
            if (process == null)
            {
                return;
            }

            var input = process.StandardInput;
            input.WriteLine(message);
        }

        public async Task Start(int id)
        {
            var startInfo = new ProcessStartInfo();
            //startInfo.Arguments = "--start-server-load-latest --server-settings /factorio/factorio/server-settings.json";
            //startInfo.FileName = "/factorio/factorio/bin/x64/factorio";
            //startInfo.Arguments = "--start-server C:\\factorio\\Factorio1\\bin\\x64\\test.zip --server-settings C:\\factorio\\Factorio1\\bin\\x64\\server-settings.json --console-log C:\\factorio\\Factorio1\\bin\\x64\\log1.log --no-log-rotation --port 34101";
            //startInfo.FileName = "C:\\factorio\\Factorio1\\bin\\x64\\factorio.exe";

            startInfo.FileName = "/usr/bin/dotnet";
            startInfo.Arguments = "/var/aspnetcore/FactorioWrapper/FactorioWrapper.dll";

            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            if (id == 0)
            {
                process = Process.Start(startInfo);

                process.OutputDataReceived += (s, e) =>
                {
                    int index = e.Data.IndexOf("[CHAT]");
                    if (index >= 0)
                    {
                        var message = e.Data.Substring(index);
                        channel.SendMessageAsync(message);
                    }

                    Debug.WriteLine(e.Data);
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    Debug.WriteLine("Error: " + e.Data);
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                return;
            }
            else
            {
                process = Process.GetProcessById(id);

                if (process == null)
                    throw null;

                if (process.HasExited)
                    throw new Exception("process has exited");

                while (true)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    await channel.SendMessageAsync(line);
                }
            }
        }

        public void Stop()
        {
            Process stopProcess = new Process();
            var killStartInfo = stopProcess.StartInfo;
            killStartInfo.FileName = "kill";
            killStartInfo.Arguments = "-2 " + process.Id;
            stopProcess.Start();
        }

        public void Kill()
        {
            process.Kill();
        }

        public void Update()
        {

        }

        public struct FactorioServerResources
        {
            public TimeSpan totalProcessorTime;
            public long memory;
        }

        public FactorioServerResources GetResourceUsage()
        {
            process.Refresh();

            return new FactorioServerResources()
            {
                totalProcessorTime = process.TotalProcessorTime,
                memory = process.WorkingSet64,
            };
        }
    }
}
