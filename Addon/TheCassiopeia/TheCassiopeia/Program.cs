
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Common;

namespace TheCassiopeia
{
    class Program
    {

        static void Main(string[] args)
        {
            EloBuddy.SDK.Events.Loading.OnLoadingComplete += new Cassiopeia().Load;
        }
    }
}
