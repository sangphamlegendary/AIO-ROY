namespace ElDiana
{
    using System;
    using System.Drawing;

    using EloBuddy;
    using LeagueSharp.Common;

    internal class Drawings
    {
        #region Public Methods and Operators

        public static void Drawing_OnDraw(EventArgs args)
        {
            var drawOff = ElDianaMenu.Menu.Item("ElDiana.Draw.off").GetValue<bool>();
            var drawQ = ElDianaMenu.Menu.Item("ElDiana.Draw.Q").GetValue<Circle>();
            var drawW = ElDianaMenu.Menu.Item("ElDiana.Draw.W").GetValue<Circle>();
            var drawE = ElDianaMenu.Menu.Item("ElDiana.Draw.E").GetValue<Circle>();
            var drawR = ElDianaMenu.Menu.Item("ElDiana.Draw.R").GetValue<Circle>();
            var drawRMisaya = ElDianaMenu.Menu.Item("ElDiana.Draw.RMisaya").GetValue<Circle>();
            var misayaRange = ElDianaMenu.Menu.Item("ElDiana.Combo.R.MisayaMinRange").GetValue<Slider>().Value;

            if (drawOff)
            {
                return;
            }

            if (drawQ.Active)
            {
                if (Diana.spells[Spells.Q].Level > 0)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Diana.spells[Spells.Q].Range, Color.White);
                }
            }

            if (drawE.Active)
            {
                if (Diana.spells[Spells.E].Level > 0)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Diana.spells[Spells.E].Range, Color.White);
                }
            }

            if (drawW.Active)
            {
                if (Diana.spells[Spells.W].Level > 0)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Diana.spells[Spells.W].Range, Color.White);
                }
            }

            if (drawR.Active)
            {
                if (Diana.spells[Spells.R].Level > 0)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Diana.spells[Spells.R].Range, Color.White);
                }
            }

            if (drawRMisaya.Active)
            {
                if (Diana.spells[Spells.R].Level > 0)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, misayaRange, Color.White);
                }
            }
        }

        #endregion
    }
}