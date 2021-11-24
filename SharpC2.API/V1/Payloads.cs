using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json;

using SharpC2.API.V1.Responses;

namespace SharpC2.API.V1
{
    public partial class SharpC2Client
    {
        public async Task<IEnumerable<string>> GetPayloadFormats()
        {
            var response = await _client.GetAsync($"{Routes.V1.Payloads}/formats");
            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<IEnumerable<string>>(content);
        }

        public async Task<PayloadResponse> GetPayload(string format, string handler)
        {
            var response = await _client.GetAsync($"{Routes.V1.Payloads}/{handler}/{format}");
            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<PayloadResponse>(content);
        }
    }
}