namespace Botwin.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Net.Http.Headers;
    using Xunit;

    public class ResponseNegotiatorTests
    {
        private TestServer server;

        private HttpClient httpClient;

        public ResponseNegotiatorTests()
        {
            this.server = new TestServer(new WebHostBuilder()
                            .ConfigureServices(x =>
                            {
                                x.AddSingleton<IAssemblyProvider, TestAssemblyProvider>();
                                //x.AddSingleton<IResponseNegotiator, TestResponseNegotiator>();
                                x.AddBotwin();
                            })
                            .Configure(x => x.UseBotwin())
                        );
            this.httpClient = this.server.CreateClient();
        }

        [Fact]
        public async Task Should_use_appropriate_response_negotiator()
        {
            this.httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("foo/bar"));
            var response = await this.httpClient.GetAsync("/negotiate");
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("FOOBAR", body);
        }

        [Fact]
        public async Task Should_fallback_to_json()
        {
            this.httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("not/known"));
            var response = await this.httpClient.GetAsync("/negotiate");
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public async Task Should_fallback_to_json_even_if_no_accept_header()
        {
            var response = await this.httpClient.GetAsync("/negotiate");
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }
    }

    internal class TestResponseNegotiator : IResponseNegotiator
    {
        public bool CanHandle(IList<MediaTypeHeaderValue> accept)
        {
            return accept.Any(x => x.MediaType.Value.IndexOf("foo/bar", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public async Task Handle(HttpRequest req, HttpResponse res, object model)
        {
            await res.WriteAsync("FOOBAR");
        }
    }
}