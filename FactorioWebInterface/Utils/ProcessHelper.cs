using Serilog;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FactorioWebInterface.Utils
{
    public static class ProcessHelper
    {
        /// <summary>
        /// Runs a process until it ends. Returns true if successful.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="arguments"></param>        
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
                return process.ExitCode == 0;
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

        /// <summary>
        /// Asynchronous runs a process until it ends. Returns true if successful.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="arguments"></param> 
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
                tcs.TrySetResult(process.ExitCode == 0);
                process.Dispose();
            };

            try
            {
                process.Start();
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "RunProcessToEndAsync");
                tcs.TrySetResult(false);
                process.Dispose();
            }

            return tcs.Task;
        }

        /// <summary>
        /// Asynchronous runs a process until it ends. Returns true if successful.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="arguments"></param> 
        /// <param name="cancellationToken"></param> 
        public static Task<bool> RunProcessToEndAsync(string fileName, string arguments, CancellationToken cancellationToken)
        {
            Log.Logger.Information("RunProcessToEndAsync filename: {fileName} arguments: {arguments}", fileName, arguments);

            if (cancellationToken.IsCancellationRequested)
            {
                Log.Logger.Information("RunProcessToEndAsync Cancelled before start");
                return Task.FromResult(false);
            }

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

            CancellationTokenRegistration registration = default;
            registration = cancellationToken.Register(() =>
            {
                try
                {
                    tcs.TrySetResult(false);
                    process.Kill();
                }
                catch
                {

                }
                finally
                {
                    Log.Logger.Information("RunProcessToEndAsync Cancelled");
                    process.Dispose();
                    registration.Dispose();
                }
            });

            process.Exited += (sender, args) =>
            {
                tcs.TrySetResult(process.ExitCode == 0);
                process.Dispose();
                registration.Dispose();
            };

            try
            {
                process.Start();
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "RunProcessToEndAsync");
                tcs.TrySetResult(false);
                process.Dispose();
                registration.Dispose();
            }

            return tcs.Task;
        }
    }
}
