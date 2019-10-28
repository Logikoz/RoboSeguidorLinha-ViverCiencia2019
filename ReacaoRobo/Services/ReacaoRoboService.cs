using RestSharp;
using System.Threading.Tasks;

namespace ReacaoRobo.Services
{
    internal class ReacaoRoboService
    {
        public static async Task<IRestResponse> VerificarReacaoAsync(string uri)
        {
            return await new RestClient(uri).ExecuteGetTaskAsync(new RestRequest(string.Empty, Method.GET, DataFormat.Json));
        }
    }
}
