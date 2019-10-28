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
        //construtor
        public TelaPrincipalViewModel(TelaPrincipalView tela)
        {
            IniciarTimer();
            this.tela = tela;
        }

        //metodos
        public void ChangeValue(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        private void IniciarTimer()
        {
            timerRequisicao = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 300) };
            timerRequisicao.Tick += TimerRequisicao_Tick;
            timerRequisicao.Start();
        }

        private void TimerRequisicao_Tick(object sender, EventArgs e)
        {
            NewMethod();
        }

        private async void NewMethod()
        {
            //fazer requisiçoes
            IRestResponse response = await ReacaoRoboService.VerificarReacaoAsync(ServidorURI);

            AdicionarNovaLinha(response.StatusCode.ToString());

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    ReacaoModel reacao = new JsonDeserializer().Deserialize<ReacaoModel>(response);
                    AlterarStatusRobo(true);
                    break;
                case HttpStatusCode.NotFound:
                    AlterarStatusRobo(false);
                    break;
                default:
                    AlterarStatusRobo(false);
                    break;
            }
        }
        private void AdicionarNovaLinha(string valor)
        {
            tela.CaixaRespostaRequisicao_rtb.AppendText(DateTime.Now.ToString("HH:mm"));
            tela.CaixaRespostaRequisicao_rtb.AppendText(valor);
        }

        private void AlterarStatusRobo(bool status)
        {
            Border border = new Border() { CornerRadius = new CornerRadius(10), Child = new TextBlock { Foreground = Brushes.White, Padding = new Thickness(4, 3, 4, 3) } };
            if (status)
                ((border.Child as TextBlock).Text, border.Background) = ("Conectado", Brushes.Green);
            else
                ((border.Child as TextBlock).Text, border.Background) = ("Desconectado", Brushes.Red);
            StatusRobo = border;
        }
    }
}
