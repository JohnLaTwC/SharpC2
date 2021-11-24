using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

using SharpC2.API.V1;
using SharpC2.API.V1.Requests;
using SharpC2.API.V1.Responses;

using Xunit;

namespace SharpC2.API.IntegrationTests
{
    public class CredentialTests : IntegrationTest
    {
        [Fact]
        public async Task AddCredential()
        {
            var oCred = new AddCredentialRecordRequest
            {
                Username = "RastaMouse",
                Domain = "TEST",
                Password = "Passw0rd!",
                Source = "API"
            };

            var response = await Client.PostAsJsonAsync(Routes.V1.Credentials, oCred);
            var credential = await response.Content.ReadFromJsonAsync<CredentialRecordResponse>();
            
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(credential);
            Assert.Equal(oCred.Username, credential.Username);
            Assert.Equal(oCred.Domain, credential.Domain);
            Assert.Equal(oCred.Password, credential.Password);
            Assert.Equal(oCred.Source, credential.Source);
        }

        [Fact]
        public async Task RemoveCredential()
        {
            var oCred = new AddCredentialRecordRequest
            {
                Username = "RastaMouse",
                Domain = "TEST",
                Password = "Passw0rd!",
                Source = "API"
            };

            var response = await Client.PostAsJsonAsync(Routes.V1.Credentials, oCred);
            var credential = await response.Content.ReadFromJsonAsync<CredentialRecordResponse>();

            await Client.DeleteAsync($"{Routes.V1.Credentials}/{credential.Guid}");
            
            response = await Client.GetAsync($"{Routes.V1.Credentials}/{credential.Guid}");
            
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}