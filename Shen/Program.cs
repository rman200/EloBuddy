﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy.SDK;
using SharpDX;
using EloBuddy;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using System.Drawing;

namespace Shen
{
    class Program
    {

        

        static void Main(string[] args)
            {
                Loading.OnLoadingComplete += On_LoadingComplete; 
            }
        private const string MenuName = "Overpowered Shen";
        public static Menu Menu, ComboMenu, DrawingsMenu, FarmMenu, MiscMenu;
        public static Spell.Targeted Q { get; private set; }
        public static Spell.Active W { get; private set; }
        public static Spell.Skillshot E { get; private set; }
        public static Spell.Targeted R { get; private set; }

        private static void On_LoadingComplete(EventArgs args)  
        {
            if (ObjectManager.Player.ChampionName != "Shen")
            { 
                return; 
            }

            Q = new Spell.Targeted(SpellSlot.Q, 475);
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Skillshot(SpellSlot.E, 600, SkillShotType.Circular);
            R = new Spell.Targeted(SpellSlot.R, uint.MaxValue);
            Menu = MainMenu.AddMenu("Shen", "Shen");
            Menu.AddGroupLabel("Overpowered Shen");
            Menu.AddLabel("First EloBuddy addon!(supposed to be the second)");
            Menu.AddLabel("Coded by rman200");
            ComboMenu = Menu.AddSubMenu("Combo", "Combo");
            ComboMenu.AddGroupLabel("Combo Settings");
            ComboMenu.AddSeparator();
            ComboMenu.Add("useQCombo", new CheckBox("Use Q"));
            
            ComboMenu.Add("useECombo", new CheckBox("Use E"));
            
            
            
            DrawingsMenu = Menu.AddSubMenu("Drawings", "Drawings");
            DrawingsMenu.Add("DrawQ", new CheckBox("Draw Q"));
            DrawingsMenu.Add("DrawE", new CheckBox("Draw E"));
            

            FarmMenu = Menu.AddSubMenu("Farm", "Farm");
            FarmMenu.AddGroupLabel("LastHit");
            FarmMenu.Add("useQ", new CheckBox("Use Q"));

            MiscMenu = Menu.AddSubMenu("Misc", "Misc");
            MiscMenu.AddGroupLabel("Misc Settings");
            MiscMenu.AddSeparator();
            MiscMenu.Add("useRCombo", new CheckBox("Auto R Low Allies"));
            MiscMenu.Add("useEturret", new CheckBox("Auto E Under Turret"));
            MiscMenu.Add("useWCombo", new CheckBox("Auto W Incoming Damage"));
            MiscMenu.Add("autoHarass", new CheckBox("Auto Harass"));
            MiscMenu.Add("useAGapcloser", new CheckBox("Anti GapCloser"));
            
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
          
        
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (DrawingsMenu["DrawQ"].Cast<CheckBox>().CurrentValue)
            {
                Drawing.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Green);
            }
                       

            if (DrawingsMenu["DrawE"].Cast<CheckBox>().CurrentValue)
            {
                Drawing.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Crimson);
            }
       }

        private static void Game_OnUpdate(EventArgs args)
        {
            var autoHarass = MiscMenu["autoHarass"].Cast<CheckBox>().CurrentValue;
            var useEturret = MiscMenu["useEturret"].Cast<CheckBox>().CurrentValue;  
            var useR = MiscMenu["useRCombo"].Cast<CheckBox>().CurrentValue;
            //Orbwalker.ForcedTarget = null;
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))                                                    //Combo
            {
                var useQ = ComboMenu["useQCombo"].Cast<CheckBox>().CurrentValue;
                var useE = ComboMenu["useECombo"].Cast<CheckBox>().CurrentValue;
                
                foreach (var target in HeroManager.Enemies.Where(o => o.IsValidTarget(500) && !o.IsDead && !o.IsZombie))
                {
                    if (useQ && Q.IsReady())
                    {
                        Q.Cast(target);
                    }
                    if (useE && E.IsReady() && target.IsValidTarget(500) && !target.IsValidTarget(250))
                    {
                        E.Cast(target);
                    }
                }
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) || Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))          //Laning
            {
                var useQ = FarmMenu["useQ"].Cast<CheckBox>().CurrentValue;
                foreach (var minion in ObjectManager.Get<Obj_AI_Base>().OrderBy(m => m.Health).Where(m => m.IsMinion && !m.IsDead && m.IsValidTarget(475)))
                {
                    if (minion.Health < ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q)+10 && Q.IsReady() && useQ && !minion.IsValidTarget(200) )
                    {
                        Q.Cast(minion);
                    }
                }
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) || autoHarass)
            {
                var useQ = ComboMenu["useQCombo"].Cast<CheckBox>().CurrentValue;
                foreach (var target in HeroManager.Enemies.Where(o => o.IsValidTarget(500) && !o.IsDead && !o.IsZombie))
                {
                    if (useQ && Q.IsReady())
                    {
                        Q.Cast(target);
                    }
                }
            }
            foreach (var ally in HeroManager.Allies.Where(z => !z.IsDead && !z.IsZombie && z.HealthPercent < 60))
            if (useR && R.IsReady())
            {
                R.Cast(ally);
            }
            var turret = ObjectManager.Get<Obj_AI_Turret>().First(a => a.IsAlly && !a.IsDead && a.Distance(ObjectManager.Player) <= 750); //get nearest turret
            if (useEturret && E.IsReady() && turret != null)
            {
                var target2 = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                if (target2.Distance(turret) < 750)
                
                E.Cast(target2);
            }
        }
        public static void OnProcessSpell(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var useW = MiscMenu["useWCombo"].Cast<CheckBox>().CurrentValue;
            if (!sender.IsMe && sender.IsEnemy && ObjectManager.Player.Health < 200 && W.IsReady() && args.Target.IsMe && useW) // for minions attack
            {
                W.Cast();
            }
            if (!sender.IsMe && sender.IsEnemy && sender is AIHeroClient && args.Target.IsMe && W.IsReady() && useW) //for heroes
            {
                W.Cast();
            }
        }

        
        void Obj_AI_Turret_OnBasicAttack(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)          //for turrets
        {
            if (sender is Obj_AI_Turret && args.Target.IsMe && W.IsReady())
            {
                W.Cast();
            }
            

            
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs gapclose)
        {
            
            var UseGapcloser = MiscMenu["useAGapcloser"].Cast<CheckBox>().CurrentValue;

            if (E.IsReady() && ObjectManager.Player.Distance(gapclose.Sender, true) < E.Range * E.Range && UseGapcloser && gapclose.Sender.IsEnemy)
            {
                E.Cast(gapclose.Sender);
            }
        }
       

    }
}
