namespace TempCleaner
{
    /// <summary>
    /// Lightweight struct for file status reporting to avoid heap allocations
    /// </summary>
    public readonly struct FileStatusReport
    {
        public int Count { get; }

        public FileStatusReport(int count)
        {
            Count = count;
        }
    }
}
