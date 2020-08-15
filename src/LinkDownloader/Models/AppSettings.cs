namespace LinkDownloader.Models
{
    public class AppSettings
    {
        /// <summary>
        /// Sets header title in console window
        /// </summary>
        public string ConsoleTitle { get; set; }
        public string KeywordInLinks { get; set; }
        public string SavingDirectory { get; set; }
        public string TemporaryDirectory { get; set; }
        public int StartIndexOfLinks { get; set; }
        public int SecondsBeforeTimeout { get; set; }
        public int NumberOfAttemptsPreLink { get; set; }
    }
}
