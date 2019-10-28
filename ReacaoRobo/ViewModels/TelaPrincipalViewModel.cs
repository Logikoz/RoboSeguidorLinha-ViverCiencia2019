using GalaSoft.MvvmLight.Command;
using ReacaoRobo.Models;
using ReacaoRobo.Services;
using ReacaoRobo.Views;
using RestSharp;
using RestSharp.Serialization.Json;
using System;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReacaoRobo.ViewModels
{
    class TelaPrincipalViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        //campos
        private Border _statusRobo;
        private string _servidorURI = "http://192.168.0.0";
        private string _tempoRequisicao = "300";
        private DispatcherTimer timerRequisicao;
        private readonly TelaPrincipalView tela;
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

        //construtores
        public TelaPrincipalViewModel(TelaPrincipalView tela)
        {
            AlterarStatusRobo(StatusRoboEnum.Desconhecido);
            IniciarTimer();
            this.tela = tela;
            StatusRequisicao = new RelayCommand<ToggleButton>(HandleChecked);
        }

        //metodos
        public void ChangeValue(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        private void IniciarTimer()
        {
            timerRequisicao = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 300) };
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
        private async void FazendoRequisicoesAsync()
        {
            IRestResponse response = await ReacaoRoboService.VerificarReacaoAsync(ServidorURI, TempoRequisisaoToInt());

            if (response.IsSuccessful)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        ReacaoModel reacao = new JsonDeserializer().Deserialize<ReacaoModel>(response);
                        AdicionarNovaLinha(response.StatusCode.ToString());
                        AdicionarNovaLinha(reacao.CardID);
                        AlterarStatusRobo(StatusRoboEnum.Conectado);
                        break;
                    default:
                        AdicionarNovaLinha("Requisiçao falhou.");
                        AlterarStatusRobo(StatusRoboEnum.Desconectado);
                        break;
                }
            }
        }

        private int TempoRequisisaoToInt()
        {
            _ = int.TryParse(_tempoRequisicao, out int result);
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
