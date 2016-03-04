using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Leblanc
{
    class Program
    {
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        private static Orbwalking.Orbwalker Orbwalker;

        private static Spell Q, W, E, R;

        private static Menu Menu;

        private static int Rstate, Wstate, Ecol;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Leblanc")
                return;

            Q = new Spell(SpellSlot.Q, 710);
            W = new Spell(SpellSlot.W, 750);
            E = new Spell(SpellSlot.E, 950);
            R = new Spell(SpellSlot.R);
            if (Rstate == 1)
                R = new Spell(SpellSlot.R, Q.Range);
            if (Rstate == 2)
            {
                R = new Spell(SpellSlot.R, W.Range);
                R.SetSkillshot(0, 70, 1500, false, SkillshotType.SkillshotLine);
            }

            //Q.SetSkillshot(300, 50, 2000, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 70, 1600, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0, 70, 1500, false, SkillshotType.SkillshotLine);

            Menu = new Menu(Player.ChampionName, Player.ChampionName, true);
            Menu orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            Menu.AddSubMenu(orbwalkerMenu);
            Menu ts = Menu.AddSubMenu(new Menu("Target Selector", "Target Selector")); ;
            TargetSelector.AddToMenu(ts);


            Menu spellMenu = Menu.AddSubMenu(new Menu("Spells", "Spells"));
            spellMenu.AddItem(new MenuItem("Use Q Harass", "Use Q Harass").SetValue(true));
            spellMenu.AddItem(new MenuItem("Use W Harass", "Use W Harass").SetValue(true));
            spellMenu.AddItem(new MenuItem("Use W Back Harass", "Use W Back Harass").SetValue(true));
            spellMenu.AddItem(new MenuItem("Use W Combo", "Use W Combo").SetValue(true));
            spellMenu.AddItem(new MenuItem("Use W Combo Gap", "Use W Combo Gap").SetValue(true));
            spellMenu.AddItem(new MenuItem("force focus selected", "force focus selected").SetValue(false));
            spellMenu.AddItem(new MenuItem("if selected in :", "if selected in :").SetValue(new Slider(1000, 1000, 1500)));
            spellMenu.AddItem(new MenuItem("QE Selected Target", "QE Selected Target").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
            spellMenu.AddItem(new MenuItem("RunActive", "Run!").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));

            Menu.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;

            Game.OnUpdate += Game_OnGameUpdate;


            Game.PrintChat("Welcome to LeblancWorld");
        }
        public static bool WgapCombo { get { return Menu.Item("Use W Combo Gap").GetValue<bool>(); } }
        public static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;
            CheckR();
            CheckW();
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
            	CheckR();
            	CheckW();
                Combo();
            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                if (Menu.Item("Use Q Harass").GetValue<bool>())
                {
                    useQ();
                }
                if (Menu.Item("Use W Harass").GetValue<bool>())
                {
                    useWH();
                }
                if (Menu.Item("Use W Back Harass").GetValue<bool>())
                {
                    useWBH();
                }

            }
            else if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                LaneClear();
            }
            CheckR();
            CheckW();
            if (Menu.Item("QE Selected Target").GetValue<KeyBind>().Active)
            {
                useQE();
            }
            if (Menu.Item("RunActive").GetValue<KeyBind>().Active)
                Run();
        }
        private static void Run()
        {
             Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
             
            if (W.IsReady() &&  Player.Spellbook.GetSpell(SpellSlot.W).Name.ToLower() == "leblancslide")
                W.Cast(Game.CursorPos);
            else if (R.IsReady() && Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSlideM")
                R.Cast(Game.CursorPos);
        }
        private static void Drawing_OnDraw(EventArgs args)
	{
	
		Utility.DrawCircle(Player.Position, 1200, Color.MediumTurquoise, 1, 55);
		Utility.DrawCircle(Player.Position, Q.Range, Color.MediumTurquoise, 1, 55);
		var target = TargetSelector.GetTarget(1200, TargetSelector.DamageType.Magical);
		if (target==null) return;
		Utility.DrawCircle( target.Position, 150,Color.MediumTurquoise, 30,
		55);
	
	}
        public static bool Selected()
        {
            if (!Menu.Item("force focus selected").GetValue<bool>())
            {
                return false;
            }
            else
            {
                var target = TargetSelector.GetSelectedTarget();
                float a = Menu.Item("if selected in :").GetValue<Slider>().Value;
                if (target == null || target.IsDead || target.IsZombie)
                {
                    return false;
                }
                else
                {
                    if (Player.Distance(target.Position) > a)
                    {
                        return false;
                    }
                    return true;
                }
            }
        }

        public static void useQ()
        {
            if (Selected())
            {
                var target = TargetSelector.GetSelectedTarget();
                if (target != null && target.IsValidTarget(Q.Range))
                {
                    Q.Cast(target);
                }

            }
            else
            {
                var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (target != null && target.IsValidTarget(Q.Range))
                {
                    Q.Cast(target);
                }
            }
        }

        public static void useE()
        {
            if (Selected())
            {
                var target = TargetSelector.GetSelectedTarget();
                if (target != null && target.IsValidTarget(Q.Range))
                {
                    CastE(target);
                }

            }
            else
            {
                var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical) ??
                             TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                if (target != null && target.IsValidTarget(E.Range))
                {
                    CastE(target);
                }
            }
        }
        public static void useW()
        {
            if (Menu.Item("Use W Combo").GetValue<bool>())
            {
                if (Selected())
                {
                    var target = TargetSelector.GetSelectedTarget();
                    if (target != null && target.IsValidTarget(W.Range))
                    {
                        CastW(target);
                    }

                }
                else
                {
                    var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
                    if (target != null && target.IsValidTarget(W.Range))
                    {
                        CastW(target);
                    }
                }
            }
        }

        public static void useWH()
        {
            if (Selected())
            {
                var target = TargetSelector.GetSelectedTarget();
                if (target != null && target.IsValidTarget(W.Range))
                {
                    CastW(target);
                }

            }
            else
            {
                var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
                if (target != null && target.IsValidTarget(W.Range))
                {
                    CastW(target);
                }
            }
        }

        public static void useWBH()
        {
            if (Wstate == 2)
                W.Cast();
        }

        public static void useR()
        {
            if (Selected())
            {
                var target = TargetSelector.GetSelectedTarget();
                if (target != null && target.IsValidTarget(Q.Range))
                {
                    CastR(target);
                }

            }
            else
            {
                var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                if (target != null && target.IsValidTarget(E.Range))
                {
                    CastR(target);
                }
            }
        }

        public static void CastR(Obj_AI_Base target)
        {
            if (R.IsReady())
            {
                if (Rstate == 1)
                {
                    if (target.IsValidTarget(R.Range))
                    {
                        R.Cast(target);
                    }
                }
                if (Rstate == 2)
                {
                    var t = Prediction.GetPrediction(target, 400).CastPosition;
                    float x = target.MoveSpeed;
                    float y = x * 400 / 1000;
                    var pos = target.Position;
                    if (target.Distance(t) <= y)
                    {
                        pos = t;
                    }
                    if (target.Distance(t) > y)
                    {
                        pos = target.Position.Extend(t, y - 50);
                    }
                    if (Player.Distance(pos) <= 600)
                    {
                        R.Cast(pos);
                    }
                    if (Player.Distance(pos) > 600)
                    {
                        if (target.Distance(t) > y)
                        {
                            var pos2 = target.Position.Extend(t, y);
                            if (Player.Distance(pos2) <= 600)
                            {
                                R.Cast(pos2);
                            }
                            else
                            {
                                var prediction = R.GetPrediction(target);
                                if (prediction.Hitchance >= HitChance.High)
                                {
                                    var pos3 = prediction.CastPosition;
                                    var pos4 = Player.Position.Extend(pos3, 600);
                                    R.Cast(pos4);
                                }
                            }
                        }

                    }
                }
            }
        }

        public static void CastW(Obj_AI_Base target)
        {
            if (!W.IsReady() || Player.Spellbook.GetSpell(SpellSlot.W).Name.ToLower() != "leblancslide")
                return;
            var t = Prediction.GetPrediction(target, 400).CastPosition;
            float x = target.MoveSpeed;
            float y = x * 400 / 1000;
            var pos = target.Position;
            if (target.Distance(t) <= y)
            {
                pos = t;
            }
            if (target.Distance(t) > y)
            {
                pos = target.Position.Extend(t, y - 50);
            }
            if (Player.Distance(pos) <= 600)
            {
                W.Cast(pos);
            }
            if (Player.Distance(pos) > 600)
            {
                if (target.Distance(t) > y)
                {
                    var pos2 = target.Position.Extend(t, y);
                    if (Player.Distance(pos2) <= 600)
                    {
                        W.Cast(pos2);
                    }
                    else
                    {
                        var prediction = W.GetPrediction(target);
                        if (prediction.Hitchance >= HitChance.High)
                        {
                            var pos3 = prediction.CastPosition;
                            var pos4 = Player.Position.Extend(pos3, 600);
                            W.Cast(pos4);
                        }
                    }

                }
            }
        }

        public static void CastE(Obj_AI_Base target)
        {
	CheckR();
            if (E.IsReady() && !Player.IsDashing())
            {
                if (!R.IsReady())
                { E.Cast(target); }
                if (R.IsReady() && Rstate == 4)
                { E.Cast(target); }
            }
        }

        public static void CheckE(Obj_AI_Base target)
        {
            if (E.IsReady())
            {
                var prediction = E.GetPrediction(target);
                if (prediction.Hitchance == HitChance.Collision)
                {
                    Ecol = 1;
                }
                else
                {
                    Ecol = 0;
                }
            }
            if (!E.IsReady())
            {
                Ecol = 0;
            }
        }
        public static void CheckR()
        {
            string x = Player.Spellbook.GetSpell(SpellSlot.R).Name;
            if (x == "LeblancChaosOrbM")
                Rstate = 1;
            if (x == "LeblancSlideM")
                Rstate = 2;
            if (x == "LeblancSoulShackleM")
                Rstate = 3;
            if (x == "leblancslidereturnm")
            {
                Rstate = 4;
            }
            if (Rstate == 1)
                R = new Spell(SpellSlot.R, Q.Range);
            if (Rstate == 2)
            {
                R = new Spell(SpellSlot.R, W.Range);
                R.SetSkillshot(0, 70, 1500, false, SkillshotType.SkillshotLine);
            }
        }
        public static void CheckW()
        {
            string x = Player.Spellbook.GetSpell(SpellSlot.W).Name;
            if (x == "leblancslidereturn")
            {
                Wstate = 2;
            }
            else
                Wstate = 1;
        }

 public static void Combo()
        {
            if (Selected())
            {
                var target = TargetSelector.GetSelectedTarget();
                if (target==null) return;
                CheckE(target);
                float a = Player.Distance(target.Position);
                if (a > Q.Range && a <= 1200) //dash closer
                {
                    if (W.IsReady() &&  Player.Spellbook.GetSpell(SpellSlot.W).Name.ToLower() == "leblancslide" && Menu.Item("Use W Combo").GetValue<bool>())
                    {
                        W.Cast(Player.Position.Extend(target.Position, 600));
                    }
                    else if (R.IsReady() && Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSlideM")
                    {
                       R.Cast(Player.Position.Extend(target.Position, 600));
                    }
                }
                if (a <= Q.Range) //if in range
                {
                    if (Ecol == 1) //if e is blocked
                    {
                        if (W.IsReady() && Player.Spellbook.GetSpell(SpellSlot.W).Name.ToLower() == "leblancslide") //if we can use W
                        {
                            useW();
                            useQ();
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name != "leblancslidereturnm") useR();
                            useE();
                        }
                        if (!W.IsReady() && Player.Spellbook.GetSpell(SpellSlot.W).Name.ToLower() == "leblancslide" && R.IsReady() && Rstate == 2)
                        {
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name != "leblancslidereturnm") useR();
                            useQ();
                            useE();
                            useW();
                        }
                        else
                        {
                            useQ();
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name != "leblancslidereturnm") useR();
                            useE();
                            if (Wstate !=2) useW();
                        }
                    }
                    if (Ecol == 0)
                    {
                        useQ();
                        if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name != "leblancslidereturnm") useR();
                        useE();
                        if (!(R.IsReady() && Rstate == 1))
                            if (Wstate==1) useW();
                    }
                }
            }
            else
            {
                var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (target != null)
                {
                    CheckE(target);
                    if (Ecol == 1)
                    {
                        if (W.IsReady() && Wstate == 1)
                        {
                            useW();
                            useQ();
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name != "leblancslidereturnm") useR();
                            useE();
                        }
                        else if (!W.IsReady()  && R.IsReady() && Rstate == 2)
                        {
                            useR();
                            useQ();
                            useE();
                            if (Wstate == 1) useW();
                        }
                        else
                        {
                            useQ();
                            if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name != "leblancslidereturnm") useR();
                            useE();
                            if (Wstate == 1) useW();
                        }
                    }
                    if (Ecol == 0)
                    {
                        useQ();
                        if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name != "leblancslidereturnm") useR();
                        useE();
                        if (!(R.IsReady() && Rstate == 1))
                            if (Wstate==1) useW();
                    }
                }
                else
                {
                    var target1 = TargetSelector.GetTarget(1200, TargetSelector.DamageType.Magical);
                    if (target1 != null)
                    {
                        if ( W.IsReady() && Player.Spellbook.GetSpell(SpellSlot.W).Name.ToLower() == "leblancslide" && Menu.Item("Use W Combo").GetValue<bool>())
                        {
                            W.Cast(Player.Position.Extend(target1.Position, 600));
                        }
                        else if (R.IsReady() && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSlideM")
                        {
                            R.Cast(Player.Position.Extend(target.Position, 600));
                        }
                    }
                }
            }
        }
        public static void useQE()
        {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                var target = TargetSelector.GetSelectedTarget();
                if (target != null && target.IsValidTarget() && !target.IsZombie)
                {
                    if( Player.Distance(target.Position) <= Q.Range)
                    {
                        Q.Cast(target);
                    }
                    if (Player.Distance(target.Position) <= E.Range)
                    {
                        E.Cast(target);
                    }
                } 
        }
         private static void LaneClear()
        {

            if ( Q.IsReady())
            {
                var minionsQ = MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
                foreach (Obj_AI_Base vMinion in 
                    from vMinion in minionsQ
                    let vMinionQDamage = ObjectManager.Player.GetSpellDamage(vMinion, SpellSlot.Q)
                    where
                        vMinion.Health <= vMinionQDamage &&
                        vMinion.Health > ObjectManager.Player.GetAutoAttackDamage(vMinion)
                    select vMinion)
                {
                    Q.CastOnUnit(vMinion);
                }
            }
	if (W.IsReady())
	{
            var canCastUlt = R.IsReady();
            var minions = MinionManager.GetMinions(W.Range).Select(m => m.ServerPosition.To2D()).ToList();
            var minionPrediction = MinionManager.GetBestCircularFarmLocation(minions, 100, W.Range);
            var castPosition = minionPrediction.Position.To3D();
            var notEnoughHits = minionPrediction.MinionsHit < 4;
            if (!notEnoughHits)
            {
                 W.Cast(castPosition);
            }

           
	}
            
        }
    }
}
