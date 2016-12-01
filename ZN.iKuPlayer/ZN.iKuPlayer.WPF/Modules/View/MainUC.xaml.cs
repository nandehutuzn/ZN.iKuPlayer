﻿using System;
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
            InitializeComponent();
            
        }
    }
}
