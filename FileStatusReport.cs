namespace TempCleaner
{
    public class FileStatusReport
    {
        public string Message { get; set; }
        public int Count { get; set; }

        public FileStatusReport(string message, int count)
        {
            Message = message;
            Count = count;
        }
    }

}
