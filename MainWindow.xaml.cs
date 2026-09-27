using System.Windows;
using NewtonApp.ViewModels;

namespace NewtonApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}