using BomberMan.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BomberMan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
            //DataContext = new BombermanViewModel(15,15);
            DataContext = new MainViewModel();
            //Focusable = true;
            //Focus();
        }

        #endregion
        /*
        #region Private methods

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            var vm = DataContext as BombermanViewModel;
            if (vm == null) return;

            switch (e.Key)
            {
                case Key.W:
                    vm.MovePlayer(0, 0);
                    break;
                case Key.S:
                    vm.MovePlayer(0, 1);
                    break;
                case Key.A:
                    vm.MovePlayer(0, 2);
                    break;
                case Key.D:
                    vm.MovePlayer(0, 3);
                    break;
                case Key.E:
                    vm.PlaceBomb(0);
                    break;
                case Key.Up:
                    vm.MovePlayer(1, 0);
                    break;
                case Key.Down:
                    vm.MovePlayer(1, 1);
                    break;
                case Key.Left:
                    vm.MovePlayer(1, 2);
                    break;
                case Key.Right:
                    vm.MovePlayer(1, 3);
                    break;
                case Key.M:
                    vm.PlaceBomb(1);
                    break;
            }
        }
        private void ExitButtonClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion
        */
    }
}