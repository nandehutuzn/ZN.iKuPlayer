using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;

namespace DataGrid
{
    class SongVM:ViewModelBase
    {
        public SongVM(SongInfo info)
        {
            Info = info;
        }

        private SongInfo _info = new SongInfo();
        public SongInfo Info {
            get { return _info; }
            set { _info = value; }
        }

        private bool _isSelected = true;
        public bool IsSelected {
            get { return _isSelected; }
            set {
                _isSelected = value;
                RaisePropertyChanged("IsSelected");
            }
        }
    }
}
