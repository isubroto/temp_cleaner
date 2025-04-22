using System.IO;

namespace TempCleaner
{
    public class DirFinder
    {
        private bool HasReadWritePermission(string path)
        {
            try
            {
                // Try opening for read/write to test access
                if (File.Exists(path))
                {
                    using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) { }
                }
                else if (Directory.Exists(path))
                {
                    string testFile = Path.Combine(path, Path.GetRandomFileName());
                    File.Create(testFile).Close();
                    File.Delete(testFile);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }


        public List<string> DirandFile(string path)
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
                    }
                }
            }
            catch (Exception) { }

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
            catch (Exception) { }

            return result;
        }



    }
}
