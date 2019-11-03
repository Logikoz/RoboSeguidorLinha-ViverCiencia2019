using ReacaoRobo.ViewModels;
using RestSharp;
using System;
using System.Threading.Tasks;

namespace ReacaoRobo.Services
{
    /// <summary>
    /// Responsavel por fazer as requisiçoes das reaçoes.
    /// </summary>
    internal class ReacaoRoboService
    {
        /// <summary>
        /// Faz a requisiçao e retorna a <see cref="IRestResponse"/> com o resultado da requisiçao
        /// </summary>
        /// <param name="uri">Uri do site que será feita a requisiçao, normalmente é uma propriedade em <see cref="TelaPrincipalViewModel"/></param>
        /// <param name="timeOut">Tempo limite da requisiçao, por padrao é 1000ms ou uma proopriedade em <see cref="TelaPrincipalViewModel"/></param>
        public static async Task<IRestResponse> VerificarReacaoAsync(Uri uri, int timeOut = 1000) => await new RestClient(uri)
        {
            //setando o tempo limite da requisiçao.
            Timeout = timeOut
        }.ExecuteGetTaskAsync(new RestRequest(string.Empty, Method.GET, DataFormat.Json));
    }
}
