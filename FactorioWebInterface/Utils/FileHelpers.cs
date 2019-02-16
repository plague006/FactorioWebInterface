namespace FactorioWebInterface.Utils
{
    public static class FileHelpers
    {
        public static bool CreateDirectorySymlink(string targetPath, string linkPath)
        {
#if WINDOWS
            return ProcessHelper.RunProcessToEnd("cmd.exe", $"/C MKLINK /D \"{linkPath}\" \"{targetPath}\"");
#else
            return ProcessHelper.RunProcessToEnd("/bin/ln", $"-s {targetPath} {linkPath}");      
#endif
        }

        public static bool IsSymbolicLink(string filePath)
        {
#if WINDOWS
            // Todo implement IsSymbolicLink for windows.
            return false; // Hack: Assume it's not a symbolic link so that it will be deleted and recreated as one.
#else
            return ProcessHelper.RunProcessToEnd("/bin/bash", $"-c \"if [ ! -L {filePath} ] ; then exit 1 ; fi\"");
#endif
        }
    }
}
