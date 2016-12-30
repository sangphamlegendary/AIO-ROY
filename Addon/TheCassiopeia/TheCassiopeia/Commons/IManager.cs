using LeagueSharp.Common;
using TheCassiopeia.Commons.ComboSystem;

namespace TheCassiopeia.Commons
{
    interface IManager
    {
        void Attach(Menu mainMenu, ComboProvider provider);
    }
}
