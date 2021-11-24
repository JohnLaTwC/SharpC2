using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace SharpC2.API.V1
{
    public partial class SharpC2Client
    {
        private readonly string _server;
        private readonly string _port;
        private readonly string _nick;
        private readonly string _password;
       
        private HttpClient _client;
        
        public SharpC2Client(string server, string port, string nick, string password, HttpClientHandler handler = null)
        {
            _server = server;
            _port = port;
            _nick = nick;
            _password = password;
            
            CreateClient(handler);
        }

        private void CreateClient(HttpClientHandler handler)
        {
            handler ??= new HttpClientHandler();

            _client = new HttpClient(handler);
            _client.BaseAddress = new Uri($"https://{_server}:{_port}");
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_nick}:{_password}"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
        }
    }
}