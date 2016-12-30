namespace ElDiana
{
    using System;
    using System.Drawing;
    using System.Linq;

    using EloBuddy;
    using LeagueSharp.Common;
    using SharpDX;
    using SharpDX.Direct3D9;

    internal class DrawDamage //by xSalice
    {
        #region Constants

        private const int Height = 9;

        private const int Width = 103;

        private const int XOffset = 0;

        private const int YOffset = 5;

        #endregion

        #region Static Fields

        public static System.Drawing.Color Color = System.Drawing.Color.Lime;

        public static bool Enabled = true;

        public static bool Fill = true;

        public static System.Drawing.Color FillColor = System.Drawing.Color.Goldenrod;

        //private static readonly Render.Text Text = new Render.Text(0, 0, "", 14, SharpDX.Color.Red, "monospace");
        private static EloBuddy.SDK.Rendering.Text Text = new EloBuddy.SDK.Rendering.Text(string.Empty, new System.Drawing.Font(System.Drawing.FontFamily.GenericSansSerif, 8, System.Drawing.FontStyle.Regular));

        private static DamageToUnitDelegate _damageToUnit;

        #endregion

        #region Delegates

        public delegate float DamageToUnitDelegate(AIHeroClient hero);

        #endregion

        #region Public Properties

        public static DamageToUnitDelegate DamageToUnit
        {
            get
            {
                return _damageToUnit;
            }

            set
            {
                if (_damageToUnit == null)
                {
                    Drawing.OnEndScene += Drawing_OnDraw;
                }
                _damageToUnit = value;
            }
        }
        #endregion

        #region Methods

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Enabled || _damageToUnit == null)
            {
                return;
            }

            foreach (var unit in HeroManager.Enemies.Where(h => h.IsValid && h.IsHPBarRendered))
            {
                var barPos = unit.HPBarPosition;
                var damage = _damageToUnit(unit);
                var percentHealthAfterDamage = Math.Max(0, unit.Health - damage) / unit.MaxHealth;
                var yPos = barPos.Y + YOffset;
                var xPosDamage = barPos.X + XOffset + Width * percentHealthAfterDamage;
                var xPosCurrentHp = barPos.X + XOffset + Width * unit.Health / unit.MaxHealth;

                if (damage > unit.Health)
                {
                    Text.X = (int)barPos.X + XOffset;
                    Text.Y = (int)barPos.Y + YOffset - 13;
                    Text.TextValue = ("Killable: " + (unit.Health - damage)).ToString();
                    Text.Position = new Vector2(Text.X, Text.Y);
                    Text.Draw();
                }

                Drawing.DrawLine(xPosDamage + 3, yPos, xPosDamage + 3, yPos + Height, 1, Color);

                if (Fill)
                {
                    var differenceInHP = xPosCurrentHp - xPosDamage;
                    var pos1 = barPos.X + 9 + (107 * percentHealthAfterDamage);

                    for (var i = 0; i < differenceInHP; i++)
                    {
                        Drawing.DrawLine(pos1 + i - 8 , yPos, pos1 + i - 8, yPos + Height, 1, FillColor);
                    }
                }
            }
        }

        #endregion
    }
}