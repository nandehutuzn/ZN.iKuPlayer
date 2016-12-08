using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ZN.iKuPlayer.WPF.Modules.Model;

namespace ZN.iKuPlayer.WPF.Modules.ViewModel
{
    class SearchSongVM :ViewModelBase
    {
        public SearchSongVM(SearchSong song)
        {
            _song = song;
        }
        public SearchSongVM() { }

        private SearchSong _song = new SearchSong();
        public SearchSong Song {
            get { return _song; }
            set {_song = value;}
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
