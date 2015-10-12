using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using System;
using System.Linq;
using SharpDX;


namespace Lissandra
{
    class Program
    {
        private const string MenuName = "Overpowered Lissandra";
        public static Menu Menu, ComboMenu, DrawingsMenu, FarmMenu, MiscMenu;
        public static Spell.Skillshot Q { get; private set; }
        public static Spell.Active W { get; private set; }
        public static Spell.Skillshot E { get; private set; }
        public static Spell.Targeted R { get; private set; }
        public static Vector2 missilepos;
        private static MissileClient LissandraEMissile;
        public static bool jump;
        //public static bool EnemyTurrets;
        //public static event Interrupter.InterruptableSpellHandler OnInterruptableSpell;
        
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoadingComplete;
        }
        private static void OnLoadingComplete(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != "Lissandra")
            {
                return;
            }
            Q = new Spell.Skillshot(SpellSlot.Q, 725,SkillShotType.Linear, 250, 2250, 75);
            W = new Spell.Active(SpellSlot.W, 450);
            E = new Spell.Skillshot(SpellSlot.E, 1050, SkillShotType.Linear, 250, 850,125);
            R = new Spell.Targeted(SpellSlot.R, 550);
            Menu = MainMenu.AddMenu("Lissandra", "Lissandra");
            Menu.AddGroupLabel("Overpowered Lissandra");
            Menu.AddLabel("First IceWitch addon!");
            Menu.AddLabel("Coded by rman200");
            ComboMenu = Menu.AddSubMenu("Combo", "Combo");
            ComboMenu.AddGroupLabel("Combo Settings");
            ComboMenu.AddSeparator();
            ComboMenu.Add("useQCombo", new CheckBox("Use Q"));
            ComboMenu.Add("useWCombo", new CheckBox("Use W"));
            ComboMenu.Add("useECombo", new CheckBox("Use E"));
            //ComboMenu.Add("useRCombo", new CheckBox("Use R"));



            DrawingsMenu = Menu.AddSubMenu("Drawings", "Drawings");
            DrawingsMenu.Add("DrawQ", new CheckBox("Draw Q"));
            DrawingsMenu.Add("DrawW", new CheckBox("Draw W"));
            DrawingsMenu.Add("DrawE", new CheckBox("Draw E"));
            DrawingsMenu.Add("DrawR", new CheckBox("Draw R"));


            FarmMenu = Menu.AddSubMenu("Farm", "Farm");
            FarmMenu.AddGroupLabel("LastHit");
            FarmMenu.Add("useQlh", new CheckBox("Use Q"));

            FarmMenu.AddSeparator();

            FarmMenu.AddGroupLabel("LaneClear");
            FarmMenu.Add("useQlc", new CheckBox("Use Q"));
            FarmMenu.Add("useWlc", new CheckBox("Use W"));
            FarmMenu.Add("useElc", new CheckBox("Use E"));


            MiscMenu = Menu.AddSubMenu("Misc", "Misc");
            MiscMenu.AddGroupLabel("Misc Settings");
            MiscMenu.AddSeparator();
            MiscMenu.Add("useRCombo", new CheckBox("Auto R Low Allies"));
            MiscMenu.Add("useWturret", new CheckBox("Auto W Under Turret"));
            MiscMenu.Add("autoHarass", new CheckBox("Auto Harass Q if Passive is On"));
            MiscMenu.Add("useAGapcloser", new CheckBox("Anti GapCloser"));
            MiscMenu.Add("useEflee", new CheckBox("E flee"));
            MiscMenu.Add("useWflee", new CheckBox("W flee"));
            MiscMenu.AddSeparator();
            MiscMenu.AddGroupLabel("Ult Settings");
            MiscMenu.Add("ulthp", new Slider("Min Health % to Ult", 20, 0, 100));
            /*foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                MiscMenu.Add("dontR" + enemy.ChampionName, new CheckBox("Dont Ult " + enemy.ChampionName, false));
            }*/
            Game.OnTick += MissilePosition;
            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += OnDraw;
            Interrupter.OnInterruptableSpell += OnInterruptableSpell;
            
           // Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
           // Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;

        }


        private static void OnDraw(EventArgs args)
        {
            if (DrawingsMenu["DrawQ"].Cast<CheckBox>().CurrentValue)
            { Drawing.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan); }
            if (DrawingsMenu["DrawW"].Cast<CheckBox>().CurrentValue)
            { Drawing.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.DarkBlue); }
            if (DrawingsMenu["DrawE"].Cast<CheckBox>().CurrentValue)
            { Drawing.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Blue);}
            if (DrawingsMenu["DrawR"].Cast<CheckBox>().CurrentValue)
            { Drawing.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.DeepSkyBlue); }

        }

        /*private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe && sender.IsEnemy && sender.is)
            {
                if (args.SData.Name == "")
                {
                    
                }
            }
        }
         */
        private static void Game_OnTick(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            { return; }
            Ult();
            AutoWonTower();
            AutoHarass();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))                                                    //Combo
            {
                //Chat.Print("Combo on");
                Combo();
                
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))                                                    //Combo
            {
                Harass();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))                                                    //Combo
            {
                LastHit();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))                                                    //Combo
            {
                LaneClear();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))                                                    //Combo
            {
                Flee();
            }

        }

        static void OnCreate(GameObject sender, EventArgs args)
        {
            var miss = sender as MissileClient;
            if (miss != null && miss.IsValid)
            {
                if (miss.SpellCaster.IsMe && miss.SpellCaster.IsValid && miss.SData.Name == "LissandraEMissile")
                {
                    LissandraEMissile = miss;
                    Chat.Print("Missil criado");
                }
            }
        }

        static void OnDelete(GameObject sender, EventArgs args)
        {
            var miss = sender as MissileClient;
            if (miss == null || !miss.IsValid) return;
            if (miss.SpellCaster is AIHeroClient && miss.SpellCaster.IsValid && miss.SpellCaster.IsMe && miss.SData.Name == "LissandraEMissile")
            {
                LissandraEMissile = null;
                missilepos = new Vector2(0, 0);
                Chat.Print("Missil apagado");
            }
        }
        
        private static void MissilePosition(EventArgs args)
        {
            if (LissandraEMissile == null || ObjectManager.Player.IsDead)
            {
                return;
            }
            missilepos = LissandraEMissile.Position.To2D();
        }

        //Combo
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range + E.Range, DamageType.Magical);
            var useQ = ComboMenu["useQCombo"].Cast<CheckBox>().CurrentValue;
            var useW = ComboMenu["useWCombo"].Cast<CheckBox>().CurrentValue;
            var useE = ComboMenu["useECombo"].Cast<CheckBox>().CurrentValue;
            foreach (var target2 in EntityManager.Heroes.Enemies.Where(t => !t.IsDead && t.IsValidTarget(W.Range)))
                {
                if (W.IsReady() && useW )
                { W.Cast(); }
            }

            if (!target.IsZombie && !target.IsInvulnerable && !target.IsDead)
            {
                
                if (Q.IsReady() && useQ && target.IsValidTarget(Q.Range))
                { Q.Cast(target);
                    
                }
                else if (useQ && Q.IsReady() && target.IsValidTarget(800) && !target.IsZombie && !target.IsInvulnerable && !target.IsDead)
                {
                    foreach (var minion in EntityManager.MinionsAndMonsters.EnemyMinions.Where(m => !m.IsDead && m.IsValidTarget(800)))
                    {
                        if (target.Distance(ObjectManager.Player, false) == minion.Distance(ObjectManager.Player, false) + minion.Distance(target, false))
                        { Q.Cast(minion); }
                    }
                }

                if (E.IsReady() && useE && !target.IsValidTarget(Q.Range) && target.IsValidTarget(E.Range + Q.Range - 100))
                {
                    Chat.Print("deveria pular");
                    if (!ObjectManager.Player.HasBuff("LissandraE"))
                    {
                        Chat.Print("vai poular");
                        ObjectManager.Player.Spellbook.CastSpell(SpellSlot.E, target.ServerPosition);
                        jump = true;
                    }
                    if (Vector2.Distance(missilepos, LissandraEMissile.EndPosition.To2D()) <= 100 && ObjectManager.Player
                        .HasBuff("LissandraE") && target.Distance(ObjectManager.Player, false) > target.Distance(missilepos, false) && jump == true /*&& EnemyTurrets != false*/ )
                    {
                        Chat.Print("pulando");
                        E.Cast(ObjectManager.Player);
                        jump = false;
                    }

                }
            }
        }



        //Harass
        private static void Harass()
        {
            var useQ = ComboMenu["useQCombo"].Cast<CheckBox>().CurrentValue;
            var target = TargetSelector.GetTarget(800, DamageType.Magical);
            if (useQ && Q.IsReady() && target.IsValidTarget(Q.Range) && !target.IsZombie && !target.IsInvulnerable && !target.IsDead)
            {
                Q.Cast(target);
            }
            else if (useQ && Q.IsReady() && target.IsValidTarget(800) && !target.IsZombie && !target.IsInvulnerable && !target.IsDead)
            {
                foreach (var minion in EntityManager.MinionsAndMonsters.EnemyMinions.Where(m => !m.IsDead && m.IsValidTarget(800)))
                {
                     if (target.Distance(ObjectManager.Player, false) == minion.Distance(ObjectManager.Player, false) + minion.Distance(target, false))
                        { Q.Cast(minion); } 
                }
            }
        }



        //LastHit
        private static void LastHit()
        {
            var minions = EntityManager.MinionsAndMonsters.EnemyMinions.OrderBy(m => m.Health).Where(m => !m.IsDead && m.IsValidTarget(Q.Range));
            if (minions == null) return;
            var useQ = FarmMenu["useQlh"].Cast<CheckBox>().CurrentValue;

            foreach (var minion in minions)
            {
                if (useQ && Q.IsReady() && minion.Health <= ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q))
                {
                    Q.Cast(minion);
                }
            }
        }



        //LaneClear
        private static void LaneClear()
        {
            var minions = EntityManager.MinionsAndMonsters.EnemyMinions.OrderBy(m => m.Health).Where(m => m.IsEnemy && !m.IsDead && m.IsValidTarget(Q.Range));
            if (minions == null) return;
            var useQ = FarmMenu["useQlc"].Cast<CheckBox>().CurrentValue;
            //var useW = FarmMenu["useWlc"].Cast<CheckBox>().CurrentValue;
            //var useE = FarmMenu["useElc"].Cast<CheckBox>().CurrentValue;

            foreach (var minion in minions)
            {
                if (useQ && Q.IsReady() && minion.Health <= ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q))
                {
                    Q.Cast(minion);

                }
                /*if (useW && W.IsReady())
                {
                    W.Cast();
                }*/
                               
            }
        }
        private static void Flee()
        {
            
            var enemies = EntityManager.Heroes.Enemies.Where(m => m.IsValidTarget(E.Range));
            if (enemies == null) return;
            
            
            var useW = MiscMenu["useWflee"].Cast<CheckBox>().CurrentValue;
            var useE = MiscMenu["useEflee"].Cast<CheckBox>().CurrentValue;

            foreach (var enemy in enemies)
            {
                if (useW && W.IsReady() && enemy.IsValidTarget(450))
                {
                    W.Cast();

                }
                if (useE && E.IsReady()) 
                {
                    E.Cast(Game.CursorPos);
                    if (Vector2.Distance(missilepos, LissandraEMissile.EndPosition.To2D())<= 100 && missilepos.Distance(enemy, false) > ObjectManager.Player.Distance(enemy, false))
                    { E.Cast(ObjectManager.Player); }

                }

            }
        }


        //Stun Tower
        private static void AutoWonTower()
        {
            var useW = MiscMenu["useWturret"].Cast<CheckBox>().CurrentValue;
            var target = TargetSelector.GetTarget(W.Range, DamageType.Magical);
            var tower = ObjectManager.Get<Obj_AI_Turret>().FirstOrDefault(a => !a.IsEnemy && !a.IsInvulnerable && !a.IsDead && a.Distance(ObjectManager.Player) <= 750);

            if (useW && W.IsReady() && target != null && tower != null && target.Distance(tower) <= 750)
            {
                W.Cast();
            }
        }

        private static void Ult()
        {
            var useR = MiscMenu["useRCombo"].Cast<CheckBox>().CurrentValue; //R check
            if (!R.IsReady() || !useR)
            { return;  }
            var ulthp = MiscMenu["ulthp"].Cast<Slider>().CurrentValue;
            if (ObjectManager.Player.HealthPercent <= ulthp)
            {
                R.Cast(ObjectManager.Player);
                return;
            }

            /*var NearEnemies = 0;
            foreach (var enemy2 in EntityManager.Heroes.Enemies.Where(o => o.IsValidTarget(R.Range) && !o.IsDead && !o.IsZombie))
            {
                
                //NearEnemies ++;
                /*if (enemy2.Health < ObjectManager.Player.GetSpellDamage(enemy2, SpellSlot.R))
                {
                    R.Cast(enemy2);
                    return;
                }
                if (NearEnemies >= 3 && ObjectManager.Player.HealthPercent <= 40)
                {
                    R.Cast(ObjectManager.Player);
                    return;
                }
                
            }*/
        }
        private static void AutoHarass()
        {
            var AutoHarass = MiscMenu["autoHarass"].Cast<CheckBox>().CurrentValue;
            if (AutoHarass && Q.IsReady() && ObjectManager.Player.HasBuff("LissandraPassiveReady"))
            {
                Harass();
            }
        }
       private static void OnInterruptableSpell(Obj_AI_Base sender , Interrupter.InterruptableSpellEventArgs Interrupt)
        {
            var dangerlvl = Interrupt.DangerLevel.ToString();
            if (!sender.IsDead && !sender.IsZombie && !sender.IsMe && sender.IsEnemy && (dangerlvl == "4" || dangerlvl == "5") )
            {
                if (sender.IsValidTarget(550) && R.IsReady() )
                { R.Cast(sender); } 
                else if (sender.IsValidTarget(450) && W.IsReady())
                { W.Cast(); }
            }
        }


    }
}