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
        public async Task<IEnumerable<HostedFileResponse>> GetHostedFiles()
        {
            var response = await _client.GetAsync(Routes.V1.HostedFiles);
            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<IEnumerable<HostedFileResponse>>(content);
        }

        public async Task AddHostedFile(AddHostedFileRequest request)
        {
            var requestJson = JsonConvert.SerializeObject(request);
            var stringContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
            
            await _client.PostAsync(Routes.V1.HostedFiles, stringContent);
        }

        public async Task DeleteHostedFile(string filename)
        {
            await _client.DeleteAsync($"{Routes.V1.HostedFiles}/{filename}");
        }
    }
}