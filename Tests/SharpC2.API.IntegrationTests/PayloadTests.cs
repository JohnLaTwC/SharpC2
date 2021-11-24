using System;
using System.Net.Http.Json;
using System.Threading.Tasks;

using SharpC2.API.V1;
using SharpC2.API.V1.Responses;

using Xunit;

namespace SharpC2.API.IntegrationTests
{
    public class PayloadTests : IntegrationTest
    {
        [Fact]
        public async Task GenerateExePayload()
        {
            var payload = await GeneratePayload("exe");
            
            Assert.NotNull(payload);
            Assert.NotNull(payload.Bytes);
            Assert.True(payload.Bytes.Length > 0);
        }
        
        [Fact]
        public async Task GenerateDllPayload()
        {
            var payload = await GeneratePayload("dll");
            
            Assert.NotNull(payload);
            Assert.NotNull(payload.Bytes);
            Assert.True(payload.Bytes.Length > 0);
        }
        
        [Fact]
        public async Task GenerateRawPayload()
        {
            var payload = await GeneratePayload("raw");
            
            Assert.NotNull(payload);
            Assert.NotNull(payload.Bytes);
            Assert.True(payload.Bytes.Length > 0);
        }
        
        [Fact]
        public async Task GeneratePowerShellPayload()
        {
            var payload = await GeneratePayload("powershell");
            
            Assert.NotNull(payload);
            Assert.NotNull(payload.Bytes);
            Assert.True(payload.Bytes.Length > 0);
        }
        
        [Fact]
        public async Task GenerateSvcPayload()
        {
            var payload = await GeneratePayload("svc");
            
            Assert.NotNull(payload);
            Assert.NotNull(payload.Bytes);
            Assert.True(payload.Bytes.Length > 0);
        }

        private async Task<PayloadResponse> GeneratePayload(string format)
        {
            const string handlerName = "default-http";
            
            var response = await Client.GetAsync($"{Routes.V1.Payloads}/{handlerName}/{format}");

            if (!response.IsSuccessStatusCode)
                return new PayloadResponse { Bytes = Array.Empty<byte>() };
            
            return await response.Content.ReadFromJsonAsync<PayloadResponse>();
        }
    }
}