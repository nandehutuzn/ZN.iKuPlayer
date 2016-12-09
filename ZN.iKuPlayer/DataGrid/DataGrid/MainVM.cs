using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;

namespace DataGrid
{
    class MainVM : ViewModelBase
    {
        public MainVM()
        {
            SongInfo ss1 = new SongInfo();
            ss1.SongName = "歌曲1";
            ss1.Singer = "歌手1";
            SongInfo ss2 = new SongInfo();
            ss2.SongName = "歌曲2";
            ss2.Singer = "歌手2";

            List.Add(new SongVM(ss1));
            List.Add(new SongVM(ss2));
        }

        private ObservableCollection<SongVM> _list = new ObservableCollection<SongVM>();
        public ObservableCollection<SongVM> List {
            get { return _list; }
            set {
                _list = value;
                RaisePropertyChanged("List");
            }
        }

        private RelayCommand _selectedCommand;
        public RelayCommand SelectedCommand {
            get{
                return _selectedCommand ?? (_selectedCommand = new RelayCommand(() =>
                    {

                    }));
            }
        }
    }
}
