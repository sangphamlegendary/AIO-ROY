using System;
using System.Linq;
using EloBuddy;
using LeagueSharp.Common;

namespace KurisuMorgana
{
    internal class Program
    {
        static void Main(string[] args)
        {
            EloBuddy.SDK.Events.Loading.OnLoadingComplete += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            new KurisuMorgana();
        }
    }
}
