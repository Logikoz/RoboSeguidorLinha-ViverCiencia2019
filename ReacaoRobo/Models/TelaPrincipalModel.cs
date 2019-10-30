namespace ReacaoRobo.Models
{
    internal class ReacaoModel
    {
        public string CardID { get; set; }
        public string Caminho { get; set; }
        public CategoriaReacoes? Caterogia { get; set; }
        public string Descricao { get; set; }
    }
    public enum StatusRoboEnum { Conectado, Desconectado, Desconhecido }
    public enum CategoriaReacoes { Raiva, Ansiedade, Tristeza, Felicidade, Prazer }
}
