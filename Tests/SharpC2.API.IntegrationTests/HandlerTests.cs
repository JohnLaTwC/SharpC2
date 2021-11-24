using System.Net.Http.Json;
using System.Threading.Tasks;

using SharpC2.API.V1;
using SharpC2.API.V1.Responses;

using Xunit;

namespace SharpC2.API.IntegrationTests
{
    public class HandlerTests : IntegrationTest
    {
        [Fact]
        public async Task GetHandlers()
        {
            var response = await Client.GetAsync(Routes.V1.Handlers);
            var json = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(json);
            Assert.True(json.Length > 20);
        }

        [Fact]
        public async Task GetHandler()
        {
            const string handlerName = "default-http";
            
            var response = await Client.GetAsync($"{Routes.V1.Handlers}/{handlerName}");
            var handler = await response.Content.ReadFromJsonAsync<HandlerResponse>();
            
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(handler);
            Assert.Equal(handlerName, handler.Name);
        }
    }
}