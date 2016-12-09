using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataGrid
{
    class Locator
    {
        public static Locator Instance = new Locator();

        private MainVM _mainVM;
        public MainVM MainVM {
            get { return _mainVM ?? (_mainVM = new MainVM()); }
        }
    }
}
