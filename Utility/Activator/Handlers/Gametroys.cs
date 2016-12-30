#region Copyright © 2015 Kurisu Solutions
// All rights are reserved. Transmission or reproduction in part or whole,
// any form or by any means, mechanical, electronical or otherwise, is prohibited
// without the prior written consent of the copyright owner.
// 
// Document:	Handlers/ObjectHandler.cs
// Date:		28/07/2016
// Author:		Robin Kurisu
#endregion

using System;
using System.Linq;
using Activator.Base;
using Activator.Data;
using EloBuddy;
using LeagueSharp.Common;

namespace Activator.Handlers
{
    public class Gametroys
    {
        public static void StartOnUpdate()
        {
            Game.OnUpdate += Game_OnUpdate;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
        }

        static void GameObject_OnDelete(GameObject obj, EventArgs args)
        {
            if (obj.IsValid<MissileClient>())
                return;

            foreach (var troy in Gametroy.Troys)
            {
                if (troy.Included && obj.Name.Contains(troy.Name))
                {
                    troy.Obj = null;
                    troy.Start = 0;
                    troy.Limiter = 0; // reset limiter
                    troy.Included = false;
                }
            }
        }

        static void GameObject_OnCreate(GameObject obj, EventArgs args)
        {
            if (obj.IsValid<MissileClient>())
                return;

            foreach (var troy in Gametroy.Troys)
            {
                if (obj.Name.Contains(troy.Name) && obj.IsValid<GameObject>())
                {                    
                    troy.Obj = obj;
                    troy.Start = Utils.GameTimeTickCount;

                    if (!troy.Included)
                         troy.Included = Helpers.IsEnemyInGame(troy.Owner);
                }
            }
        }

        static void Game_OnUpdate(EventArgs args)
        {
            foreach (var hero in Activator.Allies())
            {
                var troy = Gametroy.Troys.FirstOrDefault(x => x.Included);
                if (troy == null)
                {
                    // if not included reverse ticks/damage
                    if (hero.TroyTicks > 0)
                    {
                        if (hero.TroyTicks == 1)
                            hero.HitTypes.Clear();

                        hero.TroyDamage -= 5;
                        hero.TroyTicks -= 1;
                    }

                    continue;
                }

                if (troy.Obj == null || !troy.Obj.IsValid || !troy.Obj.IsVisible)
                {
                    // if included but obj is null..?
                    if (hero.TroyTicks > 0)
                    {
                        if (hero.TroyTicks == 1)
                        {
                            hero.HitTypes.Clear();
                            troy.Included = false; // remove
                        }

                        hero.TroyDamage -= 5;
                        hero.TroyTicks -= 1;
                    }

                    continue;
                }

                foreach (var data in Troydata.Troys.Where(x => x.Name == troy.Name))
                {
                    if (hero.Player.Distance(troy.Obj.Position) <= data.Radius + hero.Player.BoundingRadius)
                    {
                        // check delay (e.g fizz bait)
                        if (Utils.GameTimeTickCount - troy.Start >= data.DelayFromStart)
                        {
                            if (hero.Player.IsValidTarget(float.MaxValue, false))
                            {
                                if (!hero.Player.IsZombie && !hero.Immunity)
                                {
                                    foreach (var ii in data.HitTypes)
                                    {
                                        if (!hero.HitTypes.Contains(ii))
                                             hero.HitTypes.Add(ii);
                                    }

                                    // limit the damage using an interval
                                    if (Utils.GameTimeTickCount - troy.Limiter >= data.Interval * 1000)
                                    {
                                        hero.TroyDamage += 5; // todo: get actuall spell damage
                                        hero.TroyTicks += 1;
                                        troy.Limiter = Utils.GameTimeTickCount;
                                    }
                                }
                            }

                            return;
                        }
                    }
                }

                // reset damage if walked out of obj
                if (hero.TroyTicks > 0)
                {
                    if (hero.TroyTicks == 1)
                        hero.HitTypes.Clear();

                    hero.TroyDamage -= 5;
                    hero.TroyTicks -= 1;
                }
            }
        }
    }
}