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
using System.Windows.Shapes;
using System.Windows.Interop;
using ZN.iKuPlayer.WPF.Modules.ViewModel;
using ZN.iKuPlayer.WPF.Modules.Model;

namespace ZN.iKuPlayer.WPF.Modules.View
{
    /// <summary>
    /// MainUC.xaml 的交互逻辑
    /// </summary>
    public partial class MainUC : Window
    {
        public IntPtr MainHandle { get { return new WindowInteropHelper(this).Handle; } }
        public MainUC()
        {
            //Searcher s = new Searcher();
            //s.GetSearchResult("天后", 1);
            InitializeComponent(); 
        }

        private void mainUC_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Progress_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MainVM.DraggingProgressSlider = true;
        }

        private void Progress_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            MainVM.DraggingProgressSlider = false;
        }

        private void WaterMarkedTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Enter)
            //{
            //    string searchContext = WaterMarkedTextBox.Text.Trim();
            //    if (!string.IsNullOrEmpty(searchContext))
            //    {
            //        Searcher searcher = new Searcher();
            //        searcher.GetSearchResult(searchContext, 1);
            //    }
            //}
        }

        private void DataGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
