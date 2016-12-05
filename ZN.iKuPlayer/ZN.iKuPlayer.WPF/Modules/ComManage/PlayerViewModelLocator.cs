using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZN.iKuPlayer.WPF.Modules.ViewModel;

namespace ZN.iKuPlayer.WPF.Modules.ComManage
{
    class PlayerViewModelLocator
    {
        private static readonly object _syncObject = new object();

        private static PlayerViewModelLocator _instance;
        public static PlayerViewModelLocator Instance {
            get {
                if (_instance == null)
                {
                    lock (_syncObject)
                    {
                        if (_instance == null)
                            _instance = new PlayerViewModelLocator();
                    }
                }
                return _instance;
            }
        }

        private MainVM _mainViewModel;
        /// <summary>
        /// 主页面
        /// </summary>
        public MainVM MainViewModel {
            get { return _mainViewModel ?? (_mainViewModel = new MainVM()); }
        }

        private DesktopLrcVM _desktopLrcViewModel;
        /// <summary>
        /// 桌面歌词
        /// </summary>
        public DesktopLrcVM DesktopLrcViewModel {
            get { return _desktopLrcViewModel ?? (_desktopLrcViewModel = new DesktopLrcVM()); }
        }
    }
}
