using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace AmumuSharp
{
    class Program
    {
        public static Helper Helper;

        private static void Main(string[] args)
        {
            EloBuddy.SDK.Events.Loading.OnLoadingComplete += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Helper = new Helper();
            new Amumu();
        }
    }
}
