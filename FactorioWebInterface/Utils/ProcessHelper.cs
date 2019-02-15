using Serilog;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FactorioWebInterface.Utils
{
    public static class ProcessHelper
    {
        public static bool RunProcessToEnd(string fileName, string arguments)
        {
            Log.Logger.Information("RunProcessToEnd filename: {fileName} arguments: {arguments}", fileName, arguments);

            var process = new Process
            {
                StartInfo =
                {
                    FileName = fileName, Arguments = arguments,
                    UseShellExecute = false, CreateNoWindow = true,
                }
            };

            try
            {
                process.Start();
                process.WaitForExit();
                return process.ExitCode > -1;
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "RunProcessToEnd");
                return false;
            }
            finally
            {
                process.Dispose();
            }
        }

        public static Task<bool> RunProcessToEndAsync(string fileName, string arguments)
        {
            Log.Logger.Information("RunProcessToEndAsync filename: {fileName} arguments: {arguments}", fileName, arguments);

            var process = new Process
            {
                StartInfo =
                {
                    FileName = fileName, Arguments = arguments,
                    UseShellExecute = false, CreateNoWindow = true,
                },
                EnableRaisingEvents = true
            };

            var tcs = new TaskCompletionSource<bool>();

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode > -1);
                process.Dispose();
            };

            try
            {
                process.Start();
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "RunProcessToEndAsync");
                tcs.SetResult(false);
                process.Dispose();
            }

            return tcs.Task;
        }
    }
}
