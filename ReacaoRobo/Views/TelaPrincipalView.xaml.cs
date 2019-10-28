using ReacaoRobo.ViewModels;
using System.Windows;

namespace ReacaoRobo.Views
{
    /// <summary>
    /// Lógica interna para TelaPrincipalView.xaml
    /// </summary>
    public partial class TelaPrincipalView : Window
    {
        public TelaPrincipalView()
        {
            InitializeComponent();
            DataContext = new TelaPrincipalViewModel(this);
        }
    }
}
