﻿namespace ElLeeSin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using EloBuddy;
    //using EloBuddy.SDK;
    using LeagueSharp.Common;

    using SharpDX;

    using ItemData = LeagueSharp.Common.Data.ItemData;
    using Utility = LeagueSharp.Common.Utility;
    using Spell = LeagueSharp.Common.Spell;
    using TargetSelector = LeagueSharp.Common.TargetSelector;

    internal static class Program
    {
        #region Constants

        private const int FlashRange = 425;

        /// <summary>
        ///     Lee Sin R kick distance
        /// </summary>
        private const float LeeSinRKickDistance = 700;

        /// <summary>
        ///     Lee Sin R kick width
        /// </summary>
        private const float LeeSinRKickWidth = 100;

        private const int WardRange = 600;

        #endregion

        #region Static Fields

        public static bool CheckQ = true;

        public static bool ClicksecEnabled;

        public static Vector3 InsecClickPos;

        public static Vector2 InsecLinePos;

        public static Vector2 JumpPos;

        public static int LastQ, LastQ2, LastW, LastW2, LastE, LastE2, LastR, LastSpell, PassiveStacks;

        public static float LastWard;

        public static Orbwalking.Orbwalker Orbwalker;

        public static AIHeroClient Player = ObjectManager.Player;

        public static Spell smite = null;

        public static Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>
                                                             {
                                                                 { Spells.Q, new Spell(SpellSlot.Q, 1100) },
                                                                 { Spells.W, new Spell(SpellSlot.W, 700) },
                                                                 { Spells.E, new Spell(SpellSlot.E, 425) },
                                                                 { Spells.R, new Spell(SpellSlot.R, 375) },
                                                                 { Spells.R2, new Spell(SpellSlot.R, 800) }
                                                             };

        public static int Wcasttime;

        private static readonly bool castWardAgain = true;

        private static readonly int[] SmiteBlue = { 3706, 1403, 1402, 1401, 1400 };

        private static readonly int[] SmiteRed = { 3715, 1415, 1414, 1413, 1412 };

        private static readonly string[] SpellNames =
            {
                "blindmonkqone", "blindmonkwone", "blindmonkeone",
                "blindmonkwtwo", "blindmonkqtwo", "blindmonketwo",
                "blindmonkrkick"
            };

        private static bool castQAgain;

        private static int clickCount;

        private static float doubleClickReset;

        private static SpellSlot flashSlot;

        private static SpellSlot igniteSlot;

        private static InsecComboStepSelect insecComboStep;

        private static Vector3 insecPos;

        private static bool isInQ2;

        private static bool isNullInsecPos = true;

        private static bool lastClickBool;

        private static Vector3 lastClickPos;

        private static float lastPlaced;

        private static Vector3 lastWardPos;

        private static Vector3 mouse = Game.CursorPos;

        private static float passiveTimer;

        private static bool q2Done;

        private static float q2Timer;

        private static bool reCheckWard = true;

        private static float resetTime;

        private static bool waitingForQ2;

        private static bool wardJumped;

        #endregion

        #region Enums

        internal enum Spells
        {
            Q,

            W,

            E,

            R,

            R2
        }

        private enum InsecComboStepSelect
        {
            None,

            Qgapclose,

            Wgapclose,

            Pressr
        };

        private enum WCastStage
        {
            First,

            Second,

            Cooldown
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the Q Instance name
        /// </summary>
        /// <value>
        ///     Q instance name
        /// </value>
        public static bool QState
            => spells[Spells.Q].Instance.Name.Equals("BlindMonkQOne", StringComparison.InvariantCultureIgnoreCase);

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the E Instance name
        /// </summary>
        /// <value>
        ///     E instance name
        /// </value>
        private static bool EState
            => spells[Spells.E].Instance.Name.Equals("BlindMonkEOne", StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        ///     Gets Ravenous Hydra
        /// </summary>
        /// <value>
        ///     Ravenous Hydra
        /// </value>
        private static Items.Item Hydra => ItemData.Ravenous_Hydra_Melee_Only.GetItem();

        /// <summary>
        ///     Gets the Q2 Instance name
        /// </summary>
        /// <value>
        ///     Q2 instance name
        /// </value>
        private static bool Q2State
            => spells[Spells.Q].Instance.Name.Equals("BlindMonkQTwo", StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        ///     Gets Tiamat Item
        /// </summary>
        /// <value>
        ///     Tiamat Item
        /// </value>
        private static Items.Item Tiamat => ItemData.Tiamat_Melee_Only.GetItem();

        /// <summary>
        ///     Gets Titanic Hydra
        /// </summary>
        /// <value>
        ///     Titanic Hydra
        /// </value>
        private static Items.Item Titanic => ItemData.Titanic_Hydra_Melee_Only.GetItem();

        private static float WardFlashRange => WardRange + spells[Spells.R].Range - 100;

        /// <summary>
        ///     Gets the W Instance name
        /// </summary>
        /// <value>
        ///     W instance name
        /// </value>
        private static WCastStage WStage
        {
            get
            {
                if (!spells[Spells.W].IsReady())
                {
                    return WCastStage.Cooldown;
                }

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W)
                            .Name.Equals("blindmonkwtwo", StringComparison.InvariantCultureIgnoreCase)
                            ? WCastStage.Second
                            : WCastStage.First);
            }
        }

        /// <summary>
        ///     Gets the W Instance name
        /// </summary>
        /// <value>
        ///     W instance name
        /// </value>
        private static bool WState
            => spells[Spells.W].Instance.Name.Equals("BlindMonkWOne", StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        ///     Gets Youmuus Ghostblade
        /// </summary>
        /// <value>
        ///     Youmuus Ghostblade
        /// </value>
        private static Items.Item Youmuu => ItemData.Youmuus_Ghostblade.GetItem();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Cast items
        /// </summary>
        /// <param name="target"></param>
        /// <returns>true or false</returns>
        public static bool CastItems(Obj_AI_Base target)
        {
            if (Player.IsDashing() || isInQ2 || Player.Spellbook.IsAutoAttacking)
            {
                return false;
            }

            var heroes = Player.GetEnemiesInRange(385).Count;
            var count = heroes;

            var tiamat = Tiamat;
            if (tiamat.IsReady() && count > 0 && tiamat.Cast())
            {
                return true;
            }

            var hydra = Hydra;
            if (Hydra.IsReady() && count > 0 && hydra.Cast())
            {
                return true;
            }

            var youmuus = Youmuu;
            if (Youmuu.IsReady() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo
                || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed && youmuus.Cast())
            {
                return true;
            }

            var titanic = Titanic;
            return titanic.IsReady() && count > 0 && titanic.Cast();
        }

        public static InventorySlot FindBestWardItem()
        {
            try
            {
                var slot = Items.GetWardSlot();
                if (slot == default(InventorySlot))
                {
                    return null;
                }

                var sdi = GetItemSpell(slot);
                if (sdi != default(SpellDataInst) && sdi.State == SpellState.Ready)
                {
                    return slot;
                }
                return slot;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        public static Vector3 GetInsecPos(AIHeroClient target)
        {
            try
            {
                if (ClicksecEnabled && ParamBool("clickInsec"))
                {
                    InsecLinePos = Drawing.WorldToScreen(InsecClickPos);
                    return V2E(InsecClickPos, target.Position, target.Distance(InsecClickPos) + 230).To3D();
                }
                if (isNullInsecPos)
                {
                    isNullInsecPos = false;
                    insecPos = Player.Position;
                }

                if (GetAllyHeroes(target, 1500).Count > 0 && ParamBool("ElLeeSin.Insec.Ally"))
                {
                    var insecPosition = InterceptionPoint(GetAllyInsec(GetAllyHeroes(target, 1500)));

                    InsecLinePos = Drawing.WorldToScreen(insecPosition);
                    return V2E(insecPosition, target.Position, target.Distance(insecPosition) + 230).To3D();
                }

                if (ParamBool("ElLeeSin.Insec.Tower"))
                {
                    var tower =
                        ObjectManager.Get<Obj_AI_Turret>()
                            .Where(
                                turret =>
                                turret.Distance(target) - 725 <= 950 && turret.IsAlly && turret.IsVisible
                                && turret.Health > 0 && turret.Distance(target) <= 1300 && turret.Distance(target) > 400)
                            .MinOrDefault(i => target.Distance(Player));

                    if (tower != null)
                    {
                        InsecLinePos = Drawing.WorldToScreen(tower.Position);
                        return V2E(tower.Position, target.Position, target.Distance(tower.Position) + 230).To3D();
                    }
                }

                if (ParamBool("ElLeeSin.Insec.Original.Pos"))
                {
                    InsecLinePos = Drawing.WorldToScreen(insecPos);
                    return V2E(insecPos, target.Position, target.Distance(insecPos) + 230).To3D();
                }
                return new Vector3();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
            return new Vector3();
        }

        public static bool HasQBuff(this Obj_AI_Base unit)
        {
            return (unit.HasAnyBuffs("BlindMonkQOne") || unit.HasAnyBuffs("blindmonkqonechaos")
                    || unit.HasAnyBuffs("BlindMonkSonicWave") || unit.HasBuff("BlindMonkSonicWave"));
        }

        public static void Orbwalk(Vector3 pos, AIHeroClient target = null)
        {
            EloBuddy.Player.IssueOrder(GameObjectOrder.MoveTo, pos);
        }

        public static bool ParamBool(string paramName)
        {
            return InitMenu.Menu.Item(paramName).IsActive();
        }

        /// <summary>
        ///     Gets the Q2 damage
        /// </summary>
        /// <param name="target"></param>
        /// <param name="subHP"></param>
        /// <param name="monster"></param>
        /// <returns></returns>
        public static float Q2Damage(Obj_AI_Base target, float subHP = 0, bool monster = false)
        {
            var dmg = new[] { 50, 80, 110, 140, 170 }[spells[Spells.Q].Level - 1] + 0.9 * Player.FlatPhysicalDamageMod
                      + 0.08 * (target.MaxHealth - (target.Health - subHP));

            return
                (float)
                (Player.CalcDamage(
                    target,
                    LeagueSharp.Common.Damage.DamageType.Physical,
                    target is Obj_AI_Minion ? Math.Min(dmg, 400) : dmg) + subHP);
        }

        #endregion

        #region Methods

        private static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var targ = target as Obj_AI_Base;
            if (!unit.IsMe || targ == null)
            {
                return;
            }

            Orbwalker.SetOrbwalkingPoint(Vector3.Zero);
            var mode = Orbwalker.ActiveMode;
            if (mode.Equals(Orbwalking.OrbwalkingMode.None) || mode.Equals(Orbwalking.OrbwalkingMode.LastHit)
                || mode.Equals(Orbwalking.OrbwalkingMode.LaneClear))
            {
                return;
            }

            if (spells[Spells.E].IsReady() && target.Type == GameObjectType.AIHeroClient && EState)
            {
                spells[Spells.E].Cast();
            }

            if (ParamBool("Combo.Use.items"))
            {
                CastItems(targ);
            }
        }

        private static void AllClear()
        {
            try
            {
                var minions = MinionManager.GetMinions(spells[Spells.Q].Range).FirstOrDefault();
                if (minions == null)
                {
                    return;
                }

                UseItems(minions);

                if (ParamBool("ElLeeSin.Lane.Q") && !QState && spells[Spells.Q].IsReady() && minions.HasQBuff()
                    && (LastQ + 2700 < Environment.TickCount || spells[Spells.Q].GetDamage(minions, 1) > minions.Health
                        || minions.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player) + 50))
                {
                    spells[Spells.Q].Cast();
                }

                if (spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Lane.Q") && LastQ + 200 < Environment.TickCount)
                {
                    if (QState && minions.Distance(Player) < spells[Spells.Q].Range)
                    {
                        spells[Spells.Q].Cast(minions);
                    }
                }

                if (ParamBool("ElLeeSin.Combo.AAStacks")
                    && PassiveStacks > InitMenu.Menu.Item("ElLeeSin.Combo.PassiveStacks").GetValue<Slider>().Value
                    && Orbwalking.GetRealAutoAttackRange(Player) > Player.Distance(minions))
                {
                    return;
                }

                if (spells[Spells.E].IsReady() && ParamBool("ElLeeSin.Lane.E"))
                {
                    if (EState && spells[Spells.E].IsInRange(minions))
                    {
                        LastE = Environment.TickCount;
                        spells[Spells.E].Cast();
                        return;
                    }

                    if (!EState && spells[Spells.E].IsInRange(minions) && LastE + 400 < Environment.TickCount)
                    {
                        spells[Spells.E].Cast();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            var targ = args.Target as AIHeroClient;
            if (!args.Unit.IsMe || targ == null)
            {
                return;
            }

            if (!spells[Spells.E].IsReady() || !Orbwalker.ActiveMode.Equals(Orbwalking.OrbwalkingMode.Combo))
            {
                return;
            }

            if (targ.Distance(Player) <= Orbwalking.GetRealAutoAttackRange(Player))
            {
                if (ParamBool("Combo.Use.items"))
                {
                    CastItems(targ);
                }
            }
        }

        private static void CastE()
        {
            if (!spells[Spells.E].IsReady() || isInQ2 || Player.IsDashing() || Player.Spellbook.IsCastingSpell)
            {
                return;
            }

            if (EState)
            {
                var target = HeroManager.Enemies.Where(x => x.IsValidTarget(spells[Spells.E].Range + 20)).ToList();

                if (target.Count == 0)
                {
                    return;
                }

                if ((PassiveStacks == 0 && Player.Mana >= 70 || target.Count >= 2) || Orbwalker.GetTarget() == null
                        ? target.Any(t => t.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player) + 100)
                        : PassiveStacks < 2)
                {
                    spells[Spells.E].Cast();
                }
            }
            else
            {
                var target =
                    HeroManager.Enemies.Where(i => HasEBuff(i) && i.IsValidTarget(spells[Spells.E].Range)).ToList();
                if (target.Count == 0)
                {
                    return;
                }

                if ((PassiveStacks == 0 || target.Count >= 2)
                    || target.Any(t => t.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player) + 50))
                {
                    spells[Spells.E].Cast();
                }
            }
        }

        private static void CastQ(Obj_AI_Base target, bool smiteQ = false)
        {
            if (!spells[Spells.Q].IsReady() || !target.IsValidTarget(spells[Spells.Q].Range))
            {
                return;
            }

            var prediction = spells[Spells.Q].GetPrediction(
                target,
                false,
                -1,
                new[] { CollisionableObjects.YasuoWall, CollisionableObjects.Minions });

            if (prediction.Hitchance >= HitChance.High || prediction.Hitchance >= HitChance.Immobile)
            {
                if (prediction.CollisionObjects.Count == 0)
                {
                    spells[Spells.Q].Cast(prediction.CastPosition);
                }
            }
            else if (ParamBool("ElLeeSin.Smite.Q") && spells[Spells.Q].IsReady()
                     && target.IsValidTarget(spells[Spells.Q].Range)
                     && prediction.CollisionObjects.Count(a => a.NetworkId != target.NetworkId && a.IsMinion) == 1
                     && Player.GetSpellSlot(SmiteSpellName()).IsReady())
            {
                Player.Spellbook.CastSpell(
                    Player.GetSpellSlot(SmiteSpellName()),
                    prediction.CollisionObjects.Where(a => a.NetworkId != target.NetworkId && a.IsMinion).ToList()[0
                        ]);

                spells[Spells.Q].Cast(prediction.CastPosition);
            }
        }

        private static void CastW(Obj_AI_Base obj)
        {
            if (500 >= Utils.TickCount - Wcasttime || WStage != WCastStage.First)
            {
                return;
            }

            spells[Spells.W].CastOnUnit(obj);
            Wcasttime = Utils.TickCount;
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Physical);
            if (target == null)
            {
                return;
            }

            if (spells[Spells.R].IsReady() && ParamBool("ElLeeSin.Combo.R") && ParamBool("ElLeeSin.Combo.Q") && ParamBool("ElLeeSin.Combo.Q2"))
            {
                var qTarget = HeroManager.Enemies.FirstOrDefault(x => x.HasBuff("BlindMonkSonicWave"));
                if (qTarget != null)
                {
                    if (target.Health + target.AttackShield
                        > spells[Spells.Q].GetDamage(target, 1) + Player.GetAutoAttackDamage(target)
                        && target.Health + target.AttackShield
                        <= Q2Damage(target, spells[Spells.R].GetDamage(target) - 100) + Player.GetAutoAttackDamage(target))
                    {
                        if (spells[Spells.R].CastOnUnit(target))
                        {
                            return;
                        }

                        if (ParamBool("ElLeeSin.Combo.StarKill1") && !spells[Spells.R].IsInRange(target)
                            && target.Distance(Player) < 600 + spells[Spells.R].Range - 50 && Player.Mana >= 80
                            && !Player.IsDashing())
                        {
                            WardJump(target.Position, false, true);
                        }
                    }
                }
            }

            if (ParamBool("ElLeeSin.Combo.Q") && spells[Spells.Q].IsReady())
            {
                if (QState)
                {
                    CastQ(target, ParamBool("ElLeeSin.Smite.Q"));
                }
            }

            if (ParamBool("ElLeeSin.Combo.Q2") && !Player.IsDashing() && target.HasQBuff()
                && target.IsValidTarget(1300f) && !isInQ2)
            {
                if ((castQAgain || spells[Spells.Q].GetDamage(target, 1) > target.Health + target.AttackShield)
                    || ReturnQBuff()?.Distance(target) < Player.Distance(target)
                    && !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                {
                    spells[Spells.Q].Cast(target);
                }
            }

            if (spells[Spells.R].GetDamage(target) >= (target.Health + 100) && ParamBool("ElLeeSin.Combo.KS.R")
                && target.IsValidTarget(spells[Spells.R].Range))
            {
                spells[Spells.R].CastOnUnit(target);
            }

            if (ParamBool("ElLeeSin.Combo.AAStacks")
                && PassiveStacks > InitMenu.Menu.Item("ElLeeSin.Combo.PassiveStacks").GetValue<Slider>().Value
                && Orbwalking.GetRealAutoAttackRange(Player) > Player.Distance(target))
            {
                return;
            }

            if (ParamBool("ElLeeSin.Combo.W"))
            {
                if (ParamBool("ElLeeSin.Combo.Mode.WW")
                    && target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player))
                {
                    WardJump(target.Position, false, true);
                }

                if (!ParamBool("ElLeeSin.Combo.Mode.WW") && target.Distance(Player) > spells[Spells.Q].Range)
                {
                    WardJump(target.Position, false, true);
                }
            }

            if (ParamBool("ElLeeSin.Combo.E"))
            {
                CastE();
            }

            if (spells[Spells.W].IsReady() && ParamBool("ElLeeSin.Combo.W2")
                && (!ParamBool("ElLeeSin.Combo.W") || !ParamBool("ElLeeSin.Combo.Mode.WW")))
            {
                if (Environment.TickCount - LastE <= 250)
                {
                    return;
                }

                if (WState && target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                {
                    spells[Spells.W].Cast();
                    LastW = Environment.TickCount;
                }

                if (!WState && LastW + 1800 < Environment.TickCount)
                {
                    spells[Spells.W].Cast();
                }
            }
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            try
            {
                if (ObjectManager.Player.ChampionName != "LeeSin")
                {
                    return;
                }
                
                igniteSlot = Player.GetSpellSlot("summonerdot");
                flashSlot = Player.GetSpellSlot("summonerflash");

                spells[Spells.Q].SetSkillshot(0.25f, 60f, 1800f, true, SkillshotType.SkillshotLine);
                spells[Spells.E].SetTargetted(0.25f, float.MaxValue);
                spells[Spells.R2].SetSkillshot(0.25f, 100, 1500, false, SkillshotType.SkillshotLine);

                JumpHandler.Load();

                InitMenu.Initialize();

                Drawing.OnDraw += Drawings.OnDraw;
                Game.OnUpdate += Game_OnGameUpdate;
                Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
                GameObject.OnCreate += OnCreate;
                Orbwalking.AfterAttack += AfterAttack;
                Orbwalking.BeforeAttack += BeforeAttack;
                GameObject.OnDelete += GameObject_OnDelete;
                Game.OnWndProc += Game_OnWndProc;
                Obj_AI_Base.OnBuffGain += OnBuffAdd;
                Obj_AI_Base.OnBuffLose += OnBuffRemove;
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            try
            {
                if (doubleClickReset <= Environment.TickCount && clickCount != 0)
                {
                    doubleClickReset = float.MaxValue;
                    clickCount = 0;
                }

                if (clickCount >= 2)
                {
                    resetTime = Environment.TickCount + 3000;
                    ClicksecEnabled = true;
                    InsecClickPos = Game.CursorPos;
                    clickCount = 0;
                }

                if (passiveTimer <= Environment.TickCount)
                {
                    PassiveStacks = 0;
                }

                if (resetTime <= Environment.TickCount && !InitMenu.Menu.Item("InsecEnabled").GetValue<KeyBind>().Active
                    && ClicksecEnabled)
                {
                    ClicksecEnabled = false;
                }

                if (q2Timer <= Environment.TickCount)
                {
                    q2Done = false;
                }

                if (Player.IsDead || Player.IsRecalling())
                {
                    return;
                }

                if ((ParamBool("insecMode")
                         ? TargetSelector.GetSelectedTarget()
                         : TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Physical)) == null)
                {
                    insecComboStep = InsecComboStepSelect.None;
                }

                if (InitMenu.Menu.Item("starCombo").GetValue<KeyBind>().Active)
                {
                    WardCombo();
                }

                if (ParamBool("ElLeeSin.Ignite.KS"))
                {
                    var newTarget = TargetSelector.GetTarget(600f, TargetSelector.DamageType.True);

                    if (newTarget != null && igniteSlot != SpellSlot.Unknown
                        && Player.Spellbook.CanUseSpell(igniteSlot) == SpellState.Ready
                        && ObjectManager.Player.GetSummonerSpellDamage(newTarget, LeagueSharp.Common.Damage.SummonerSpell.Ignite)
                        > newTarget.Health)
                    {
                        Player.Spellbook.CastSpell(igniteSlot, newTarget);
                    }
                }

                if (InitMenu.Menu.Item("InsecEnabled").GetValue<KeyBind>().Active)
                {
                    if (ParamBool("insecOrbwalk"))
                    {
                        Orbwalk(Game.CursorPos);
                    }

                    var newTarget = ParamBool("insecMode")
                                        ? (TargetSelector.GetSelectedTarget()
                                           ?? TargetSelector.GetTarget(
                                               spells[Spells.Q].Range,
                                               TargetSelector.DamageType.Physical))
                                        : TargetSelector.GetTarget(
                                            spells[Spells.Q].Range,
                                            TargetSelector.DamageType.Physical);

                    if (newTarget != null)
                    {
                        InsecCombo(newTarget);
                    }
                }
                else
                {
                    isNullInsecPos = true;
                    wardJumped = false;
                }

                if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
                {
                    insecComboStep = InsecComboStepSelect.None;
                }

                switch (Orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        Combo();
                        break;
                    case Orbwalking.OrbwalkingMode.LaneClear:
                        AllClear();
                        JungleClear();
                        break;
                    case Orbwalking.OrbwalkingMode.Mixed:
                        Harass();
                        break;
                }

                if (InitMenu.Menu.Item("ElLeeSin.Wardjump").GetValue<KeyBind>().Active)
                {
                    WardjumpToMouse();
                }

                if (InitMenu.Menu.Item("ElLeeSin.Insec.UseInstaFlash").GetValue<KeyBind>().Active)
                {
                    var target = TargetSelector.GetTarget(spells[Spells.R].Range, TargetSelector.DamageType.Physical);
                    if (target == null)
                    {
                        return;
                    }

                    if (spells[Spells.R].IsReady() && !target.IsZombie
                        && Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready
                        && target.IsValidTarget(spells[Spells.R].Range))
                    {
                        spells[Spells.R].CastOnUnit(target);
                    }
                }

                if (ParamBool("ElLeeSin.Combo.New"))
                {
                    var minREnemies = InitMenu.Menu.Item("ElLeeSin.Combo.R.Count").GetValue<Slider>().Value;
                    foreach (var enemy in HeroManager.Enemies)
                    {
                        var startPos = enemy.ServerPosition;
                        var endPos = Player.ServerPosition.Extend(
                            startPos,
                            Player.Distance(enemy) + LeeSinRKickDistance);

                        var rectangle = new Geometry.Polygon.Rectangle(startPos, endPos, LeeSinRKickWidth);
                        if (HeroManager.Enemies.Count(x => rectangle.IsInside(x)) >= minREnemies)
                        {
                            spells[Spells.R].Cast(enemy);
                        }
                    }
                }

                if (ParamBool("ElLeeSin.Combo.New.R.Kill"))
                {
                    foreach (var enemy2 in HeroManager.Enemies)
                    {
                        var startPos = enemy2.ServerPosition;
                        var endPos = Player.ServerPosition.Extend(
                            startPos,
                            Player.Distance(enemy2) + LeeSinRKickDistance);

                        var rectangle = new Geometry.Polygon.Rectangle(startPos, endPos, LeeSinRKickWidth);
                        if (
                            HeroManager.Enemies.Where(x => rectangle.IsInside(x) && !x.IsDead)
                                .Any(i => spells[Spells.R].IsKillable(i)) && spells[Spells.R].CastOnUnit(enemy2))
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowsMessages.WM_LBUTTONDOWN)
            {
                return;
            }

            var asec = HeroManager.Enemies.Where(a => a.Distance(Game.CursorPos) < 200 && a.IsValid && !a.IsDead);

            if (asec.Any())
            {
                return;
            }
            if (!lastClickBool || clickCount == 0)
            {
                clickCount++;
                lastClickPos = Game.CursorPos;
                lastClickBool = true;
                doubleClickReset = Environment.TickCount + 600;
                return;
            }
            if (lastClickBool && lastClickPos.Distance(Game.CursorPos) < 200)
            {
                clickCount++;
                lastClickBool = false;
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_GeneralParticleEmitter))
            {
                return;
            }
            if (sender.Name.Contains("blindmonk_q_resonatingstrike") && waitingForQ2)
            {
                waitingForQ2 = false;
                q2Done = true;
                q2Timer = Environment.TickCount + 800;
            }
        }

        private static List<AIHeroClient> GetAllyHeroes(AIHeroClient position, int range)
        {
            return
                HeroManager.Allies.Where(hero => !hero.IsMe && !hero.IsDead && hero.Distance(position) < range).ToList();
        }

        private static List<AIHeroClient> GetAllyInsec(List<AIHeroClient> heroes)
        {
            byte alliesAround = 0;
            var tempObject = new AIHeroClient();
            foreach (var hero in heroes)
            {
                var localTemp =
                    GetAllyHeroes(hero, 500 + InitMenu.Menu.Item("bonusRangeA").GetValue<Slider>().Value).Count;
                if (localTemp > alliesAround)
                {
                    tempObject = hero;
                    alliesAround = (byte)localTemp;
                }
            }
            return GetAllyHeroes(tempObject, 500 + InitMenu.Menu.Item("bonusRangeA").GetValue<Slider>().Value);
        }

        private static SpellDataInst GetItemSpell(InventorySlot invSlot)
        {
            return Player.Spellbook.Spells.FirstOrDefault(spell => (int)spell.Slot == invSlot.Slot + 4);
        }

        private static void Harass()
        {
            try
            {
                var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Physical);
                if (target == null)
                {
                    return;
                }

                if (!QState && LastQ + 200 < Environment.TickCount && ParamBool("ElLeeSin.Harass.Q1") && !QState
                    && spells[Spells.Q].IsReady() && target.HasQBuff()
                    && (LastQ + 2700 < Environment.TickCount || spells[Spells.Q].GetDamage(target, 1) > target.Health
                        || target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player) + 50))
                {
                    spells[Spells.Q].Cast();
                }

                if (ParamBool("ElLeeSin.Combo.AAStacks")
                    && PassiveStacks > InitMenu.Menu.Item("ElLeeSin.Harass.PassiveStacks").GetValue<Slider>().Value
                    && Orbwalking.GetRealAutoAttackRange(Player) > Player.Distance(target))
                {
                    return;
                }

                if (spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Harass.Q1") && LastQ + 200 < Environment.TickCount)
                {
                    if (QState && target.Distance(Player) < spells[Spells.Q].Range)
                    {
                        CastQ(target, ParamBool("ElLeeSin.Smite.Q"));
                    }
                }

                if (spells[Spells.E].IsReady() && ParamBool("ElLeeSin.Harass.E1") && LastE + 200 < Environment.TickCount)
                {
                    if (EState && target.Distance(Player) < spells[Spells.E].Range)
                    {
                        spells[Spells.E].Cast();
                        return;
                    }

                    if (!EState && target.Distance(Player) > Orbwalking.GetRealAutoAttackRange(Player) + 50)
                    {
                        spells[Spells.E].Cast();
                    }
                }

                if (ParamBool("ElLeeSin.Harass.Wardjump") && Player.Distance(target) < 50 && !(target.HasQBuff())
                    && (EState || !spells[Spells.E].IsReady() && ParamBool("ElLeeSin.Harass.E1"))
                    && (QState || !spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Harass.Q1")))
                {
                    var min =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(a => a.IsAlly && a.Distance(Player) <= spells[Spells.W].Range)
                            .OrderByDescending(a => a.Distance(target))
                            .FirstOrDefault();

                    spells[Spells.W].CastOnUnit(min);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static bool HasAnyBuffs(this Obj_AI_Base unit, string s)
        {
            return
                unit.Buffs.Any(
                    a => a.Name.ToLower().Contains(s.ToLower()) || a.DisplayName.ToLower().Contains(s.ToLower()));
        }

        /// <summary>
        ///     Checks if the target has the E buff
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private static bool HasEBuff(Obj_AI_Base target) => target.HasBuff("BlindMonkTempest");

        private static void InsecCombo(AIHeroClient target)
        {
            if (target == null)
            {
                return;
            }

            if (Player.Distance(GetInsecPos(target)) < 200)
            {
                insecComboStep = InsecComboStepSelect.Pressr;
            }
            else if (insecComboStep == InsecComboStepSelect.None && GetInsecPos(target).Distance(Player.Position) < 600)
            {
                insecComboStep = InsecComboStepSelect.Wgapclose;
            }
            else if (insecComboStep == InsecComboStepSelect.None && target.Distance(Player) < spells[Spells.Q].Range)
            {
                insecComboStep = InsecComboStepSelect.Qgapclose;
            }

            switch (insecComboStep)
            {
                case InsecComboStepSelect.Qgapclose:
                    if (QState)
                    {
                        var pred1 = spells[Spells.Q].GetPrediction(target);
                        if (pred1.Hitchance >= HitChance.High)
                        {
                            if (pred1.CollisionObjects.Count == 0)
                            {
                                if (spells[Spells.Q].Cast(pred1.CastPosition))
                                {
                                    return;
                                }
                            }
                            else
                            {
                                CastQ(target, ParamBool("ElLeeSin.Smite.Q"));
                            }
                        }

                        if (!ParamBool("checkOthers1"))
                        {
                            return;
                        }

                        var insectObjects =
                            HeroManager.Enemies.Where(
                                i =>
                                i.IsValidTarget(spells[Spells.Q].Range)
                                && spells[Spells.Q].GetHealthPrediction(i) > spells[Spells.Q].GetDamage(i)
                                && i.Distance(target) < target.Distance(Player) && i.Distance(target) < 550)
                                .Concat(MinionManager.GetMinions(Player.ServerPosition, spells[Spells.Q].Range))
                                .Where(
                                    m =>
                                    m.IsValidTarget(spells[Spells.Q].Range)
                                    && spells[Spells.Q].GetHealthPrediction(m) > spells[Spells.Q].GetDamage(m)
                                    && m.Distance(target) < 400f)
                                .OrderBy(i => i.Distance(target))
                                .ThenByDescending(i => i.Health)
                                .ToList();

                        if (insectObjects.Count == 0)
                        {
                            return;
                        }

                        insectObjects.ForEach(i => spells[Spells.Q].Cast(i));
                    }

                    if (!(target.HasQBuff()) && QState)
                    {
                        CastQ(target, ParamBool("ElLeeSin.Smite.Q"));
                    }
                    else if (target.HasQBuff())
                    {
                        spells[Spells.Q].Cast();
                        insecComboStep = InsecComboStepSelect.Wgapclose;
                    }
                    else
                    {
                        if (spells[Spells.Q].Instance.Name.Equals(
                            "blindmonkqtwo",
                            StringComparison.InvariantCultureIgnoreCase) && ReturnQBuff()?.Distance(target) <= 600)
                        {
                            spells[Spells.Q].Cast();
                        }
                    }
                    break;

                case InsecComboStepSelect.Wgapclose:

                    if (Player.Distance(target) < WardRange)
                    {
                        WardJump(GetInsecPos(target), false, true, true);

                        if (FindBestWardItem() == null && spells[Spells.R].IsReady()
                            && ParamBool("ElLeeSin.Flash.Insec")
                            && Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready)
                        {
                            if ((GetInsecPos(target).Distance(Player.Position) < FlashRange
                                 && LastWard + 1000 < Environment.TickCount) || !spells[Spells.W].IsReady())
                            {
                                Player.Spellbook.CastSpell(flashSlot, GetInsecPos(target));
                            }
                        }
                    }
                    else if (Player.Distance(target) < WardFlashRange)
                    {
                        WardJump(target.Position);

                        if (spells[Spells.R].IsReady() && ParamBool("ElLeeSin.Flash.Insec")
                            && Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready)
                        {
                            if (Player.Distance(target) < FlashRange - 25)
                            {
                                if (FindBestWardItem() == null || LastWard + 1000 < Environment.TickCount)
                                {
                                    Player.Spellbook.CastSpell(flashSlot, GetInsecPos(target));
                                }
                            }
                        }
                    }
                    break;

                case InsecComboStepSelect.Pressr:
                    spells[Spells.R].CastOnUnit(target);
                    break;
            }
        }

        private static Vector3 InterceptionPoint(List<AIHeroClient> heroes)
        {
            var result = new Vector3();
            foreach (var hero in heroes)
            {
                result += hero.Position;
            }
            result.X /= heroes.Count;
            result.Y /= heroes.Count;
            return result;
        }

        private static void JungleClear()
        {
            try
            {
                var minion =
                    MinionManager.GetMinions(
                        spells[Spells.Q].Range,
                        MinionTypes.All,
                        MinionTeam.Neutral,
                        MinionOrderTypes.MaxHealth).FirstOrDefault();

                if (minion == null)
                {
                    return;
                }

                if (PassiveStacks > 0 || LastSpell + 400 > Environment.TickCount)
                {
                    return;
                }

                if (spells[Spells.W].IsReady() && ParamBool("ElLeeSin.Jungle.W"))
                {
                    if (Environment.TickCount - LastE <= 250)
                    {
                        return;
                    }

                    if (WState && minion.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                    {
                        spells[Spells.W].Cast();
                        LastW = Environment.TickCount;
                    }

                    if (!WState && LastW + 1000 < Environment.TickCount)
                    {
                        spells[Spells.W].Cast();
                    }
                }

                if (spells[Spells.E].IsReady() && ParamBool("ElLeeSin.Jungle.E"))
                {
                    if (EState && spells[Spells.E].IsInRange(minion))
                    {
                        spells[Spells.E].Cast();
                        LastSpell = Environment.TickCount;
                        return;
                    }

                    if (!EState && spells[Spells.E].IsInRange(minion) && LastE + 400 < Environment.TickCount)
                    {
                        spells[Spells.E].Cast();
                        LastSpell = Environment.TickCount;
                    }
                }

                if (spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Jungle.Q"))
                {
                    if (QState && minion.Distance(Player) < spells[Spells.Q].Range
                        && LastQ + 200 < Environment.TickCount)
                    {
                        spells[Spells.Q].Cast(minion);
                        LastSpell = Environment.TickCount;
                        return;
                    }

                    spells[Spells.Q].Cast();
                    LastSpell = Environment.TickCount;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static void Main(string[] args)
        {
            EloBuddy.SDK.Events.Loading.OnLoadingComplete += Game_OnGameLoad;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            try
            {
                if (!sender.IsMe)
                {
                    return;
                }

                switch (args.Slot)
                {
                    case SpellSlot.Q:
                        if (!args.SData.Name.ToLower().Contains("one"))
                        {
                            isInQ2 = true;
                        }
                        break;
                }

                if (SpellNames.Contains(args.SData.Name.ToLower()))
                {
                    PassiveStacks = 2;
                    passiveTimer = Environment.TickCount + 3000;
                }

                if (args.SData.Name.Equals("BlindMonkQOne", StringComparison.InvariantCultureIgnoreCase))
                {
                    castQAgain = false;
                    Utility.DelayAction.Add(2900, () => { castQAgain = true; });
                }

                if (spells[Spells.R].IsReady() && Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready)
                {
                    var target = ParamBool("insecMode")
                                     ? TargetSelector.GetSelectedTarget()
                                     : TargetSelector.GetTarget(
                                         spells[Spells.R].Range,
                                         TargetSelector.DamageType.Physical);

                    if (target == null)
                    {
                        return;
                    }
                    if (args.SData.Name.Equals("BlindMonkRKick", StringComparison.InvariantCultureIgnoreCase)
                        && InitMenu.Menu.Item("ElLeeSin.Insec.UseInstaFlash").GetValue<KeyBind>().Active)
                    {
                        Utility.DelayAction.Add(80, () => Player.Spellbook.CastSpell(flashSlot, GetInsecPos(target)));
                    }
                }

                if (args.SData.Name.Equals("summonerflash", StringComparison.InvariantCultureIgnoreCase)
                    && insecComboStep != InsecComboStepSelect.None)
                {
                    var target = ParamBool("insecMode")
                                     ? TargetSelector.GetSelectedTarget()
                                     : TargetSelector.GetTarget(
                                         spells[Spells.Q].Range,
                                         TargetSelector.DamageType.Physical);

                    insecComboStep = InsecComboStepSelect.Pressr;

                    Utility.DelayAction.Add(80, () => spells[Spells.R].CastOnUnit(target));
                }
                if (args.SData.Name.Equals("BlindMonkQTwo", StringComparison.InvariantCultureIgnoreCase))
                {
                    waitingForQ2 = true;
                    Utility.DelayAction.Add(3000, () => { waitingForQ2 = false; });
                }
                if (args.SData.Name.Equals("BlindMonkRKick", StringComparison.InvariantCultureIgnoreCase))
                {
                    insecComboStep = InsecComboStepSelect.None;
                }

                switch (args.SData.Name.ToLower())
                {
                    case "blindmonkqone":
                        LastQ = Environment.TickCount;
                        LastSpell = Environment.TickCount;
                        PassiveStacks = 2;
                        break;
                    case "blindmonkwone":
                        LastW = Environment.TickCount;
                        LastSpell = Environment.TickCount;
                        PassiveStacks = 2;
                        break;
                    case "blindmonkeone":
                        LastE = Environment.TickCount;
                        LastSpell = Environment.TickCount;
                        PassiveStacks = 2;
                        break;
                    case "blindmonkqtwo":
                        LastQ2 = Environment.TickCount;
                        LastSpell = Environment.TickCount;
                        PassiveStacks = 2;
                        CheckQ = false;
                        break;
                    case "blindmonkwtwo":
                        LastW2 = Environment.TickCount;
                        LastSpell = Environment.TickCount;
                        PassiveStacks = 2;
                        break;
                    case "blindmonketwo":
                        LastQ = Environment.TickCount;
                        LastSpell = Environment.TickCount;
                        PassiveStacks = 2;
                        break;
                    case "blindmonkrkick":
                        LastR = Environment.TickCount;
                        LastSpell = Environment.TickCount;
                        PassiveStacks = 2;
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static void OnBuffAdd(Obj_AI_Base sender, Obj_AI_BaseBuffGainEventArgs args)
        {
            if (sender.IsMe)
            {
                switch (args.Buff.DisplayName)
                {
                    case "BlindMonkQTwoDash":
                        isInQ2 = true;
                        break;
                }
            }
        }

        private static void OnBuffRemove(Obj_AI_Base sender, Obj_AI_BaseBuffLoseEventArgs args)
        {
            if (sender.IsMe)
            {
                switch (args.Buff.DisplayName)
                {
                    case "BlindMonkQTwoDash":
                        isInQ2 = false;
                        break;
                }
            }
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            if (Environment.TickCount < lastPlaced + 300 && spells[Spells.W].IsReady() && sender.Name.ToLower().Contains("ward"))
            {
                var ward = (Obj_AI_Base)sender;
                if (ward.Distance(lastWardPos) < 500)
                {
                    spells[Spells.W].Cast(ward);
                }
            }
        }

        private static void OrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            try
            {
                if (unit.IsMe && PassiveStacks > 0)
                {
                    PassiveStacks--;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static Obj_AI_Base ReturnQBuff()
        {
            try
            {
                return
                    ObjectManager.Get<Obj_AI_Base>()
                        .Where(a => a.IsValidTarget(1300))
                        .FirstOrDefault(unit => unit.HasQBuff());
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
            return null;
        }

        private static string SmiteSpellName()
        {
            if (SmiteBlue.Any(a => Items.HasItem(a)))
            {
                return "s5_summonersmiteplayerganker";
            }

            if (SmiteRed.Any(a => Items.HasItem(a)))
            {
                return "s5_summonersmiteduel";
            }

            return "summonersmite";
        }

        private static bool UseItems(Obj_AI_Base target)
        {
            if (Player.IsDashing() || Player.Spellbook.IsAutoAttacking)
            {
                return false;
            }

            var youmuus = Youmuu;
            if (Youmuu.IsReady() && youmuus.Cast())
            {
                return true;
            }

            var heroes = Player.GetEnemiesInRange(385).Count;
            var count = heroes;

            var tiamat = Tiamat;
            if (tiamat.IsReady() && count > 0 && tiamat.Cast())
            {
                return true;
            }

            var hydra = Hydra;
            if (Hydra.IsReady() && count > 0 && hydra.Cast())
            {
                return true;
            }

            var titanic = Titanic;
            return titanic.IsReady() && count > 0 && titanic.Cast();
        }

        private static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return from.To2D() + distance * Vector3.Normalize(direction - from).To2D();
        }

        private static void WardCombo()
        {
            try
            {
                var target = TargetSelector.GetTarget(
                    spells[Spells.W].Range + spells[Spells.R].Range,
                    TargetSelector.DamageType.Physical);

                Orbwalking.Orbwalk(
                    target ?? null,
                    Game.CursorPos,
                    InitMenu.Menu.Item("ExtraWindup").GetValue<Slider>().Value,
                    InitMenu.Menu.Item("HoldPosRadius").GetValue<Slider>().Value);

                if (target == null)
                {
                    return;
                }

                CastItems(target);

                if (spells[Spells.Q].IsReady())
                {
                    if (QState)
                    {
                        if (spells[Spells.R].IsReady() && spells[Spells.Q].Cast(target).IsCasted())
                        {
                            return;
                        }
                    }
                    else if (target.HasQBuff()
                             && (spells[Spells.Q].IsKillable(target, 1)
                                 || (!spells[Spells.R].IsReady()
                                     && Utils.TickCount - spells[Spells.R].LastCastAttemptT > 300
                                     && Utils.TickCount - spells[Spells.R].LastCastAttemptT < 1500
                                     && spells[Spells.Q].Cast())))
                    {
                        return;
                    }
                }

                if (spells[Spells.E].IsReady())
                {
                    if (EState)
                    {
                        if (spells[Spells.E].IsInRange(target) && target.HasQBuff() && !spells[Spells.R].IsReady()
                            && Utils.TickCount - spells[Spells.R].LastCastAttemptT < 1500 && Player.Mana >= 80
                            && spells[Spells.E].Cast())
                        {
                            return;
                        }
                    }
                }

                if (!spells[Spells.Q].IsReady() || !spells[Spells.R].IsReady() || QState || !target.HasQBuff())
                {
                    return;
                }

                if (spells[Spells.R].IsInRange(target))
                {
                    spells[Spells.R].CastOnUnit(target);
                }
                else if (spells[Spells.W].IsReady())
                {
                    if (target.Distance(Player) > spells[Spells.R].Range
                        && target.Distance(Player) < spells[Spells.R].Range + 580 && target.HasQBuff())
                    {
                        WardJump(target.Position, false);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }
        }

        private static void WardJump(
            Vector3 pos,
            bool m2M = true,
            bool maxRange = false,
            bool reqinMaxRange = false,
            bool minions = true,
            bool champions = true)
        {
            if (WStage != WCastStage.First)
            {
                return;
            }

            var basePos = Player.Position.To2D();
            var newPos = (pos.To2D() - Player.Position.To2D());

            if (JumpPos == new Vector2())
            {
                if (reqinMaxRange)
                {
                    JumpPos = pos.To2D();
                }
                else if (maxRange || Player.Distance(pos) > 590)
                {
                    JumpPos = basePos + (newPos.Normalized() * (590));
                }
                else
                {
                    JumpPos = basePos + (newPos.Normalized() * (Player.Distance(pos)));
                }
            }
            if (JumpPos != new Vector2() && reCheckWard)
            {
                reCheckWard = false;
                Utility.DelayAction.Add(
                    20,
                    () =>
                        {
                            if (JumpPos != new Vector2())
                            {
                                JumpPos = new Vector2();
                                reCheckWard = true;
                            }
                        });
            }
            if (m2M)
            {
                Orbwalk(pos);
            }
            if (!spells[Spells.W].IsReady() || WStage != WCastStage.First
                || reqinMaxRange && Player.Distance(pos) > spells[Spells.W].Range)
            {
                return;
            }

            if (minions || champions)
            {
                if (champions)
                {
                    var wardJumpableChampion =
                        ObjectManager.Get<AIHeroClient>()
                            .Where(
                                x =>
                                x.IsAlly && x.Distance(Player) < spells[Spells.W].Range && x.Distance(pos) < 200
                                && !x.IsMe)
                            .OrderByDescending(i => i.Distance(Player))
                            .ToList()
                            .FirstOrDefault();

                    if (wardJumpableChampion != null && WStage == WCastStage.First)
                    {
                        if (500 >= Utils.TickCount - Wcasttime || WStage != WCastStage.First)
                        {
                            return;
                        }

                        CastW(wardJumpableChampion);
                        return;
                    }
                }
                if (minions)
                {
                    var wardJumpableMinion =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                m =>
                                m.IsAlly && m.Distance(Player) < spells[Spells.W].Range && m.Distance(pos) < 200
                                && !m.Name.ToLower().Contains("ward"))
                            .OrderByDescending(i => i.Distance(Player))
                            .ToList()
                            .FirstOrDefault();

                    if (wardJumpableMinion != null && WStage == WCastStage.First)
                    {
                        if (500 >= Utils.TickCount - Wcasttime || WStage != WCastStage.First)
                        {
                            return;
                        }

                        CastW(wardJumpableMinion);
                        return;
                    }
                }
            }

            var isWard = false;

            var wardObject =
                ObjectManager.Get<Obj_AI_Base>()
                    .Where(o => o.IsAlly && o.Name.ToLower().Contains("ward") && o.Distance(JumpPos) < 200)
                    .ToList()
                    .FirstOrDefault();

            if (wardObject != null)
            {
                isWard = true;
                if (500 >= Utils.TickCount - Wcasttime || WStage != WCastStage.First)
                {
                    return;
                }

                CastW(wardObject);
            }

            if (!isWard && castWardAgain)
            {
                var ward = FindBestWardItem();
                if (ward == null)
                {
                    return;
                }

                if (spells[Spells.W].IsReady() && WState && LastWard + 400 < Utils.TickCount)
                {
                    Player.Spellbook.CastSpell(ward.SpellSlot, JumpPos.To3D());
                    lastWardPos = JumpPos.To3D();
                    LastWard = Utils.TickCount;
                }
            }
        }

        private static void WardjumpToMouse()
        {
            WardJump(
                Game.CursorPos,
                ParamBool("ElLeeSin.Wardjump.Mouse"),
                false,
                false,
                ParamBool("ElLeeSin.Wardjump.Minions"),
                ParamBool("ElLeeSin.Wardjump.Champions"));
        }

        #endregion
    }
}