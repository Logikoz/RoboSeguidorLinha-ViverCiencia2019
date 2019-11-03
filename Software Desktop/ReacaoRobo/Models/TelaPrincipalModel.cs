namespace ReacaoRobo.Models
{
    internal class ReacaoModel
    {
        public string CardID { get; set; }
        public string Caminho { get; set; }
        public CategoriaReacoesEnum? Categoria { get; set; }
        public string Descricao { get; set; }
    }
    public enum StatusRoboEnum { Conectado, Desconectado, Desconhecido }
    public enum CategoriaReacoesEnum { Felicidade, Tristeza, Ansiedade, Raiva, Prazer }
}
