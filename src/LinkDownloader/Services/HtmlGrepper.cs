using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkDownloader.Services
{
    public class HtmlGrepperOptions
    {
        public string Url { get; set; }
    }

    public class HtmlGrepper
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly HtmlGrepperOptions _options;
        private readonly HttpResponseMessage _response;
        public HtmlGrepper(ILogger<HtmlGrepper> logger,
            IOptions<HtmlGrepperOptions> options)
        {
            _options = options.Value;
            Task<HttpResponseMessage> task = _client.GetAsync(_options.Url);
            task.Wait();
            _response = task.Result;
        }

        public string GetHtmlBody()
        {
            _response.EnsureSuccessStatusCode();
            // string responseBody = 
            var task = _response.Content.ReadAsStringAsync();
            task.Wait();
            return task.Result;
        }

        public string GetRequestUrl()
        {
            return _options.Url;
        }
    }
}
