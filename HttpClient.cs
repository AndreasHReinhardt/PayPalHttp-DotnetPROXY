using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;

namespace PayPalHttp
{
    public class HttpClient
    {
        public Encoder Encoder { get; }

        protected Environment environment;
        private System.Net.Http.HttpClient client;
        private List<IInjector> injectors;

        public System.Net.IWebProxy Proxy { get; set; }

        public HttpClient(Environment environment, System.Net.IWebProxy Proxy2Use)
        {
            this.Proxy = Proxy2Use;
            this.environment = environment;
            this.injectors = new List<IInjector>();
            this.Encoder = new Encoder();

            HttpClientHandler httpClientHandler = new HttpClientHandler();
            /*{
                Proxy = this.Proxy ,
            };*/
            if (this.Proxy != null)
            {
                httpClientHandler.Proxy = this.Proxy;
                httpClientHandler.Credentials = this.Proxy.Credentials;
            }
            else
            { 
                throw new Exception ( "No FORKING PROXY");
            }
            client = new System.Net.Http.HttpClient(handler: httpClientHandler, disposeHandler: true);
           
            //client = new System.Net.Http.HttpClient();
            
            client.BaseAddress = new Uri(environment.BaseUrl());
            client.DefaultRequestHeaders.Add("User-Agent", GetUserAgent());
        }

        protected virtual string GetUserAgent()
        {
            return "PayPalHttp-Dotnet HTTP/1.1";
        }

        public void AddInjector(IInjector injector)
        {
            if (injector != null)
            {
                this.injectors.Add(injector);
            }
        }

        public void SetConnectTimeout(TimeSpan timeout)
        {
            client.Timeout = timeout;
        }

        public virtual async Task<HttpResponse> Execute<T>(T req) where T: HttpRequest
        {
            var request = req.Clone<T>();

            foreach (var injector in injectors) {
                injector.Inject(request);
            }

            request.RequestUri = new Uri(this.environment.BaseUrl() + request.Path);

            if (request.Body != null)
            {
                request.Content = Encoder.SerializeRequest(request);
            }
            
			var response = await client.SendAsync(request);

            
            if (response.IsSuccessStatusCode)
            {
                object responseBody = null;
                if (response.Content.Headers.ContentType != null)
                {
                    responseBody = Encoder.DeserializeResponse(response.Content, request.ResponseType);
                }
                return new HttpResponse(response.Headers, response.StatusCode, responseBody);
            }
            else
            {
				var responseBody = await response.Content.ReadAsStringAsync();
				throw new HttpException(response.StatusCode, response.Headers, responseBody);
            }
        }
    }
}
