namespace ElAurelion_Sol
{
    using LeagueSharp.Common;

    internal class Program
    {
        private static void Main(string[] args)
        {
            EloBuddy.SDK.Events.Loading.OnLoadingComplete += AurelionSol.OnGameLoad;
        }
    }
}