using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReacaoRobo.ViewModels
{
    class TelaPrincipalViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        //campos
        private Border _statusRobo;
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
        //construtor
        public TelaPrincipalViewModel() => AlterarStatusRobo(true);
        //metodos
        public void ChangeValue(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        private void AlterarStatusRobo(bool status)
        {
            Border border = new Border() { CornerRadius = new CornerRadius(10), Child = new TextBlock { Foreground = Brushes.White, Padding = new Thickness(4, 3, 4, 3) } };
            if (status)
                ((border.Child as TextBlock).Text, border.Background) = ("Ligado", Brushes.Green);
            else
                ((border.Child as TextBlock).Text, border.Background) = ("Desligado", Brushes.Red);
            StatusRobo = border;
        }
    }
}
