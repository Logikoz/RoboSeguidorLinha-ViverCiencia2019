using GalaSoft.MvvmLight.Command;
using MaterialDesignThemes.Wpf;
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ReacaoRobo.ViewModels
{
    class TelaPrincipalViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        //campos
        private Border _statusRobo;
        private string _servidorURI = "http://192.168.43.91"; //IP padrao
        private string _tempoRequisicao = "1000"; //tempo padrao.
        private string _textoDescricao;

        private DispatcherTimer timerRequisicao;
        private readonly TelaPrincipalView tela;
        private List<ReacaoModel> imagens;
        private ReacaoModel reacaoAnterior;

        //propriedades
        public Border StatusRobo
        {
            get => _statusRobo;
            set
            {
                _statusRobo = value;
                ChangeValue("StatusRobo");
            }
        }
        public string ServidorURI
        {
            get => _servidorURI;
            set
            {
                _servidorURI = value;
                ChangeValue("ServidorURI");
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
                ChangeValue("TempoRequisicao");
            }
        }
        public RelayCommand<ToggleButton> StatusRequisicao { get; private set; }
        public string TextoDescricao
        {
            get => _textoDescricao;
            set
            {
                _textoDescricao = value;
                ChangeValue("TextoDescricao");
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
        public void ChangeValue(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
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
            if (tb.IsChecked.Value)
            {
                timerRequisicao.Start();
                tela.CaixaRespostaRequisicao_rtb.Document.Blocks.Clear();
            }
            else
            {
                timerRequisicao.Stop();
                timerRequisicao.IsEnabled = false;
                AlterarStatusRobo(StatusRoboEnum.Desconhecido);
            }
        }
        //faz as requisiçoes para verificar se há alguma reaçao para ser mostrada.
        private async void FazendoRequisicoesAsync()
        {
            IRestResponse response = await ReacaoRoboService.VerificarReacaoAsync(ServidorURI, TempoRequisisaoToInt());

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    ReacaoModel reacao = new JsonDeserializer().Deserialize<ReacaoModel>(response);
                    AdicionarNovaLinha(response.StatusCode.ToString());
                    //AdicionarNovaLinha(reacao.CardID);

                    VisualizarReacao(reacao);

                    AlterarStatusRobo(StatusRoboEnum.Conectado);
                    break;
                case 0:
                    AdicionarNovaLinha("Requisiçao falhou.");
                    AlterarStatusRobo(StatusRoboEnum.Desconectado);
                    break;
                default:
                    AlterarStatusRobo(StatusRoboEnum.Desconhecido);
                    break;
            }
        }
        //Ler o arquivo json que está na ./ do app.
        private async void LerLocalJson()
        {
            using StreamReader ler = File.OpenText("./reacoes.json");
            imagens = SimpleJson.DeserializeObject<List<ReacaoModel>>(await ler.ReadToEndAsync());
        }
        //adiciona uma nova reaçao na tela
        private void VisualizarReacao(ReacaoModel reacao)
        {
            if (reacaoAnterior is null || reacao.CardID != reacaoAnterior.CardID)
            {
                try
                {
                    //criando lista com os ID iguais ao da requisiçao.
                    List<ReacaoModel> filterItens = imagens.Where(a => a.CardID == reacao.CardID).ToList();
                    //sortiando um item da lista.
                    ReacaoModel sortedItem = filterItens[new Random().Next(0, filterItens.Count)];
                    TextoDescricao = sortedItem.Descricao;
                    string imagem = sortedItem.Caminho;
                    tela.GridImagens_gd.Children.Clear();
                    _ = tela.GridImagens_gd.Children.Add(
                        new Card
                        {
                            Content = new Image
                            {
                                Source = new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + imagem)),
                                Stretch = Stretch.Uniform
                            }
                        }                    );
                    reacaoAnterior = sortedItem;
                }
                catch
                {
                    AdicionarNovaLinha("Nao foi possivel achar a imagem.");
                    AdicionarNovaLinha("Programador de burro!");
                }
            }
        }
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
    }
}
