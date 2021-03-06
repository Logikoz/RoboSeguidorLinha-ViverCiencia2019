﻿using GalaSoft.MvvmLight;
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
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using reacoes = ReacaoRobo.Models.CategoriaReacoesEnum;

namespace ReacaoRobo.ViewModels
{
    /// <summary>
    /// Reponsável pelo binding do Model e View.
    /// </summary>
    internal class TelaPrincipalViewModel : ViewModelBase
    {
        private readonly TelaPrincipalView _tela;

        private DispatcherTimer timerRequisicao;
        private List<ReacaoModel> imagens;
        private ReacaoModel reacaoAnterior;
        private ToggleButton desligarToggleButton;
        private byte qtdRequisicoesFail = 0;

        private Border __statusRobo;
        private string __servidorURI = "http://192.168.43.91"; //IP padrao
        private string __tempoRequisicao = "1000"; //tempo padrao.
        private string __textoDescricao;
        private string __tipoReacao = "Indisponível";

        //propriedades
        /// <summary>
        /// Define o valor que mostrará o Status do Robo na view (<see cref="StatusRoboEnum.Conectado"/>, <see cref="StatusRoboEnum.Desconectado"/>, <see cref="StatusRoboEnum.Desconhecido"/>).
        /// </summary>
        public Border StatusRobo
        {
            get => __statusRobo;
            set => Set(ref __statusRobo, value);
        }

        /// <summary>
        /// Define a URI do site que será feita as requisiçoes.
        /// </summary>
        public string ServidorURI
        {
            get => __servidorURI;
            set => Set(ref __servidorURI, value);
        }

        /// <summary>
        /// Define o tempo em (ms) que será feito as requisiçoes.
        /// </summary>
        public string TempoRequisicao
        {
            get => __tempoRequisicao;
            set
            {
                _ = int.TryParse(value, out int result);
                timerRequisicao.Interval = new TimeSpan(0, 0, 0, 0, result);
                Set(ref __tempoRequisicao, value);
            }
        }

        /// <summary>
        /// Comando para definir o <see cref="StatusRobo"/>.
        /// </summary>

        /// <summary>
        /// Define o texto que contém na descriçao da reaçao.
        /// </summary>
        public string TextoDescricao
        {
            get => __textoDescricao;
            set => Set(ref __textoDescricao, value);
        }

        /// <summary>
        /// Define o texto do tipo da reaçao (<see cref="reacoes.Ansiedade "/>, <see cref="reacoes.Felicidade"/>, <see cref="reacoes.Prazer"/>, <see cref="reacoes.Raiva"/>, <see cref="reacoes.Tristeza"/>).
        /// </summary>
        public string TipoReacao
        {
            get => __tipoReacao;
            set => Set(ref __tipoReacao, value);
        }

        public RelayCommand<ToggleButton> StatusRequisicao { get; private set; }

        //construtores
        public TelaPrincipalViewModel(TelaPrincipalView tela)
        {
            IniciarTimer();
            _tela = tela;
            StatusRequisicao = new RelayCommand<ToggleButton>(AlterarStatusRequisicao);
            LerLocalJson();
        }

