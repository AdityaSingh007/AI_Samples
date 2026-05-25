namespace CAE_Log_Agent.Utilities
{
    public static class FolderUtility
    {
        private static readonly string BaseFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Agent_Temp");

        public static async Task<bool> EmptyAgentTempFolder()
        {
            try
            {
                if (!Directory.Exists(BaseFolderPath))
                {
                    throw new DirectoryNotFoundException($"The directory '{BaseFolderPath}' does not exist.");
                }

                foreach (var filePath in Directory.EnumerateFiles(BaseFolderPath, "*", SearchOption.AllDirectories))
                {
                    File.Delete(filePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error emptying folder: {ex.Message}");
                return false;
            }
        }
    }
}
