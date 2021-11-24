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
        public async Task<IEnumerable<DroneResponse>> GetDrones()
        {
            var response = await _client.GetAsync(Routes.V1.Drones);
            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<IEnumerable<DroneResponse>>(content);
        }

        public async Task<DroneResponse> GetDrone(string guid)
        {
            var response = await _client.GetAsync($"{Routes.V1.Drones}/{guid}");
            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<DroneResponse>(content);
        }

        public async Task SendDroneTask(string guid, DroneTaskRequest request)
        {
            var requestJson = JsonConvert.SerializeObject(request);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            await _client.PostAsync($"{Routes.V1.Drones}/{guid}/tasks", requestContent);
        }
    }
}