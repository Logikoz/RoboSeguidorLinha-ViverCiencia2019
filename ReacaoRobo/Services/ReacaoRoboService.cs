using RestSharp;
using System.Threading.Tasks;

namespace ReacaoRobo.Services
{
    internal class ReacaoRoboService
    {
        public static async Task<IRestResponse> VerificarReacaoAsync(string uri)
        {
            return await new RestClient(uri).ExecuteGetTaskAsync(new RestRequest("/", Method.GET, DataFormat.Json));
        }
    }
}
