using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using EloBuddy;
using LeagueSharp.Common;
//using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class DrMundo : Helper
    {
        private static Orbwalking.Orbwalker Orbwalker;
        public DrMundo()
        {
            Q = new Spell(SpellSlot.Q, 1050, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W, 325);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);
            Q.SetSkillshot(0.25f, 60, 2000, true, SkillshotType.SkillshotLine);

            var orbwalker = new Menu("Orbwalk", "rorb");
            Orbwalker = new Orbwalking.Orbwalker(orbwalker);
            MainMenu.AddSubMenu(orbwalker);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "QCol", "-> Smite Collision");
                    AddBool(comboMenu, "W", "Use W");
                    AddSlider(comboMenu, "WHpA", "-> If Hp >=", 20);
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "R", "Use R");
                    AddSlider(comboMenu, "RHpU", "-> If Hp <", 50);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddKeybind(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle);
                    AddSlider(harassMenu, "AutoQHpA", "-> If Hp >=", 30);
                    AddBool(harassMenu, "Q", "Use Q");
                    AddBool(harassMenu, "W", "Use W");
                    AddSlider(harassMenu, "WHpA", "-> If Hp >=", 20);
                    AddBool(harassMenu, "E", "Use E");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMob(clearMenu);
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "W", "Use W");
                    AddSlider(clearMenu, "WHpA", "-> If Hp >=", 20);
                    AddBool(clearMenu, "E", "Use E");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddBool(lastHitMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        AddBool(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    AddSlider(miscMenu, "WExtraRange", "W Extra Range Before Cancel", 60, 0, 200);
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddBool(drawMenu, "Q", "Q Range", false);
                    AddBool(drawMenu, "W", "W Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            AttackableUnit.OnDamage += OnDamage;
            Orbwalking.OnAttack += OnAttack;
        }

        private static bool HaveW
        {
            get { return Player.HasBuff("BurningAgony"); }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                return;
            }
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Fight("Combo");
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Fight("Harass");
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Clear();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    LastHit();
                    break;
            }
            if (GetValue<bool>("SmiteMob", "Auto") && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear)
            {
                SmiteMob();
            }
            AutoQ();
            KillSteal();
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (GetValue<bool>("Draw", "Q") && Q.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "W") && W.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnDamage(AttackableUnit sender, AttackableUnitDamageEventArgs args)
        {
            if (args.Target.NetworkId != Player.NetworkId || Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                return;
            }
            if (GetValue<bool>("Combo", "R") && R.IsReady() &&
                Player.HealthPercent < GetValue<Slider>("Combo", "RHpU").Value)
            {
                R.Cast();
            }
        }

        private static void OnAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!E.IsReady())
            {
                return;
            }
            if ((((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed) &&
                  target is AIHeroClient) || (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && target is Obj_AI_Minion)) &&
                GetValue<bool>(Orbwalker.ActiveMode.ToString(), "E"))
            {
                E.Cast();
            }
        }

        private static void Fight(string mode)
        {
            if (GetValue<bool>(mode, "Q") && Q.IsReady())
            {
                var target = Q.GetTarget();
                if (target != null)
                {
                    var state = Q.Cast(target);
                    if (state.IsCasted())
                    {
                        return;
                    }
                    if (state == Spell.CastStates.Collision && GetValue<bool>(mode, "QCol"))
                    {
                        var pred = Q.GetPrediction(target);
                        if (
                            pred.CollisionObjects.Count(
                                i => i.IsValid<Obj_AI_Minion>() && IsSmiteable((Obj_AI_Minion) i)) == 1 &&
                            CastSmite(pred.CollisionObjects.First()) && Q.Cast(pred.CastPosition))
                        {
                            return;
                        }
                    }
                }
            }
            if (GetValue<bool>(mode, "W") && W.IsReady())
            {
                if (Player.HealthPercent >= GetValue<Slider>(mode, "WHpA").Value &&
                    W.GetTarget(GetValue<Slider>("Misc", "WExtraRange").Value) != null)
                {
                    if (!HaveW)
                    {
                        W.Cast();
                    }
                }
                else if (HaveW)
                {
                    W.Cast();
                }
            }
        }

        private static void Clear()
        {
            SmiteMob();
            var minionObj = GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (!minionObj.Any())
            {
                if (GetValue<bool>("Clear", "W") && W.IsReady() && HaveW)
                {
                    W.Cast();
                }
                return;
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady())
            {
                if (Player.HealthPercent >= GetValue<Slider>("Clear", "WHpA").Value &&
                    (minionObj.Count(i => W.IsInRange(i, W.Range + GetValue<Slider>("Misc", "WExtraRange").Value)) > 1 ||
                     minionObj.Any(
                         i =>
                             i.MaxHealth >= 1200 &&
                             W.IsInRange(i, W.Range + GetValue<Slider>("Misc", "WExtraRange").Value))))
                {
                    if (!HaveW && W.Cast())
                    {
                        return;
                    }
                }
                else if (HaveW && W.Cast())
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady())
            {
                var list = minionObj.Where(i => Q.GetPrediction(i).Hitchance >= Q.MinHitChance).ToList();
                var obj = list.FirstOrDefault(i => Q.IsKillable(i)) ?? list.MinOrDefault(i => i.Distance(Player));
                if (obj != null)
                {
                    Q.Cast(obj);
                }
            }
        }

        private static void LastHit()
        {
            if (!GetValue<bool>("LastHit", "Q") || !Q.IsReady())
            {
                return;
            }
            var obj =
                GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Where(i => Q.IsKillable(i))
                    .FirstOrDefault(i => Q.GetPrediction(i).Hitchance >= Q.MinHitChance);
            if (obj == null)
            {
                return;
            }
            Q.Cast(obj);
        }

        private static void AutoQ()
        {
            if (!GetValue<KeyBind>("Harass", "AutoQ").Active ||
                Player.HealthPercent < GetValue<Slider>("Harass", "AutoQHpA").Value)
            {
                return;
            }
            Q.CastOnBestTarget(0);
        }

        private static void KillSteal()
        {
            if (GetValue<bool>("KillSteal", "Ignite") && Ignite.IsReady())
            {
                var target = TargetSelector.GetTarget(600, TargetSelector.DamageType.True);
                if (target != null && CastIgnite(target))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "Smite") &&
                (CurrentSmiteType == SmiteType.Blue || CurrentSmiteType == SmiteType.Red))
            {
                var target = TargetSelector.GetTarget(760, TargetSelector.DamageType.True);
                if (target != null && CastSmite(target))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "Q") && Q.IsReady())
            {
                var target = Q.GetTarget();
                if (target != null && Q.IsKillable(target))
                {
                    Q.Cast(target);
                }
            }
        }
    }
}