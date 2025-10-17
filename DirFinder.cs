using System.IO;

namespace TempCleaner
{
    public class DirFinder
    {
        public int count = 0;

        private bool HasReadWritePermission(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    // Try opening the file for read/write access
                    using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)) { }
                    return true;
                }
                else if (Directory.Exists(path))
                {
                    // Check read access (non-throwing version)
                    bool canRead = false;
                    try
                    {
                        canRead = Directory.EnumerateFileSystemEntries(path).Any();
                    }
                    catch
                    {
                        canRead = false;
                    }

                    // Check write access with try-catch isolation
                    bool canWrite = false;
                    try
                    {
                        string testFile = Path.Combine(path, Path.GetRandomFileName());
                        using (FileStream fs = File.Create(testFile)) { }
                        File.Delete(testFile);
                        canWrite = true;
                    }
                    catch
                    {
                        canWrite = false;
                    }

                    return canRead && canWrite;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }




        public List<string> DirandFile(string path, Action<FileStatusReport>? report = null)
        {
            var result = new List<string>();

            // Get all files with read/write access
            try
            {
                var allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                foreach (var file in allFiles)
                {
                    if (HasReadWritePermission(file))
                    {
                        result.Add(file);
                        count++;
                        report?.Invoke(new FileStatusReport($"✔ File found: {file}", count));

                    }
                }
            }
            catch (Exception ex)
            {
            }

            // Get all directories with read/write access
            try
            {
                var allDirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
                foreach (var dir in allDirs)
                {
                    if (HasReadWritePermission(dir))
                    {
                        result.Add(dir);
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return result;
        }
    }
}
