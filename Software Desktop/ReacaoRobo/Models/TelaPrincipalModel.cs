namespace ReacaoRobo.Models
{
    /// <summary>
    /// Contem o modelo para as requisiçoes.
    /// </summary>
    internal class ReacaoModel
    {
        /// <summary>
        /// Codigo de identificaçao do cartao.
        /// </summary>
        public string CardID { get; set; }

        /// <summary>
        /// Caminho da imagem no computador.
        /// </summary>
        public string Caminho { get; set; }

        /// <summary>
        /// Contem o tipo da reaçao <see cref="CategoriaReacoesEnum"/>.
        /// </summary>
        public CategoriaReacoesEnum? Categoria { get; set; }

        /// <summary>
        /// Contem a descriçao da imagem.
        /// </summary>
        public string Descricao { get; set; }
    }

    /// <summary>
    /// Possui uma lista de valores para definir o status atual do robo com base nas requisiçoes.
    /// </summary>
    public enum StatusRoboEnum
    {
        Conectado, Desconectado, Desconhecido
    }

    /// <summary>
    /// Possui a lista de todas as reaçoes possíveis.
    /// </summary>
    public enum CategoriaReacoesEnum
    {
        Felicidade, Tristeza, Ansiedade, Raiva, Prazer
    }
}