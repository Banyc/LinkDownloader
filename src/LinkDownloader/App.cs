using System.Diagnostics;
using System.Data.Common;
using System.Timers;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LinkDownloader.Models;
using LinkDownloader.Services;
using System.Net;
using System.Threading.Tasks;
using LinkDownloader.Extensions;

namespace LinkDownloader
{
    public class App
    {
        private readonly ITestService _testService;
        private readonly ILogger<App> _logger;
        private readonly AppSettings _config;
        private readonly HtmlGrepper _grepper;

        public App(ITestService testService,
            IOptions<AppSettings> config,
            ILogger<App> logger,
            HtmlGrepper grepper)
        {
            _testService = testService;
            _logger = logger;
            _config = config.Value;
            _grepper = grepper;
        }

        public void Run()
        {
            _logger.LogInformation($"This is a console application for {_config.ConsoleTitle}");
            _testService.Run();

            string htmlBody = _grepper.GetHtmlBody();
            // Console.WriteLine(htmlBody);

            string pattern = $"(?<=href=\").*?(?i){_config.KeywordInLinks}(?-i).*?(?=\")";

            MatchCollection matches = Regex.Matches(htmlBody, pattern);

            Directory.CreateDirectory(_config.TemporaryDirectory);
            Directory.CreateDirectory(_config.SavingDirectory);

            int indexOfMatches = 0;

            // matchedLinks
            foreach (Match match in matches)
            {
                if (indexOfMatches < _config.StartIndexOfLinks)
                {
                    indexOfMatches++;
                    continue;
                }
                Uri baseUrl = new Uri(_grepper.GetRequestUrl());
                Uri fullUrl = new Uri(baseUrl, match.ToString());
                string filename = Path.GetFileName(fullUrl.AbsoluteUri);
                DownloadFileAsync(fullUrl, filename).Wait();
                indexOfMatches++;
            }
        }

        private async Task DownloadFileAsync(Uri fullUrl, string filename)
        {
            string finalFilePath = Path.Combine(_config.SavingDirectory, filename);
            string tmpFilePath = Path.Combine(_config.TemporaryDirectory, filename);
            // skip if exists
            if (File.Exists(finalFilePath))
            {
                return;
            }
            _logger.LogInformation($"Downloading ({fullUrl.AbsoluteUri})");
            bool isSuccess = false;
            int numberOfAttempts = 0;
            TimeSpan timeCost = TimeSpan.FromTicks(0);
            // BUG: If those fileinfo initialized here, it will be more likely to trigger IO error below
            // FileInfo tmpFileToWrite = new FileInfo(tmpFilePath);
            // FileInfo finalFileToWrite = new FileInfo(finalFilePath);
            while (true)
            {
                // BUG/WORKAROUND: If those fileinfo initialized here, it will seldom trigger IO error below
                FileInfo tmpFileToWrite = new FileInfo(tmpFilePath);
                FileInfo finalFileToWrite = new FileInfo(finalFilePath);
                // busy waiting
                while (tmpFileToWrite.Exists && tmpFileToWrite.IsFileLocked())
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                if (isSuccess)
                {
                    while (finalFileToWrite.Exists && finalFileToWrite.IsFileLocked())
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                    // finalFileToWrite.Delete();
                    // File.Move(tmpFilePath, finalFilePath);
                    tmpFileToWrite.MoveTo(finalFilePath, true);
                    _logger.LogInformation($"Time cost: {timeCost.TotalSeconds}s. ({fullUrl.AbsoluteUri})");
                    break;
                }
                if (numberOfAttempts >= _config.NumberOfAttemptsPreLink)
                {
                    // File.Delete(tmpFilePath);
                    tmpFileToWrite.Delete();
                    _logger.LogError($"Run out of attempts. ({fullUrl.AbsoluteUri})");
                    break;
                }
                try
                {
                    // File.Delete(tmpFilePath);
                    tmpFileToWrite.Delete();
                    using (var client = new WebDownload((int)TimeSpan.FromSeconds(_config.SecondsBeforeTimeout).TotalMilliseconds))
                    {
                        var stopwatch = new Stopwatch();
                        // using (var timer = new Timer())
                        // {
                        // timer.Interval = TimeSpan.FromSeconds(_config.SecondsBeforeTimeout).TotalMilliseconds;
                        // timer.Start();
                        var downloadTask = client.DownloadFileTaskAsync(fullUrl, tmpFilePath);
                        // bool isTimeout = false;
                        // timer.Elapsed += (sender, e) => this.Timeout(sender, e, downloadTask, out isTimeout);
                        // client.DownloadFile(fullUrl, filePath);
                        stopwatch.Start();
                        downloadTask.Wait(TimeSpan.FromSeconds(_config.SecondsBeforeTimeout));
                        stopwatch.Stop();
                        timeCost = stopwatch.Elapsed;
                        // timer.Stop();
                        // timeCostInSeconds = _config.SecondsBeforeTimeout - TimeSpan.FromMilliseconds(timer.Interval).TotalSeconds;
                        // isSuccess = !isTimeout;
                        isSuccess = downloadTask.IsCompleted;
                        // }
                    }
                }
                catch (IOException)
                {
                    throw;
                }
                // // BUG: not catching
                // catch (WebException)
                // {
                //     // System.Net.WebException : The operation has timed out.
                //     isSuccess = false;
                // }
                catch (Exception)
                {
                    isSuccess = false;
                }
                if (!isSuccess)
                {
                    _logger.LogError($"Timeout! ({fullUrl.AbsoluteUri})");
                }
                numberOfAttempts++;
            }
        }

        // private void Timeout(object sender, ElapsedEventArgs e, Task downloadTask, out bool isTimeout)
        // {
        //     downloadTask.Dispose();
        //     isTimeout = true;
        //     _logger.LogError($"Timeout!");
        // }
    }
}
