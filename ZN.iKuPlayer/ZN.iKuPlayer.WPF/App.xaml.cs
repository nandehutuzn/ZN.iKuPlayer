using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Media.Animation;

namespace ZN.iKuPlayer.WPF
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        private static string[] _args;
        /// <summary>
        /// 启动参数
        /// </summary>
        public static string[] Args { get { return _args; } }

        /// <summary>
        /// 启动目录
        /// </summary>
        public static string WorkPath { get { return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName); } }

        /// <summary>
        /// 当前程序版本号
        /// </summary>
        public static string Version { get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); } }

        protected override void OnStartup(StartupEventArgs e)
        {
            _args = e.Args;
            //设置WPF动画默认帧数
            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = 25 });
            base.OnStartup(e);
        }
    }
}
