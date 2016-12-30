using System;
using EloBuddy;
using LeagueSharp.Common;

namespace AhriSharp
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
            new Ahri();
        }
    }
}
