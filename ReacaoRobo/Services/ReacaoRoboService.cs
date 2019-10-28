using RestSharp;
using System.Threading.Tasks;

namespace ReacaoRobo.Services
{
    internal class ReacaoRoboService
    {
        public static async Task<IRestResponse> VerificarReacaoAsync(string uri, int timeOut = 300)
        {
            return await new RestClient(uri) { Timeout = timeOut }.ExecuteGetTaskAsync(new RestRequest(string.Empty, Method.GET, DataFormat.Json));
        }
    }
}
