using System.Drawing;
using EloBuddy;
using LeagueSharp.Common;

namespace TheCassiopeia.Commons.Debug
{
    public static class DevAssistant
    {
        public static void Init()
        {
            var mainMenu = new Menu("The DevAssistant", "TheDevAssistant", true);

            var drawSpells = true;
            var drawBuffs = true;

            mainMenu.AddMItem("Draw Spells", false, (sender, args) => drawSpells = args.GetNewValue<bool>());
            mainMenu.AddMItem("Draw Buffs", false, (sender, args) => drawBuffs = args.GetNewValue<bool>());
            mainMenu.ProcStoredValueChanged<bool>();
            mainMenu.AddToMainMenu();
            
            Drawing.OnDraw += (args) =>
            {
                int i = 50;
                if (drawBuffs)
                {
                    if (TargetSelector.GetSelectedTarget().IsValidTarget())
                    {
                      
                        foreach (var buff in TargetSelector.GetSelectedTarget().Buffs)
                        {
                            Drawing.DrawText(200, i += 20, Color.Red, buff.Name);
                        }
                    }
                    else
                    {
                        foreach (var buff in ObjectManager.Player.Buffs)
                        {
                            Drawing.DrawText(200, i += 20, Color.Red, buff.Name);
                        }
                    }
                }


                i = 50;
                if (drawSpells)
                    foreach (var buff in ObjectManager.Player.Spellbook.Spells)
                    {
                        Drawing.DrawText(600, i += 20, Color.Red, buff.Name+" / "+buff.Ammo+" / "+buff.Level+" / ");
                    }
            };
        }
        //Tear_of_the_Goddess
        //Archangels_Staff

    }
}
