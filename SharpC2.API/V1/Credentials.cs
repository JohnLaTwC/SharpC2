using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using SharpC2.API.V1.Requests;
using SharpC2.API.V1.Responses;

namespace SharpC2.API.V1
{
    public partial class SharpC2Client
    {
        public async Task<IEnumerable<CredentialRecordResponse>> GetCredentials()
        {
            var response = await _client.GetAsync(Routes.V1.Credentials);
            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<IEnumerable<CredentialRecordResponse>>(content);
        }

        public async Task<CredentialRecordResponse> GetCredential(string guid)
        {
            var response = await _client.GetAsync($"{Routes.V1.Credentials}/{guid}");
            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<CredentialRecordResponse>(content);
        }

        public async Task<bool> AddCredential(AddCredentialRecordRequest request)
        {
            var requestJson = JsonConvert.SerializeObject(request);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(Routes.V1.Credentials, requestContent);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteCredential(string guid)
        {
            var response = await _client.DeleteAsync($"{Routes.V1.Credentials}/{guid}");
            return response.IsSuccessStatusCode;
        }
    }
}