        //metodos
        private void IniciarTimer()
        {
            //verificando se foi possivel fazer a conversao, e iniciando o timer caso verdadeiro.
            if (int.TryParse(TempoRequisicao, out int result))
            {
                timerRequisicao = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, result) };
                timerRequisicao.Tick += TimerRequisicao_Tick;
            }
        }

        private void TimerRequisicao_Tick(object sender, EventArgs e) => FazendoRequisicoesAsync();

        //definir se as requisiçoes estao ligadas ou nao.
        private void AlterarStatusRequisicao(ToggleButton tb)
        {
            desligarToggleButton = tb;
            if (tb.IsChecked.Value)
            {
                timerRequisicao.Start();
                _tela.CaixaRespostaRequisicao_rtb.Document.Blocks.Clear();
            }
            else
            {
                if (timerRequisicao.IsEnabled)
                    timerRequisicao.Stop();
                AlterandoDesconhecido();
            }
        }

        //faz as requisiçoes para verificar se há alguma reaçao para ser mostrada.
        private async void FazendoRequisicoesAsync()
        {
            //verificando se a uri está no formato correto.
            if (!VerificarValidadeURI())
                return;
            //fazendo requisiçao
            IRestResponse response = await ReacaoRoboService.VerificarReacaoAsync(new Uri(ServidorURI), TempoRequisisaoToInt());
            //corrigindo bug
            //desmarca o toggleButton caso ainda tenha uma requisiçao em andamento.
            if (!desligarToggleButton.IsChecked.Value)
            {
                AlterarStatusRequisicao(desligarToggleButton);
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
                    AlterandoDesconhecido();
                    break;
            }
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
                    var filterItens = imagens.Where(a => a.CardID == reacao.CardID).ToList();
                    //sorteando um item da lista.
                    ReacaoModel imagemSorteada = filterItens[new Random().Next(0, filterItens.Count)];
                    TextoDescricao = imagemSorteada.Descricao;
                    TipoReacao = imagemSorteada.Categoria.ToString();
                    //limpando grid de visualizaçao, e adicionando uma nova imagem.
                    _tela.GridImagens_gd.Children.Clear();
                    AdicinandoReacao(imagemSorteada);
                    //atribuindo a reacao atual para o campo para evitar que a proxima requisiçao seja a mesma.
                    reacaoAnterior = imagemSorteada;
                }
                catch
                {
                    AdicionarNovaLinha("Não foi possível achar a imagem.");
                    AdicionarNovaLinha("Programador burro!");
                }
            }
        }

        private void AdicinandoReacao(ReacaoModel imagemSorteada)
        {
            //adicinando um novo card no grid onde é mostrado a imagem da reaçao.
            _ = _tela.GridImagens_gd.Children.Add(
                new Card
                {
                    //adicionando a imagem no conteudo do card.
                    Content = new Image
                    {
                        //setando a imagem de acordo com o caminho da imagem sorteada.
                        Source = new BitmapImage(new Uri($"./Recursos/{imagemSorteada.Categoria}/{imagemSorteada.Caminho}")),
                        Stretch = Stretch.Fill
                    }
                });
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
            _tela.GridImagens_gd.Children.Clear();
            _ = _tela.GridImagens_gd.Children.Add(
                new Card
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Content = new TextBlock { FontSize = 20, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center }
                }
            );
            switch (roboEnum)
            {
                case StatusRoboEnum.Desconectado:
                    (((_tela.GridImagens_gd.Children[0] as Card).Content as TextBlock).Text, reacaoAnterior) = ("Sem Sinal!", null);
                    break;

                case StatusRoboEnum.Desconhecido:
                    (((_tela.GridImagens_gd.Children[0] as Card).Content as TextBlock).Text, reacaoAnterior) = ("Viver Ciência 2019", null);
                    break;
            }
            //removendo tipo da reacao e descriçao
            ZerandoDescricao();
        }

        //Caso dispare uma exception, quer dizer que a URI nao está em um formato valido.
        private bool VerificarValidadeURI()
        {
            try
            {
                _ = new Uri(ServidorURI);
                return true;
            }
            catch
            {
                _tela.BarraNotificacao_sb.MessageQueue.Enqueue("Formato do IP do servidor é inválido.");
                desligarToggleButton.IsChecked = false;
                AlterarStatusRequisicao(desligarToggleButton);
                return false;
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
            //adiciona uma nova linha no RichTextBox
            _tela.CaixaRespostaRequisicao_rtb.AppendText($"{DateTime.Now.ToString("HH:mm")}: {valor}\n");
            //move o scroll para o ultimo texto inserido.
            _tela.CaixaRespostaRequisicao_rtb.ScrollToEnd();
        }

        private void ZerandoDescricao() => (TextoDescricao, TipoReacao) = (string.Empty, "Indisponível");

        private void DesativarRequisicoes()
        {
            desligarToggleButton.IsChecked = false;
            AlterarStatusRequisicao(desligarToggleButton);
            ZerandoDescricao();
        }

        private void AlterandoDesconhecido()
        {
            AlterarStatusRobo(StatusRoboEnum.Desconhecido);
            LimparVisualizacao(StatusRoboEnum.Desconhecido);
            qtdRequisicoesFail = 0;
        }
    }
}