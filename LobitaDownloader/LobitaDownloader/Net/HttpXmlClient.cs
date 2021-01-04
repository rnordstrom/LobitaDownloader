using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml;

namespace LobitaDownloader
{
    public class HttpXmlClient
    {
        private HttpClient client = new HttpClient();

        public HttpXmlClient(string uri)
        {
            client.BaseAddress = new Uri(uri);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/xml"));
        }

        public async Task<XmlElement> GetPosts(string path)
        {
            XmlElement result = null;
            HttpResponseMessage response = await client.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                result = await response.Content.ReadAsAsync<XmlElement>();
            }

            return result;
        }
    }
}
