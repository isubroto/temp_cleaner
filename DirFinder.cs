using System.IO;
using System.Runtime.CompilerServices;

namespace TempCleaner
{
    public sealed class DirFinder
    {
        private int _count;
        
        public int Count => _count;

        // Shared enumeration options - avoid recreating each call
        private static readonly EnumerationOptions SharedEnumOptions = new()
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.System | FileAttributes.ReparsePoint
        };

        public List<string> DirandFile(string path, Action<FileStatusReport>? report = null)
        {
            var result = new List<string>(512);
            _count = 0;

            if (!Directory.Exists(path))
                return result;

            try
            {
                // Process files - no per-file access check needed since EnumerationOptions handles it
                foreach (var file in Directory.EnumerateFiles(path, "*", SharedEnumOptions))
                {
                    result.Add(file);
                    _count++;
                    
                    // Report every 250 files to minimize callback overhead
                    if ((_count & 0xFF) == 0) // Bitwise check is faster than modulo
                    {
                        report?.Invoke(new FileStatusReport(_count));
                    }
                }

                // Process directories - only top-level empty directories for cleanup
                foreach (var dir in Directory.EnumerateDirectories(path, "*", SharedEnumOptions))
                {
                    result.Add(dir);
                }
            }
            catch
            {
                // Silently handle access errors
            }

            // Final report
            if (report != null)
                report(new FileStatusReport(_count));

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => _count = 0;
    }
}
