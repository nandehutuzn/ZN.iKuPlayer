using System;
using System.Collections.Generic;
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
using System.Windows.Interop;
using ZN.iKuPlayer.BASS;

namespace ZN.iKuPlayer.WPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 窗口句柄
        /// </summary>
        public IntPtr Handle { get { return new WindowInteropHelper(this).Handle; } }
        public MainWindow()
        {
            InitializeComponent();
            Player play = Player.GetInstance(Handle);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Player play = Player.GetInstance(Handle);
        }
    }
}
