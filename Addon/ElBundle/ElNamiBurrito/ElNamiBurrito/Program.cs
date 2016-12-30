namespace ElNamiBurrito
{
    using LeagueSharp.Common;

    internal class Program
    {
        #region Methods

        private static void Main(string[] args)
        {
            EloBuddy.SDK.Events.Loading.OnLoadingComplete += Nami.Game_OnGameLoad;
        }

        #endregion
    }
}