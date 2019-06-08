﻿using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using SPrediction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy_Mid.Champions
{
    class Brand
    {
        private static Menu _menu;
        private static Spell q, w, e, r;
        private static SpellSlot ignite;
        private static AIHeroClient _player { get { return ObjectManager.Player; } }
        #region
        //Combat
        public static readonly MenuBool comboQ = new MenuBool("comboQ", "Use Q on Combo");
        public static readonly MenuBool comboW = new MenuBool("comboW", "Use W on Combo");
        public static readonly MenuBool comboE = new MenuBool("comboE", "Use E on Combo");
        public static readonly MenuBool comboR = new MenuBool("comboR", "Use R on Combo");
        public static readonly MenuSlider Raoe = new MenuSlider("raoe", "^ Only use R if hits X enemies", 2, 1, 5);

        //Harass
        public static readonly MenuBool harassQ = new MenuBool("harassQ", "Use Q on Harass");
        public static readonly MenuBool harassW = new MenuBool("harassW", "Use W on Harass");
        public static readonly MenuBool harassE = new MenuBool("harassE", "Use E on Harass");
        public static readonly MenuSlider harassmana = new MenuSlider("harassmana", "^ Mana >= X%", 60, 0, 100);

        //Push Wave
        public static readonly MenuBool laneE = new MenuBool("laneE", "Use E on Clear Wave");
        public static readonly MenuBool laneW = new MenuBool("laneW", "Use W on Clear Wave");
        public static readonly MenuSlider clearsmana = new MenuSlider("clearsmana", "Clear Wave Mana >= X%", 60, 0, 100);

        //Hit Chance
        public static readonly MenuList qhit = new MenuList<string>("qhit", "Q - HitChance :", new[] { "High", "Medium", "Low"});
        public static readonly MenuList whit = new MenuList<string>("whit", "W - HitChance :", new[] { "High", "Medium", "Low" });
        #endregion

        public static void OnLoad()
        {
            q = new Spell(SpellSlot.Q, 1050);
            w = new Spell(SpellSlot.W, 900);
            e = new Spell(SpellSlot.E, 625);
            r = new Spell(SpellSlot.R, 750);

            q.SetSkillshot(0.25f, 60, 1600, true, SkillshotType.Line);
            w.SetSkillshot(1, 240, float.MaxValue, false, SkillshotType.Circle);
            e.SetTargetted(0.25f, float.MaxValue);
            r.SetTargetted(0.25f, 1000);

            ignite = _player.GetSpellSlot("summonerdot");

            MenuCreate();
            Game.OnUpdate += Game_OnGameUpdate;
        }

        private static void MenuCreate()
        {
            var _menu = new Menu("easymidbrand", "Easy_Mid.Brand", true);
            var hitconfig = new Menu("hitconfig", "[HIT CHANCE] Settings");
            hitconfig.Add(qhit);
            hitconfig.Add(whit);

            var combat = new Menu("combat", "[COMBO] Settings");
            combat.Add(comboQ);
            combat.Add(comboW);
            combat.Add(comboE);
            combat.Add(comboR);
            combat.Add(Raoe);

            var harass = new Menu("harass", "[HARASS] Settings");
            harass.Add(harassQ);
            harass.Add(harassW);
            harass.Add(harassE);
            harass.Add(harassmana);

            var clearwave = new Menu("clearwave", "[CLEAR WAVE] Settings");
            clearwave.Add(laneE);
            clearwave.Add(laneW);
            clearwave.Add(clearsmana);

            var pred = new Menu("spred", "[SPREDICTION] Settings");
            SPrediction.Prediction.Initialize(pred);

            _menu.Add(hitconfig);
            _menu.Add(combat);
            _menu.Add(harass);
            _menu.Add(clearwave);
            _menu.Add(pred);
            _menu.Attach();
        }

        public static void Game_OnGameUpdate(EventArgs args)
        {
            if (_player.IsDead)
                return;

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    DoCombo();
                    break;
                case OrbwalkerMode.Harass:
                    DoHarass();
                    break;
                case OrbwalkerMode.LaneClear:
                    DoLaneClear();
                    break;
            }
        }

        private static bool HasPassive(AIBaseClient unit)
        {
            return unit.HasBuff("brandablaze");
        }

        private double getComboDamage(AIHeroClient target)
        {
            double damage = _player.GetAutoAttackDamage(target);
            if (q.IsReady() && comboQ.Enabled)
                damage += _player.GetSpellDamage(target, SpellSlot.Q);
            if (w.IsReady() && comboW.Enabled)
                damage += _player.GetSpellDamage(target, SpellSlot.W);
            if (e.IsReady() && comboE.Enabled)
                damage += _player.GetSpellDamage(target, SpellSlot.E);
            if (r.IsReady() && comboR.Enabled)
                damage += _player.GetSpellDamage(target, SpellSlot.R);
            if (ignite.IsReady())
                damage += _player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);
            return damage;
        }


        public static HitChance hitchanceW()
        {
            var hit = HitChance.High;
            switch (whit.Index)
            {
                case 0:
                    hit = HitChance.High;
                    break;
                case 1:
                    hit = HitChance.Medium;
                    break;
                case 2:
                    hit = HitChance.Low;
                    break;
            }
            return hit;
        }

        public static HitChance hitchanceQ()
        {
            var hit = HitChance.High;
            switch (qhit.Index)
            {
                case 0:
                    hit = HitChance.High;
                    break;
                case 1:
                    hit = HitChance.Medium;
                    break;
                case 2:
                    hit = HitChance.Low;
                    break;
            }
            return hit;
        }

        private static void DoCombo()
        {
            var target = TargetSelector.GetTarget(w.Range - 50);
            var hitQ = hitchanceQ();
            var hitW = hitchanceW();

            if (target != null && target.IsValidTarget(e.Range))
            {
                if (e.IsReady() && comboE.Enabled)
                {
                    e.Cast(target,true);
                }
                if (q.IsReady() && HasPassive(target) && comboQ.Enabled)
                {
                    q.SPredictionCast(target, hitQ);
                }
                if (w.IsReady() && HasPassive(target) && comboW.Enabled)
                {
                    w.SPredictionCast(target, hitW);
                }
                if (r.IsReady() && HasPassive(target) && comboR.Enabled)
                {
                    if(Raoe.Value > 1)
                    {
                        if(target.CountEnemyHeroesInRange(750) >= Raoe.Value)
                        {
                            r.Cast(target, true);
                        }
                    }
                    else
                    {
                        r.Cast(target, true);
                    }
                }
            }
            else if(target.IsValidTarget(w.Range))
            {
                if (w.IsReady() && comboW.Enabled)
                {
                    w.SPredictionCast(target, hitW);
                }
                if (e.IsReady() && harassE.Enabled && target.IsValidTarget(e.Range))
                {
                    e.Cast(target, true);
                }
                if (q.IsReady() && HasPassive(target) && comboQ.Enabled)
                {
                    q.SPredictionCast(target, hitQ);
                }
                if (r.IsReady() && HasPassive(target) && comboR.Enabled)
                {
                    if (Raoe.Value > 1)
                    {
                        if (target.CountEnemyHeroesInRange(750) >= Raoe.Value)
                        {
                            r.Cast(target, true);
                        }
                    }
                    else
                    {
                        r.Cast(target, true);
                    }
                }
            }
        }

        public static void DoHarass()
        {
            var target = TargetSelector.GetTarget(w.Range - 50);
            var hitQ = hitchanceQ();
            var hitW = hitchanceW();

            if (target == null)
                return;

            if (_player.ManaPercent < harassmana.Value)
                return;

            if (target.IsValidTarget(e.Range))
            {
                if (e.IsReady() && harassE.Enabled)
                {
                    e.Cast(target, true);
                }
                if (q.IsReady() && HasPassive(target) && harassQ.Enabled)
                {
                    q.SPredictionCast(target, hitQ);
                }

                if (w.IsReady() && HasPassive(target) && harassW.Enabled)
                {
                    w.SPredictionCast(target, hitW);
                }
            }
            else if (target.IsValidTarget(w.Range))
            {
                if (w.IsReady() && harassW.Enabled)
                {
                    w.SPredictionCast(target, hitW);
                }
                if (e.IsReady() && harassE.Enabled)
                {
                    e.Cast(target, true);
                }
                if (q.IsReady() && HasPassive(target) && harassQ.Enabled)
                {
                    q.SPredictionCast(target, hitQ);
                }
            }

        }

        private static void DoLaneClear()
        {
            List<AIBaseClient> minions = MinionManager.GetMinions(_player.Position, w.Range);
            var wCastLocation = w.GetCircularFarmLocation(minions, w.Width);
            if (w.IsReady() && wCastLocation.MinionsHit > 2 && laneW)
            {
                w.Cast(wCastLocation.Position);
            }
            if (e.IsReady() && laneE)
            {
                foreach (AIBaseClient minion in minions)
                {
                    if (HasPassive(minion) && _player.Distance(minion.Position) < e.Range)
                    {
                        e.Cast(minion);
                        break;
                    }
                }
            }
        }
    }
}
