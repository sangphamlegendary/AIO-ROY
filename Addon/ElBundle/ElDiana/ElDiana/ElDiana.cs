namespace ElDiana
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using EloBuddy;
    using LeagueSharp.Common;

    internal enum Spells
    {
        Q,

        W,

        E,

        R
    }

    internal static class Diana
    {
        #region Static Fields

        public static Orbwalking.Orbwalker Orbwalker;

        public static Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>
                                                             {
                                                                     { Spells.Q, new Spell(SpellSlot.Q, 830) },
                                                                     { Spells.W, new Spell(SpellSlot.W, 250) },
                                                                     { Spells.E, new Spell(SpellSlot.E, 450) },
                                                                     { Spells.R, new Spell(SpellSlot.R, 825) }
                                                             };

        private static SpellSlot ignite;

        #endregion

        #region Public Properties

        public static string ScriptVersion => typeof(Diana).Assembly.GetName().Version.ToString();

        #endregion

        #region Properties

        private static HitChance CustomHitChance => GetHitchance();

        private static AIHeroClient Player => ObjectManager.Player;

        #endregion

        #region Public Methods and Operators

        public static float GetComboDamage(Obj_AI_Base enemy)
        {
            float damage = 0;

            if (spells[Spells.Q].IsReady())
            {
                damage += spells[Spells.Q].GetDamage(enemy);
            }

            if (spells[Spells.W].IsReady())
            {
                damage += spells[Spells.W].GetDamage(enemy);
            }

            if (spells[Spells.E].IsReady())
            {
                damage += spells[Spells.E].GetDamage(enemy);
            }

            if (spells[Spells.R].IsReady())
            {
                damage += spells[Spells.R].GetDamage(enemy);
            }

            if (ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(ignite) != SpellState.Ready)
            {
                damage += (float)Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);
            }

            return damage;
        }

        public static void OnLoad(EventArgs args)
        {
            if (ObjectManager.Player.CharData.BaseSkinName != "Diana")
            {
                return;
            }

            spells[Spells.Q].SetSkillshot(0.25f, 185f, 1620f, false, SkillshotType.SkillshotCircle);
            ignite = Player.GetSpellSlot("summonerdot");

            ElDianaMenu.Initialize();
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += Drawings.Drawing_OnDraw;

            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += (source, eventArgs) =>
                {
                    var eSlot = spells[Spells.E];
                    if (ElDianaMenu.Menu.Item("ElDiana.Interrupt.UseEInterrupt").GetValue<bool>() && eSlot.IsReady()
                        && eSlot.Range >= Player.Distance(source))
                    {
                        eSlot.Cast();
                    }
                };

            CustomEvents.Unit.OnDash += (source, eventArgs) =>
                {
                    if (!source.IsEnemy)
                    {
                        return;
                    }

                    var eSlot = spells[Spells.E];
                    var dis = Player.Distance(source);
                    if (!eventArgs.IsBlink && ElDianaMenu.Menu.Item("ElDiana.Interrupt.UseEDashes").GetValue<bool>()
                        && eSlot.IsReady() && eSlot.Range >= dis)
                    {
                        eSlot.Cast();
                    }
                };
        }

        #endregion

        #region Methods

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!gapcloser.Sender.IsValidTarget(spells[Spells.E].Range))
            {
                return;
            }

            if (gapcloser.Sender.IsValidTarget(spells[Spells.E].Range))
            {
                if (IsActive("ElDiana.Interrupt.G") && spells[Spells.E].IsReady())
                {
                    spells[Spells.E].Cast(gapcloser.Sender);
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);
            if (target == null)
            {
                return;
            }

            if (IsActive("ElDiana.Combo.Q") && spells[Spells.Q].IsReady())
            {
                if (Player.Distance(target) <= spells[Spells.Q].Range)
                {
                    var prediction = spells[Spells.Q].GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.VeryHigh)
                    {
                        spells[Spells.Q].Cast(prediction.CastPosition);
                    }
                }
            }

            if (IsActive("ElDiana.Combo.QR"))
            {
                var killableTarget =
                    HeroManager.Enemies.FirstOrDefault(
                        x => spells[Spells.R].IsKillable(x) && x.Distance(Player) <= spells[Spells.R].Range * 2);

                if (killableTarget != null)
                {
                    GapCloser(killableTarget);
                }
            }

            if (IsActive("ElDiana.Combo.R") && spells[Spells.R].IsReady())
            {
                if (Player.Distance(target) <= spells[Spells.R].Range)
                {
                    if (HasQBuff(target)
                        && (!target.UnderTurret(true)
                            || (ElDianaMenu.Menu.Item("ElDiana.Combo.R.PreventUnderTower").GetValue<Slider>().Value
                                <= Player.HealthPercent)))
                    {
                        spells[Spells.R].Cast(target);
                    }
                }
            }

            if (IsActive("ElDiana.Combo.W") && spells[Spells.W].IsReady())
            {
                if (Player.IsDashing() || !target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)))
                {
                    return;
                }

                spells[Spells.W].Cast();
            }

            if (IsActive("ElDiana.Combo.E") && spells[Spells.E].IsReady())
            {
                if (Player.IsDashing() || Player.Distance(target) > spells[Spells.E].Range)
                {
                    return;
                }

                spells[Spells.E].Cast();
            }

            if (IsActive("ElDiana.Combo.Secure")
                && (!target.UnderTurret(true)
                    || (ElDianaMenu.Menu.Item("ElDiana.Combo.R.PreventUnderTower").GetValue<Slider>().Value
                        <= Player.HealthPercent)))
            {
                var closeEnemies = Player.GetEnemiesInRange(spells[Spells.R].Range * 2).Count;

                if (closeEnemies <= ElDianaMenu.Menu.Item("ElDiana.Combo.UseSecondRLimitation").GetValue<Slider>().Value
                    && IsActive("ElDiana.Combo.R") && !spells[Spells.Q].IsReady() && spells[Spells.R].IsReady())
                {
                    if (target.Health < spells[Spells.R].GetDamage(target)
                        && (!target.UnderTurret(true)
                            || (ElDianaMenu.Menu.Item("ElDiana.Combo.R.PreventUnderTower").GetValue<Slider>().Value
                                <= Player.HealthPercent)))
                    {
                        spells[Spells.R].Cast(target);
                    }
                }

                if (closeEnemies <= ElDianaMenu.Menu.Item("ElDiana.Combo.UseSecondRLimitation").GetValue<Slider>().Value
                    && spells[Spells.R].IsReady())
                {
                    if (target.Health < spells[Spells.R].GetDamage(target))
                    {
                        spells[Spells.R].Cast(target);
                    }
                }
            }

            if (Player.Distance(target) <= 600 && IgniteDamage(target) >= target.Health
                && IsActive("ElDiana.Combo.Ignite"))
            {
                Player.Spellbook.CastSpell(ignite, target);
            }
        }

        private static void GapCloser(Obj_AI_Base target)
        {
            if (target == null || !spells[Spells.R].IsInRange(target))
            {
                return;
            }

            if (spells[Spells.Q].IsReady() && spells[Spells.R].IsReady())
            {
                var closeMinion =
                    MinionManager.GetMinions(Player.ServerPosition, spells[Spells.R].Range)
                        .OrderBy(x => x.Distance(target))
                        .FirstOrDefault(x => !spells[Spells.Q].IsKillable(x));

                if (closeMinion != null)
                {
                    spells[Spells.Q].Cast(closeMinion);
                    if (HasQBuff(closeMinion))
                    {
                        spells[Spells.R].Cast(closeMinion);
                    }
                }
            }
        }

        private static HitChance GetHitchance()
        {
            switch (ElDianaMenu.Menu.Item("ElDiana.hitChance").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    return HitChance.Low;
                case 1:
                    return HitChance.Medium;
                case 2:
                    return HitChance.High;
                case 3:
                    return HitChance.VeryHigh;
                default:
                    return HitChance.VeryHigh;
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Physical);
            if (target == null)
            {
                return;
            }

            if (Player.ManaPercent <= ElDianaMenu.Menu.Item("ElDiana.Harass.Mana").GetValue<Slider>().Value)
            {
                return;
            }

            if (IsActive("ElDiana.Harass.Q") && spells[Spells.Q].IsReady() && spells[Spells.Q].IsInRange(target))
            {
                var pred = spells[Spells.Q].GetPrediction(target);
                if (pred.Hitchance >= CustomHitChance)
                {
                    spells[Spells.Q].Cast(target);
                }
            }

            if (IsActive("ElDiana.Harass.W") && spells[Spells.W].IsReady() && spells[Spells.W].IsInRange(target))
            {
                spells[Spells.W].Cast();
            }

            if (IsActive("ElDiana.Harass.E") && spells[Spells.E].IsReady()
                && Player.Distance(target) <= spells[Spells.E].Range)
            {
                spells[Spells.E].Cast();
            }
        }

        private static bool HasQBuff(Obj_AI_Base target)
        {
            return target.HasBuff("dianamoonlight");
        }

        private static float IgniteDamage(AIHeroClient target)
        {
            if (ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(ignite) != SpellState.Ready)
            {
                return 0f;
            }
            return (float)Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }

        private static bool IsActive(string menuItem)
        {
            return ElDianaMenu.Menu.Item(menuItem).IsActive();
        }

        private static void JungleClear()
        {
            var minions = MinionManager.GetMinions(
                ObjectManager.Player.ServerPosition,
                spells[Spells.Q].Range,
                MinionTypes.All,
                MinionTeam.Neutral,
                MinionOrderTypes.MaxHealth);

            var qMinions = minions.FindAll(minion => minion.IsValidTarget(spells[Spells.Q].Range));
            var qMinion = qMinions.FirstOrDefault();

            if (qMinion == null)
            {
                return;
            }

            if (IsActive("ElDiana.JungleClear.Q") && spells[Spells.Q].IsReady())
            {
                if (qMinion.IsValidTarget())
                {
                    spells[Spells.Q].Cast(qMinion);
                }
            }

            if (IsActive("ElDiana.JungleClear.W") && spells[Spells.W].IsReady())
            {
                spells[Spells.W].Cast();
            }

            if (IsActive("ElDiana.JungleClear.E") && spells[Spells.E].IsReady()
                && qMinions.Count(m => Player.Distance(m) < spells[Spells.W].Range) < 1)
            {
                spells[Spells.E].Cast();
            }

            if (IsActive("ElDiana.JungleClear.R") && spells[Spells.R].IsReady())
            {
                var moonlightMob =
                    minions.FindAll(minion => HasQBuff(minion)).OrderBy(minion => minion.HealthPercent);
                if (moonlightMob.Any())
                {
                    var canBeKilled = moonlightMob.Find(minion => minion.Health < spells[Spells.R].GetDamage(minion));
                    if (canBeKilled.IsValidTarget())
                    {
                        spells[Spells.R].Cast(canBeKilled);
                    }
                }
            }
        }

        private static void LaneClear()
        {
            var minion =
                MinionManager.GetMinions(ObjectManager.Player.ServerPosition, spells[Spells.Q].Range).FirstOrDefault();
            if (minion == null || minion.Name.ToLower().Contains("ward"))
            {
                return;
            }

            var countQ = ElDianaMenu.Menu.Item("ElDiana.LaneClear.Count.Minions.Q").GetValue<Slider>().Value;
            var countW = ElDianaMenu.Menu.Item("ElDiana.LaneClear.Count.Minions.W").GetValue<Slider>().Value;
            var countE = ElDianaMenu.Menu.Item("ElDiana.LaneClear.Count.Minions.E").GetValue<Slider>().Value;

            var minions = MinionManager.GetMinions(
                ObjectManager.Player.ServerPosition,
                spells[Spells.Q].Range,
                MinionTypes.All,
                MinionTeam.NotAlly);

            var qMinions = minions.FindAll(minionQ => minion.IsValidTarget(spells[Spells.Q].Range));
            var qMinion = qMinions.Find(minionQ => minionQ.IsValidTarget());

            if (IsActive("ElDiana.LaneClear.Q") && spells[Spells.Q].IsReady()
                && spells[Spells.Q].GetCircularFarmLocation(minions).MinionsHit >= countQ)
            {
                spells[Spells.Q].Cast(qMinion);
            }

            if (IsActive("ElDiana.LaneClear.W") && spells[Spells.W].IsReady()
                && spells[Spells.W].GetCircularFarmLocation(minions).MinionsHit >= countW)
            {
                spells[Spells.W].Cast();
            }

            if (IsActive("ElDiana.LaneClear.E") && spells[Spells.E].IsReady() && Player.Distance(qMinion, false) < 200
                && spells[Spells.E].GetCircularFarmLocation(minions).MinionsHit >= countE)
            {
                spells[Spells.E].Cast();
            }

            var minionsR = MinionManager.GetMinions(
                ObjectManager.Player.ServerPosition,
                spells[Spells.Q].Range,
                MinionTypes.All,
                MinionTeam.NotAlly,
                MinionOrderTypes.MaxHealth);

            if (IsActive("ElDiana.LaneClear.R") && spells[Spells.R].IsReady())
            {
                var moonlightMob = minionsR.FindAll(x => HasQBuff(x)).OrderBy(x => minion.HealthPercent);
                if (moonlightMob.Any())
                {
                    var canBeKilled = moonlightMob.Find(x => minion.Health < spells[Spells.R].GetDamage(minion));
                    if (canBeKilled.IsValidTarget())
                    {
                        spells[Spells.R].Cast(canBeKilled);
                    }
                }
            }
        }

        private static void Lasthit()
        {
            var qKillableMinion =
                MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition,
                    spells[Spells.Q].Range,
                    MinionTypes.All,
                    MinionTeam.NotAlly,
                    MinionOrderTypes.MaxHealth).FirstOrDefault(x => spells[Spells.Q].IsKillable(x));

            if (qKillableMinion == null
                || Player.ManaPercent <= ElDianaMenu.Menu.Item("ElDiana.LastHit.Mana").GetValue<Slider>().Value)
            {
                return;
            }

            spells[Spells.Q].Cast(qKillableMinion);
        }

        private static void MisayaCombo()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range, TargetSelector.DamageType.Magical);
            if (target == null)
            {
                return;
            }

            var minHpToDive = ElDianaMenu.Menu.Item("ElDiana.Combo.R.PreventUnderTower").GetValue<Slider>().Value;

            var useR = ElDianaMenu.Menu.Item("ElDiana.Combo.R").GetValue<bool>()
                       && (!target.UnderTurret(true) || (minHpToDive <= Player.HealthPercent));
            var useIgnite = ElDianaMenu.Menu.Item("ElDiana.Combo.Ignite").GetValue<bool>();
            var secondR = ElDianaMenu.Menu.Item("ElDiana.Combo.Secure").GetValue<bool>()
                          && (!target.UnderTurret(true) || (minHpToDive <= Player.HealthPercent));
            var distToTarget = Player.Distance(target, false);
            var misayaMinRange = ElDianaMenu.Menu.Item("ElDiana.Combo.R.MisayaMinRange").GetValue<Slider>().Value;
            var useSecondRLimitation =
                ElDianaMenu.Menu.Item("ElDiana.Combo.UseSecondRLimitation").GetValue<Slider>().Value;

            if (useR && spells[Spells.R].IsReady() && distToTarget > spells[Spells.R].Range)
            {
                return;
            }

            if (IsActive("ElDiana.Combo.Q") && useR && spells[Spells.Q].IsReady() && spells[Spells.R].IsReady()
                && distToTarget >= misayaMinRange)
            {
                spells[Spells.R].Cast(target);
                spells[Spells.Q].Cast(target);
            }

            if (IsActive("ElDiana.Combo.Q") && spells[Spells.Q].IsReady()
                && target.IsValidTarget(spells[Spells.Q].Range))
            {
                var pred = spells[Spells.Q].GetPrediction(target);
                if (pred.Hitchance >= HitChance.VeryHigh)
                {
                    spells[Spells.Q].Cast(pred.CastPosition);
                }
            }

            if (useR && spells[Spells.R].IsReady() && target.IsValidTarget(spells[Spells.R].Range)
                && HasQBuff(target))
            {
                spells[Spells.R].Cast(target);
            }

            if (IsActive("ElDiana.Combo.W") && spells[Spells.W].IsReady()
                && target.IsValidTarget(Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)))
            {
                spells[Spells.W].Cast();
            }

            if (IsActive("ElDiana.Combo.E") && spells[Spells.E].IsReady() && target.IsValidTarget(400f))
            {
                spells[Spells.E].Cast();
            }

            if (secondR)
            {
                var closeEnemies = Player.GetEnemiesInRange(spells[Spells.R].Range * 2).Count;

                if (closeEnemies <= useSecondRLimitation && useR && !spells[Spells.Q].IsReady()
                    && spells[Spells.R].IsReady())
                {
                    if (target.Health < spells[Spells.R].GetDamage(target))
                    {
                        spells[Spells.R].Cast(target);
                    }
                }

                if (closeEnemies <= useSecondRLimitation && spells[Spells.R].IsReady())
                {
                    if (target.Health < spells[Spells.R].GetDamage(target))
                    {
                        spells[Spells.R].Cast(target);
                    }
                }
            }

            if (Player.Distance(target) <= 600 && IgniteDamage(target) >= target.Health && useIgnite)
            {
                Player.Spellbook.CastSpell(ignite, target);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    var ultType = ElDianaMenu.Menu.Item("ElDiana.Combo.R.Mode").GetValue<StringList>().SelectedIndex;
                    if (ElDianaMenu.Menu.Item("ElDiana.Hotkey.ToggleComboMode").GetValue<KeyBind>().Active)
                    {
                        ultType = (ultType + 1) % 2;
                    }
                    switch (ultType)
                    {
                        case 0:
                            Combo();
                            break;

                        case 1:
                            MisayaCombo();
                            break;
                    }
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    Lasthit();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
            }
        }

        #endregion
    }
}