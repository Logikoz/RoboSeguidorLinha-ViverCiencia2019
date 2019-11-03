using GalaSoft.MvvmLight.Command;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using ReacaoRobo.Models;
using ReacaoRobo.Services;
using ReacaoRobo.Views;
using RestSharp;
using RestSharp.Serialization.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ReacaoRobo.ViewModels
{
    internal class TelaPrincipalViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        //campos
        private Border _statusRobo;
        private string _servidorURI = "http://192.168.43.91"; //IP padrao
        private string _tempoRequisicao = "1000"; //tempo padrao.
        private string _textoDescricao;
        private string _tipoReacao = "Indisponível";

        private DispatcherTimer timerRequisicao;
        private readonly TelaPrincipalView tela;
        private List<ReacaoModel> imagens;
        private ReacaoModel reacaoAnterior;
        private ToggleButton desligarToggleButton;
        private byte qtdRequisicoesFail = 0;

        //propriedades
        public Border StatusRobo
        {
            get => _statusRobo;
            set
            {
                _statusRobo = value;
                AlterarValor("StatusRobo");
            }
        }
        public string ServidorURI
        {
            get => _servidorURI;
            set
            {
                _servidorURI = value;
                AlterarValor("ServidorURI");
            }
        }
        public string TempoRequisicao
        {
            get => _tempoRequisicao;
            set
            {
                _tempoRequisicao = value;
                _ = int.TryParse(value, out int result);
                timerRequisicao.Interval = new TimeSpan(0, 0, 0, 0, result);
                AlterarValor("TempoRequisicao");
            }
        }
        public RelayCommand<ToggleButton> StatusRequisicao { get; private set; }
        public string TextoDescricao
        {
            get => _textoDescricao;
            set
            {
                _textoDescricao = value;
                AlterarValor("TextoDescricao");
            }
        }
        public string TipoReacao
        {
            get => _tipoReacao;
            set
            {
                _tipoReacao = value;
                AlterarValor("TipoReacao");
            }
        }

        //construtores
        public TelaPrincipalViewModel(TelaPrincipalView tela)
        {
            AlterarStatusRobo(StatusRoboEnum.Desconhecido);
            IniciarTimer();
            this.tela = tela;
            StatusRequisicao = new RelayCommand<ToggleButton>(HandleChecked);
            LerLocalJson();
        }

        //metodos
        public void AlterarValor(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        private void IniciarTimer()
        {
            _ = int.TryParse(TempoRequisicao, out int result);
            timerRequisicao = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, result) };
            timerRequisicao.Tick += TimerRequisicao_Tick;
        }
        private void TimerRequisicao_Tick(object sender, EventArgs e) => FazendoRequisicoesAsync();
        //requisiçoes estao ligadas ou nao
        private void HandleChecked(ToggleButton tb)
        {
            desligarToggleButton = tb;
            if (tb.IsChecked.Value)
            {
                timerRequisicao.Start();
                tela.CaixaRespostaRequisicao_rtb.Document.Blocks.Clear();
            }
            else
            {
                if (timerRequisicao.IsEnabled)
                    timerRequisicao.Stop();
                AlterarStatusRobo(StatusRoboEnum.Desconhecido);
                LimparVisualizacao(StatusRoboEnum.Desconhecido);
            }
        }
        //faz as requisiçoes para verificar se há alguma reaçao para ser mostrada.
        private async void FazendoRequisicoesAsync()
        {
            //verificando se a uri está no formato correto.
            if (!VerificarValidadeURI())
                return;
            //fazendo requisiçao
            IRestResponse response = await ReacaoRoboService.VerificarReacaoAsync(ServidorURI, TempoRequisisaoToInt());
            //corrigindo bug
            //desmarca o toggleButton caso ainda tenha uma requisiçao em andamento.
            if (!desligarToggleButton.IsChecked.Value)
            {
                HandleChecked(desligarToggleButton);
                return;
            }
            //verificando status da requisiçao e adicionando reaçoes caso OK.
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    ReacaoModel reacao = new JsonDeserializer().Deserialize<ReacaoModel>(response);
                    AdicionarNovaLinha(response.StatusCode.ToString());
                    //caso o cartao seja o de saida, ele encerra as requisiçoes.
                    if (reacao.CardID == "ID_Cartao_Parada")
                    {
                        DesativarRequisicoes();
                        return;
                    }
                    VisualizarReacao(reacao);
                    AlterarStatusRobo(StatusRoboEnum.Conectado);
                    qtdRequisicoesFail = 0;
                    break;
                case 0:
                    AdicionarNovaLinha("Requisiçao falhou.");
                    if (qtdRequisicoesFail == 5)
                    {
                        AlterarStatusRobo(StatusRoboEnum.Desconectado);
                        LimparVisualizacao(StatusRoboEnum.Desconectado);
                        qtdRequisicoesFail = 0;
                    }
                    qtdRequisicoesFail++;
                    break;
                default:
                    AlterarStatusRobo(StatusRoboEnum.Desconhecido);
                    LimparVisualizacao(StatusRoboEnum.Desconhecido);
                    break;
            }
        }
        private void DesativarRequisicoes()
        {
            desligarToggleButton.IsChecked = false;
            HandleChecked(desligarToggleButton);
            TipoReacao = "Indisponível";
            TextoDescricao = string.Empty;
        }
        //Ler o arquivo json que está na ./ do app.
        private async void LerLocalJson()
        {
            using StreamReader ler = File.OpenText("./Recursos/reacoes.json");
            imagens = JsonConvert.DeserializeObject<List<ReacaoModel>>(await ler.ReadToEndAsync());
            
        }
        //adiciona uma nova reaçao na tela
        private void VisualizarReacao(ReacaoModel reacao)
        {
            //verificando se a reaçao anterior é nula ou se o ID da reacao recebida é diferente do ID da reacao anterior.
            //para evitar com que adicione a mesma reaçao seguida.
            if (reacaoAnterior is null || reacao.CardID != reacaoAnterior.CardID)
            {
                try
                {
                    //criando lista com os ID iguais ao da requisiçao.
                    List<ReacaoModel> filterItens = imagens.Where(a => a.CardID == reacao.CardID).ToList();
                    //sortiando um item da lista.
                    ReacaoModel sortedItem = filterItens[new Random().Next(0, filterItens.Count)];
                    TextoDescricao = sortedItem.Descricao;
                    TipoReacao = sortedItem.Categoria.ToString();
                    //limpando grid de visualizaçao, e adicionando uma nova imagem.
                    tela.GridImagens_gd.Children.Clear();
                    _ = tela.GridImagens_gd.Children.Add(
                        new Card
                        {
                            Content = new Image
                            {
                                //nao testado
                                Source = new BitmapImage(new Uri($"./Recursos/{sortedItem.Categoria}/{sortedItem.Caminho}")),
                                Stretch = Stretch.Fill
                            }
                        });
                    reacaoAnterior = sortedItem;
                }
                catch
                {
                    AdicionarNovaLinha("Não foi possível achar a imagem.");
                    AdicionarNovaLinha("Programador de burro!");
                    LimparVisualizacao(StatusRoboEnum.Desconectado);
                }
            }
        }
        //converte o tempo da requisiçao para inteiro.
        private int TempoRequisisaoToInt()
        {
            _ = int.TryParse(TempoRequisicao, out int result);
            return result;
        }
        private void AdicionarNovaLinha(string valor)
        {
            tela.CaixaRespostaRequisicao_rtb.AppendText($"{DateTime.Now.ToString("HH:mm")}: {valor}\n");
            tela.CaixaRespostaRequisicao_rtb.ScrollToEnd();
        }
        private void AlterarStatusRobo(StatusRoboEnum status)
        {
            Border border = new Border() { CornerRadius = new CornerRadius(10), Child = new TextBlock { Foreground = Brushes.White, Padding = new Thickness(4, 3, 4, 3) } };
            switch (status)
            {
                case StatusRoboEnum.Conectado:
                    ((border.Child as TextBlock).Text, border.Background) = ("Conectado", Brushes.Green);
                    break;
                case StatusRoboEnum.Desconectado:
                    ((border.Child as TextBlock).Text, border.Background) = ("Desconectado", Brushes.Red);
                    break;
                case StatusRoboEnum.Desconhecido:
                    ((border.Child as TextBlock).Text, border.Background) = ("Desconhecido", Brushes.Gray);
                    break;
            }

            StatusRobo = border;
        }
        //Alterando visualizaçao ao ocorrer um erro na requisiçao
        //ou, o usuario desativar as requisiçoes.
        private void LimparVisualizacao(StatusRoboEnum roboEnum)
        {
            tela.GridImagens_gd.Children.Clear();
            _ = tela.GridImagens_gd.Children.Add(
                new Card
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Content = new TextBlock { FontSize = 20, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center }
                }
            );
            switch (roboEnum)
            {
                case StatusRoboEnum.Desconectado:
                    ((tela.GridImagens_gd.Children[0] as Card).Content as TextBlock).Text = "Sem Sinal!";
                    reacaoAnterior = null;
                    break;
                case StatusRoboEnum.Desconhecido:
                    ((tela.GridImagens_gd.Children[0] as Card).Content as TextBlock).Text = "Viver Ciência 2019";
                    reacaoAnterior = null;
                    break;
            }
            //removendo tipo da reacao e descriçao
            TextoDescricao = string.Empty;
            TipoReacao = "Indisponível";
        }
        private bool VerificarValidadeURI()
        {
            try
            {
                _ = new Uri(ServidorURI);
                return true;
            }
            catch
            {
                tela.BarraNotificacao_sb.MessageQueue.Enqueue("Formato do IP do servidor é inválido.");
                desligarToggleButton.IsChecked = false;
                HandleChecked(desligarToggleButton);
                return false;
            }
        }
    }
}
