namespace Elvarus
{
    using LeagueSharp.Common;

    internal class Program
    {
        #region Methods

        private static void Main(string[] args)
        {
            EloBuddy.SDK.Events.Loading.OnLoadingComplete += Varus.Game_OnGameLoad;
        }

        #endregion
    }
}