using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FactorioWebInterface.Models
{
    public class SaveDeflater
    {
        private static readonly string FILE_MISSING_MESSAGE = "error('File was removed to decrease save file size. Please visit https://github.com/Refactorio/RedMew if you wish to download this scenario.')";
        private static readonly Regex luaPathRegex = new Regex(@"(?<=.*/)(.*)", RegexOptions.Compiled);

        private static readonly string scanScript = "global={} serpent={} local _require = require require = function(args) local success, result = pcall(_require, args) if success then return result else return {} end end local zero_function=function() return 0 end table_size=function(tbl) local count=0 for _,_ in pairs(tbl or{} ) do count=count + 1 end return count end script=setmetatable({} ,{__index=function() return zero_function end} ) defines={events=setmetatable({} ,{__index=zero_function} ) , } commands={add_command=function() end} FILES={} local _print=print print=function() end require 'control' for _,s in pairs(FILES) do _print(s) end";

        private readonly ILogger<FactorioServerManager> logger;
        private Dictionary<string, string[]> luaFileRequirePaths;

        public SaveDeflater(ILogger<FactorioServerManager> logger)
        {
            this.logger = logger;
        }

        public void Deflate(string path)
        {
            var saveDir = path.Substring(0, path.Length - 4);
            try
            {
                if (Directory.Exists(saveDir))
                    Directory.Delete(saveDir, true);
                ZipFile.ExtractToDirectory(path, saveDir);
                var files = Directory.GetFileSystemEntries(saveDir, "*", SearchOption.TopDirectoryOnly);
                if (files.Length == 1 && Directory.Exists(files[0].ToString()))
                {
                    var root = files[0].ToString();
                    copyScriptFiles(root);
                    injectPayload(root);
                    var requiredFiles = scanRequiredFiles(root);
                    logger.LogInformation(requiredFiles.ToString());
                    removeNotRequiredFiles(path, requiredFiles);
                } else
                {
                    logger.LogWarning("There was an error while reading Archive: Corrupted save. Number of files in top level of archive: " + files.Length);

                }
            }
            catch (Exception e)
            {
                logger.LogError("There was an error while reading Archive: " + e);
            }
            finally
            {
                if (Directory.Exists(saveDir))
                    Directory.Delete(saveDir, true);
            }
        }
        
        private void injectPayload(string path) {
            var files = Directory.GetFileSystemEntries(path, "*.lua", SearchOption.AllDirectories);
            List<Task> tasks = new List<Task>();
            foreach (var file in files)
            {
                var t = new Task(() => {
                    var relativePath = Path.GetFullPath(file).Substring(Path.GetFullPath(path).Length);
                    if (Path.GetDirectoryName(file) == path && (relativePath.Contains("scanner.lua") || relativePath.Contains("util.lua") || relativePath.Contains("inject.lua")))
                        return;
                    string content = File.ReadAllText(file);
                    content = String.Format("table.insert(FILES, '{0}')\n{1}", relativePath.Replace('\\', '/').Substring(1), content);
                    File.WriteAllText(file, content);
                });
                t.Start();
                tasks.Add(t);
            }

            Task.WaitAll(tasks.ToArray());
        }
        private string[] scanRequiredFiles(string path)
        {
            logger.LogInformation(path);
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c cd " + path + "; lua scanner.lua; ls /factorio/",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                }
            };

            proc.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    logger.LogInformation(e.Data);
                }
            };

            proc.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    logger.LogError("[Error] " + e.Data);
                }
            };
            
            proc.Start();
            logger.LogInformation("started");


            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            proc.WaitForExit();
            logger.LogInformation("done");

            return new string[] { "control.lua", "config.lua", "utils/utils.lua" }; //TODO: Replace this with the process output
        }

        private void copyScriptFiles(string path)
        {
            File.Copy("/factorio/1/data/core/lualib/util.lua", path + "/util.lua");
            File.Copy("/factorio/1/data/core/lualib/inspect.lua", path + "/inspect.lua");
            File.WriteAllText(path + "/scanner.lua", scanScript);
        }

        private void removeNotRequiredFiles(string path, string[] requiredFiles)
        {
            try
            {
                using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Update))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        var luaPath = luaPathRegex.Match(entry.FullName).ToString().Trim();
                        if (!luaPath.EndsWith(".cfg") && !luaPath.EndsWith(".dat") && !luaPath.Equals("LICENSE") && !luaPath.Equals("preview.png") && !luaPath.EndsWith(".json"))
                        {
                            if (luaPath.Contains("control"))
                            {
                                logger.LogInformation(luaPath + " " + requiredFiles.Any(luaPath.Equals).ToString());
                                logger.LogInformation(requiredFiles[0]);
                            }
                            if (!requiredFiles.Any(luaPath.Equals))
                            {
                                using (Stream stream = entry.Open())
                                {
                                    stream.SetLength(FILE_MISSING_MESSAGE.Length);
                                    using (StreamWriter writer = new StreamWriter(stream))
                                    {
                                        try
                                        {
                                            writer.Write(FILE_MISSING_MESSAGE);
                                        }
                                        catch (IOException)
                                        {
                                            logger.LogError("Could not override file: " + entry.FullName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                logger.LogError("Could not open archive: " + path);
            }
        }
    }
}
