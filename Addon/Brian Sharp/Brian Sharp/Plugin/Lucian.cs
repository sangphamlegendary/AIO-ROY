using System;
using System.Collections.Generic;
using System.Linq;
using BrianSharp.Common;
using EloBuddy;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
//using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Lucian : Helper
    {
        private static Orbwalking.Orbwalker Orbwalker;
        private static bool _qCasted, _wCasted, _eCasted;

        public Lucian()
        {
            Q = new Spell(SpellSlot.Q, 675);
            Q2 = new Spell(SpellSlot.Q, 900);
            W = new Spell(SpellSlot.W, 1000, TargetSelector.DamageType.Magical);
            W2 = new Spell(SpellSlot.W, 1000, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 425);
            R = new Spell(SpellSlot.R, 1400);
            Q2.SetSkillshot(0.5f, 65, float.MaxValue, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 150, 1600, true, SkillshotType.SkillshotCircle);
            W2.SetSkillshot(0.25f, 150, 1600, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.5f, 110, 2800, true, SkillshotType.SkillshotLine);

            var orbwalker = new Menu("Orbwalk", "rorb");
            Orbwalker = new Orbwalking.Orbwalker(orbwalker);
            MainMenu.AddSubMenu(orbwalker);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "P", "Use Passive");
                    AddBool(comboMenu, "PSave", "-> Always Save", false);
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "QExtend", "-> Extend");
                    AddBool(comboMenu, "W", "Use W");
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "EGap", "-> Gap Closer");
                    AddSlider(comboMenu, "EDelay", "-> Stop Q/W If E Will Ready In (ms)", 500, 100, 1000);
                    AddList(comboMenu, "EMode", "-> Mode", new[] { "Safe", "Mouse", "Chase" });
                    AddKeybind(comboMenu, "EModeKey", "--> Key Switch", "Z", KeyBindType.Toggle).ValueChanged +=
                        ComboEModeChanged;
                    AddBool(comboMenu, "EModeDraw", "--> Draw Text", false);
                    AddBool(comboMenu, "R", "Use R If Killable");
                    AddBool(comboMenu, "RItem", "-> Use Youmuu For More Damage");
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddKeybind(harassMenu, "AutoQ", "Auto Q (Only Extend)", "H", KeyBindType.Toggle);
                    AddSlider(harassMenu, "AutoQMpA", "-> If Mp >=", 50);
                    AddBool(harassMenu, "P", "Use Passive");
                    AddBool(harassMenu, "PSave", "-> Always Save", false);
                    AddBool(harassMenu, "Q", "Use Q");
                    AddBool(harassMenu, "W", "Use W");
                    AddBool(harassMenu, "E", "Use E");
                    AddSlider(harassMenu, "EHpA", "-> If Hp >=", 20);
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "W", "Use W");
                    AddBool(clearMenu, "E", "Use E");
                    AddSlider(clearMenu, "EDelay", "-> Stop Q/W If E Will Ready In (ms)", 500, 100, 1000);
                    champMenu.AddSubMenu(clearMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddBool(fleeMenu, "E", "Use E");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "RStop", "Stop R For Kill Steal");
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "W", "Use W");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    AddBool(miscMenu, "LockR", "Lock R On Target");
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddBool(drawMenu, "Q", "Q Range", false);
                    AddBool(drawMenu, "W", "W Range", false);
                    AddBool(drawMenu, "E", "E Range", false);
                    AddBool(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnTick += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalking.AfterAttack += AfterAttack;
        }

        private static bool HavePassive
        {
            get
            {
                return (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear ||
                        GetValue<bool>(Orbwalker.ActiveMode.ToString(), "P")) &&
                       (_qCasted || _wCasted || _eCasted || Player.HasBuff("LucianPassiveBuff"));
            }
        }

        private static void ComboEModeChanged(object sender, OnValueChangeEventArgs e)
        {
            var mode = GetValue<StringList>("Combo", "EMode").SelectedIndex;
            GetItem("Combo", "EMode")
                .SetValue(new StringList(GetValue<StringList>("Combo", "EMode").SList, mode == 2 ? 0 : mode + 1));
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                return;
            }
            KillSteal();
            if (Player.IsCastingInterruptableSpell(true))
            {
                if (GetValue<bool>("Misc", "LockR"))
                {
                    LockROnTarget();
                }
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
                case Orbwalking.OrbwalkingMode.Flee:
                    if (GetValue<bool>("Flee", "E") && E.IsReady() && E.Cast(Game.CursorPos))
                    {
                        return;
                    }
                    break;
            }
            AutoQ();
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (GetValue<bool>("Combo", "E") && GetValue<bool>("Combo", "EModeDraw"))
            {
                var pos = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(pos.X, pos.Y, Color.Orange, GetValue<StringList>("Combo", "EMode").SelectedValue);
            }
            if (GetValue<bool>("Draw", "Q") && Q.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "W") && W.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "R") && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (args.SData.Name == "LucianQ")
            {
                _qCasted = true;
                LeagueSharp.Common.Utility.DelayAction.Add((int) (Q2.Delay * 1000) + 50, () => _qCasted = false);
            }
            if (args.SData.Name == "LucianW")
            {
                _wCasted = true;
                LeagueSharp.Common.Utility.DelayAction.Add((int) (W.Delay * 1000) + 50, () => _wCasted = false);
            }
            if (args.SData.Name == "LucianE")
            {
                _eCasted = true;
                LeagueSharp.Common.Utility.DelayAction.Add(100, () => _eCasted = false);
            }
        }

        private static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!E.IsReady())
            {
                return;
            }
            if (((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && target is Obj_AI_Minion) ||
                 ((Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ||
                   (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed &&
                    Player.HealthPercent >= GetValue<Slider>("Harass", "EHpA").Value)) && target is AIHeroClient)) &&
                GetValue<bool>(Orbwalker.ActiveMode.ToString(), "E") && !HavePassive)
            {
                var obj = (Obj_AI_Base) target;
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed ||
                    (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo &&
                     GetValue<StringList>("Combo", "EMode").SelectedIndex == 0))
                {
                    var pos = Geometry.CircleCircleIntersection(
                        Player.ServerPosition.To2D(), Prediction.GetPrediction(obj, 0.25f).UnitPosition.To2D(), E.Range,
                        Orbwalking.GetAutoAttackRange(obj));
                    if (pos.Count() > 0)
                    {
                        E.Cast(pos.MinOrDefault(i => i.Distance(Game.CursorPos)));
                    }
                    else
                    {
                        E.Cast(Player.ServerPosition.Extend(obj.ServerPosition, -E.Range));
                    }
                }
                else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                {
                    switch (GetValue<StringList>("Combo", "EMode").SelectedIndex)
                    {
                        case 1:
                            E.Cast(Player.ServerPosition.Extend(Game.CursorPos, E.Range));
                            break;
                        case 2:
                            E.Cast(obj.ServerPosition);
                            break;
                    }
                }
            }
        }

        private static void Fight(string mode)
        {
            if (mode == "Combo" && GetValue<bool>(mode, "R") && R.IsReady())
            {
                var target = R.GetTarget();
                if (target != null && CanKill(target, GetRDmg(target)))
                {
                    if (Player.Distance(target) > 550 ||
                        (!Orbwalking.InAutoAttackRange(target) && (!GetValue<bool>(mode, "Q") || !Q.IsReady()) &&
                         (!GetValue<bool>(mode, "W") || !W.IsReady()) && (!GetValue<bool>(mode, "E") || !E.IsReady())))
                    {
                        if (R.Cast(target).IsCasted())
                        {
                            if (GetValue<bool>(mode, "RItem") && Youmuu.IsReady())
                            {
                                LeagueSharp.Common.Utility.DelayAction.Add(10, () => Youmuu.Cast());
                            }
                            return;
                        }
                    }
                }
            }
            if (mode == "Combo" && GetValue<bool>(mode, "E") && GetValue<bool>(mode, "EGap") && E.IsReady())
            {
                var target = E.GetTarget(Orbwalking.GetAutoAttackRange() - 30);
                if (target != null && !Orbwalking.InAutoAttackRange(target) &&
                    Orbwalking.InAutoAttackRange(target, 20, Player.ServerPosition.Extend(Game.CursorPos, E.Range)) &&
                    E.Cast(Player.ServerPosition.Extend(Game.CursorPos, E.Range)))
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "PSave") && HavePassive)
            {
                return;
            }
            if (GetValue<bool>(mode, "E") &&
                (E.IsReady() || (mode == "Combo" && E.IsReady(GetValue<Slider>(mode, "EDelay").Value))))
            {
                return;
            }
            if (GetValue<bool>(mode, "Q") && Q.IsReady())
            {
                var target = Q.GetTarget() ?? Q2.GetTarget();
                if (target != null)
                {
                    if (((Orbwalking.InAutoAttackRange(target) && !HavePassive) ||
                         (!Orbwalking.InAutoAttackRange(target, 20) && Q.IsInRange(target))) &&
                        Q.CastOnUnit(target))
                    {
                        return;
                    }
                    if ((mode == "Harass" || GetValue<bool>(mode, "QExtend")) && !Q.IsInRange(target) &&
                        CastExtendQ(target))
                    {
                        return;
                    }
                }
            }
            if ((!GetValue<bool>(mode, "Q") || !Q.IsReady()) && GetValue<bool>(mode, "W") && W.IsReady() &&
                !Player.IsDashing())
            {
                var target = W.GetTarget();
                if (target != null &&
                    ((Orbwalking.InAutoAttackRange(target) && !HavePassive) || !Orbwalking.InAutoAttackRange(target, 20)))
                {
                    if (Orbwalking.InAutoAttackRange(target))
                    {
                        W2.CastIfWillHit(target, -1);
                    }
                    else
                    {
                        W.CastIfWillHit(target, -1);
                    }
                }
            }
        }

        private static void Clear()
        {
            var minionObj =
                GetMinions(Q2.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Cast<Obj_AI_Base>()
                    .ToList();
            if (!minionObj.Any())
            {
                return;
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady(GetValue<Slider>("Clear", "EDelay").Value))
            {
                return;
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady() && !HavePassive)
            {
                var obj =
                    minionObj.Where(i => Q.IsInRange(i))
                        .MaxOrDefault(
                            i => Q2.CountHits(minionObj, Player.ServerPosition.Extend(i.ServerPosition, Q2.Range)));
                if (obj != null && Q.CastOnUnit(obj))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady() && !Player.IsDashing() && !HavePassive)
            {
                var pos = W.GetCircularFarmLocation(minionObj.Where(i => W.IsInRange(i)).ToList());
                if (pos.MinionsHit > 1)
                {
                    W.Cast(pos.Position);
                }
                else
                {
                    var obj =
                        minionObj.Where(i => W.GetPrediction(i).Hitchance >= W.MinHitChance)
                            .MinOrDefault(i => i.Distance(Player));
                    if (obj != null)
                    {
                        W.Cast(obj);
                    }
                }
            }
        }

        private static void AutoQ()
        {
            if (!GetValue<KeyBind>("Harass", "AutoQ").Active ||
                Player.ManaPercent < GetValue<Slider>("Harass", "AutoQMpA").Value || !Q.IsReady())
            {
                return;
            }
            var target = Q2.GetTarget();
            if (target == null || Q.IsInRange(target))
            {
                return;
            }
            CastExtendQ(target);
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
            if (Player.IsDashing() ||
                (!GetValue<bool>("KillSteal", "RStop") && Player.IsCastingInterruptableSpell(true)))
            {
                return;
            }
            var cancelR = GetValue<bool>("KillSteal", "RStop") && Player.IsCastingInterruptableSpell(true);
            if (GetValue<bool>("KillSteal", "Q") && Q.IsReady())
            {
                var target = Q.GetTarget() ?? Q2.GetTarget();
                if (target != null && Q.IsKillable(target))
                {
                    if (Q.IsInRange(target))
                    {
                        if ((!cancelR || R.Cast()) && Q.CastOnUnit(target))
                        {
                            return;
                        }
                    }
                    else if (CastExtendQ(target, cancelR))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>("KillSteal", "W") && W.IsReady() && !Player.IsDashing())
            {
                var target = W.GetTarget();
                if (target != null && W.IsKillable(target) && (!cancelR || R.Cast()))
                {
                    W.Cast(target);
                }
            }
        }

        private static void LockROnTarget()
        {
            var target = R.GetTarget();
            if (target == null)
            {
                return;
            }
            var endPos = (Player.ServerPosition - target.ServerPosition).Normalized();
            var predPos = R.GetPrediction(target).CastPosition.To2D();
            var fullPoint = new Vector2(predPos.X + endPos.X * R.Range * 0.98f, predPos.Y + endPos.Y * R.Range * 0.98f);
            var closestPoint = Player.ServerPosition.To2D().Closest(new List<Vector2> { predPos, fullPoint });
            if (closestPoint.IsValid() && !closestPoint.IsWall() && predPos.Distance(closestPoint) > E.Range)
            {
                EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, closestPoint.To3D());
            }
            else if (fullPoint.IsValid() && !fullPoint.IsWall() && predPos.Distance(fullPoint) < R.Range &&
                     predPos.Distance(fullPoint) > 100)
            {
                EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, fullPoint.To3D());
            }
        }

        private static bool CastExtendQ(AIHeroClient target, bool cancelR = false)
        {
            var objNear = new List<Obj_AI_Base>();
            objNear.AddRange(HeroManager.Enemies.Where(i => i.IsValidTarget(Q.Range)));
            objNear.AddRange(GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly));
            var obj =
                objNear.FirstOrDefault(
                    i => Q2.WillHit(target, Player.ServerPosition.Extend(i.ServerPosition, Q2.Range)));
            return obj != null && (!cancelR || R.Cast()) && Q.CastOnUnit(obj);
        }

        private static double GetRDmg(AIHeroClient target)
        {
            var shot = (int) (7.5 + new[] { 7.5, 9, 10.5 }[R.Level - 1] * 1 / Player.AttackDelay);
            var maxShot = new[] { 26, 30, 33 }[R.Level - 1];
            return Player.CalcDamage(
                target, Damage.DamageType.Physical,
                (new[] { 40, 50, 60 }[R.Level - 1] + 0.25 * Player.FlatPhysicalDamageMod +
                 0.1 * Player.FlatMagicDamageMod) * (shot > maxShot ? maxShot : shot));
        }
    }
}