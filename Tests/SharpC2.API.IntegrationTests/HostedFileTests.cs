using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

using SharpC2.API.V1;
using SharpC2.API.V1.Requests;

using Xunit;

namespace SharpC2.API.IntegrationTests
{
    public class HostedFileTests : IntegrationTest
    {
        private const string TestFile = "dGhpcyBpcyBhIHRlc3QgZmlsZQ==";
        
        [Fact]
        public async Task AddFile()
        {
            var request = new AddHostedFileRequest
            {
                Content = Convert.FromBase64String(TestFile),
                Filename = "test.txt"
            };

            var response = await Client.PostAsJsonAsync(Routes.V1.HostedFiles, request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }
}