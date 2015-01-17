﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace xcsoft_Ezreal
{
    internal class Program
    {
        static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        static Orbwalking.Orbwalker Orbwalker;

        static Spell Q,W,E,R;

        private static Menu Menu;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Ezreal")
                return;

            Q = new Spell(SpellSlot.Q, 1200);
            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 800);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E, 1225);

            R = new Spell(SpellSlot.R, 3000);
            R.SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);


            Menu = new Menu("[xcsoft] Ezreal (Work in progress)", "xcoft_ezreal", true);

            var orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            Menu.AddSubMenu(orbwalkerMenu);

            var ts = Menu.AddSubMenu(new Menu("Target Selector", "Target Selector"));
            TargetSelector.AddToMenu(ts);

            var comboMenu = new Menu("ComboMode Settings", "comboset");
            comboMenu.AddItem(new MenuItem("comboUseQ", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("comboUseW", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem("comboUseR", "Use R").SetValue(true));
            Menu.AddSubMenu(comboMenu);

            var lasthitMenu = new Menu("Lasthit Settings", "lasthitset");
            lasthitMenu.AddItem(new MenuItem("lasthitUseQ", "Use Q").SetValue(true));
            Menu.AddSubMenu(lasthitMenu);

            var harassMenu = new Menu("Harass Settings", "harassop");
            harassMenu.AddItem(new MenuItem("harassUseQ", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem("harassAutoQ", "Use Auto Q").SetValue(true));
            Menu.AddSubMenu(harassMenu);

            var laneclearMenu = new Menu("LaneClear Settings", "laneclearset");
            laneclearMenu.AddItem(new MenuItem("laneclearUseQ", "Use Q").SetValue(true));
            Menu.AddSubMenu(laneclearMenu);

            var jungleclearMenu = new Menu("JungleClear Settings", "jungleclearset");
            jungleclearMenu.AddItem(new MenuItem("jungleclearUseQ", "Use Q").SetValue(true));
            Menu.AddSubMenu(jungleclearMenu);

            var additionalMenu = new Menu("Additional Options", "additionalop");
            additionalMenu.AddItem(new MenuItem("killsteal", "Try Killsteal (with safe E)").SetValue(true));
            additionalMenu.AddItem(new MenuItem("packet", "Use Packet casting").SetValue(false));
            additionalMenu.AddItem(new MenuItem("arcane", "Arcane Shift").SetValue(new KeyBind('E', KeyBindType.Press)));
            Menu.AddSubMenu(additionalMenu);

            var Drawings = new Menu("Drawings Settings", "Drawings");
            Drawings.AddItem(new MenuItem("AAcircle", "Real AA Range").SetValue(new Circle(true, Color.LightSkyBlue)));
            Drawings.AddItem(new MenuItem("Qcircle", "Q Range").SetValue(new Circle(true, Color.LightGoldenrodYellow)));
            Drawings.AddItem(new MenuItem("Wcircle", "W Range").SetValue(new Circle(true, Color.LightGoldenrodYellow)));
            Drawings.AddItem(new MenuItem("Ecircle", "E Range").SetValue(new Circle(false, Color.LightGoldenrodYellow)));
            Drawings.AddItem(new MenuItem("drawMinionLastHit", "Minion Last Hit").SetValue(new Circle(true, Color.GreenYellow)));
            Drawings.AddItem(new MenuItem("drawMinionNearKill", "Minion Near Kill").SetValue(new Circle(true, Color.Gray)));
            Drawings.AddItem(new MenuItem("jgpos", "JunglePosition").SetValue(true));
            Menu.AddSubMenu(Drawings);

            var predMenu = new Menu("Prediction", "pred");
            predMenu.AddItem(new MenuItem("kappa", "Maybe Best"));
            Menu.AddSubMenu(predMenu);

            var havefun = new MenuItem("Have Fun!", "Have Fun!");
            Menu.AddItem(havefun);

            Menu.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Drawing.OnDraw += Drawing_OnDraw;

            Game.PrintChat("<font color = \"#33CCCC\">[xcsoft] Ezreal -</font> Loaded");
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;

            var AAcircle = Menu.Item("AAcircle").GetValue<Circle>();
            var Qcircle = Menu.Item("Qcircle").GetValue<Circle>();
            var Wcircle = Menu.Item("Wcircle").GetValue<Circle>();
            var Ecircle = Menu.Item("Ecircle").GetValue<Circle>();

            if (AAcircle.Active)
                Render.Circle.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player), AAcircle.Color, 5);

            if (Q.IsReady() && Qcircle.Active)
                Render.Circle.DrawCircle(Player.Position, Q.Range, Qcircle.Color, 5);

            if (W.IsReady() && Wcircle.Active)
                Render.Circle.DrawCircle(Player.Position, W.Range, Wcircle.Color, 5);

            if (E.IsReady() && Ecircle.Active)
                Render.Circle.DrawCircle(Player.Position, 475, Ecircle.Color, 5);

            var drawMinionLastHit = Menu.Item("drawMinionLastHit").GetValue<Circle>();
            var drawMinionNearKill = Menu.Item("drawMinionNearKill").GetValue<Circle>();
            if (drawMinionLastHit.Active || drawMinionNearKill.Active)
            {
                var xMinions =
                    MinionManager.GetMinions(Player.Position, Player.AttackRange + Player.BoundingRadius + 300, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);

                foreach (var xMinion in xMinions)
                {
                    if (drawMinionLastHit.Active && Player.GetAutoAttackDamage(xMinion, true) >= xMinion.Health)
                    {
                        Render.Circle.DrawCircle(xMinion.Position, xMinion.BoundingRadius, drawMinionLastHit.Color, 5);
                    }
                    else if (drawMinionNearKill.Active && Player.GetAutoAttackDamage(xMinion, true) * 2 >= xMinion.Health)
                    {
                        Render.Circle.DrawCircle(xMinion.Position, xMinion.BoundingRadius, drawMinionNearKill.Color, 5);
                    }
                }
            }

            if (Game.MapId == (GameMapId)11 && Menu.Item("jgpos").GetValue<bool>())
            {
                const float circleRange = 100f;

                Render.Circle.DrawCircle(new Vector3(7461.018f, 3253.575f, 52.57141f), circleRange, Color.Blue, 5); // blue team :red
                Render.Circle.DrawCircle(new Vector3(3511.601f, 8745.617f, 52.57141f), circleRange, Color.Blue, 5); // blue team :blue
                Render.Circle.DrawCircle(new Vector3(7462.053f, 2489.813f, 52.57141f), circleRange, Color.Blue, 5); // blue team :golems
                Render.Circle.DrawCircle(new Vector3(3144.897f, 7106.449f, 51.89026f), circleRange, Color.Blue, 5); // blue team :wolfs
                Render.Circle.DrawCircle(new Vector3(7770.341f, 5061.238f, 49.26587f), circleRange, Color.Blue, 5); // blue team :wariaths

                Render.Circle.DrawCircle(new Vector3(10930.93f, 5405.83f, -68.72192f), circleRange, Color.Yellow, 5); // Dragon

                Render.Circle.DrawCircle(new Vector3(7326.056f, 11643.01f, 50.21985f), circleRange, Color.Red, 5); // red team :red
                Render.Circle.DrawCircle(new Vector3(11417.6f, 6216.028f, 51.00244f), circleRange, Color.Red, 5); // red team :blue
                Render.Circle.DrawCircle(new Vector3(7368.408f, 12488.37f, 56.47668f), circleRange, Color.Red, 5); // red team :golems
                Render.Circle.DrawCircle(new Vector3(10342.77f, 8896.083f, 51.72742f), circleRange, Color.Red, 5); // red team :wolfs
                Render.Circle.DrawCircle(new Vector3(7001.741f, 9915.717f, 54.02466f), circleRange, Color.Red, 5); // red team :wariaths                    
            }

            if (Menu.Item("arcane").GetValue<KeyBind>().Active)
            {
                var cursorpos = Game.CursorPos;
                Render.Circle.DrawCircle(cursorpos, 50, Color.Gold, 5);
                var targetpos = Drawing.WorldToScreen(cursorpos);
                Drawing.DrawText(targetpos[0] - 60, targetpos[1] + 20, Color.Gold, "Arcane Shift");
            }
        }

        static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if(target is Obj_AI_Hero)
            {
                if(Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                {
                    if (Q.CanCast((Obj_AI_Base)target))
                        Q.Cast((Obj_AI_Base)target);
                    else if (W.CanCast((Obj_AI_Base)target))
                            W.Cast((Obj_AI_Base)target);
                }
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                harass();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                Lasthit();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                LaneClear();
                JungleClear();
            }

            if(Menu.Item("arcane").GetValue<KeyBind>().Active)
            {
                var vec = Player.ServerPosition.Extend(Game.CursorPos, 475);
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                if(E.IsReady())
                    E.Cast(vec);
            }

            if(Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
                AutoQ();

            killsteal();
        }

        static void AutoQ()
        {
            if (!Menu.Item("harassAutoQ").GetValue<bool>())
                return;

            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, true);

            if (Q.CanCast(target) && Q.GetPrediction(target).Hitchance >= HitChance.VeryHigh)
                Q.Cast(target);
        }

        static void Combo()
        {
            if (!Orbwalking.CanMove(50))
                return;

            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, true);

            if (Q.CanCast(target) && Menu.Item("comboUseQ").GetValue<bool>())
            {
                var pred = Q.GetPrediction(target);

                if (pred.Hitchance >= HitChance.VeryHigh)
                    Q.Cast(target, Menu.Item("packet").GetValue<bool>());
            }

            if (W.CanCast(target) && Menu.Item("comboUseW").GetValue<bool>())
            {
                var pred = W.GetPrediction(target);
                
                if (pred.Hitchance >= HitChance.VeryHigh)
                    W.Cast(target, Menu.Item("packet").GetValue<bool>());
            }
        }

        static void harass()
        {
            if (!Orbwalking.CanMove(50))
                return;

            Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical, true);

            if (Q.CanCast(target) && Menu.Item("harassUseQ").GetValue<bool>())
            {
                var pred = Q.GetPrediction(target);

                if (pred.Hitchance >= HitChance.VeryHigh)
                    Q.Cast(target, Menu.Item("packet").GetValue<bool>());
            }
        }

        static void Lasthit()
        {
           
        }

        static void LaneClear()
        {
            if (!Orbwalking.CanMove(50))
                return;

            if (Q.IsReady() && Menu.Item("laneclearUseQ").GetValue<bool>())
            {
                var allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy);

                var locQ = Q.GetLineFarmLocation(allMinionsQ);

                if (locQ.MinionsHit >= 1)
                    Q.Cast(locQ.Position, Menu.Item("packet").GetValue<bool>());
            }
        }

        static void JungleClear()
        {
            if (!Orbwalking.CanMove(50))
                return;

            var mobs = MinionManager.GetMinions(Player.ServerPosition, Orbwalking.GetRealAutoAttackRange(Player) + 100, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0)
                return;

            if (Q.CanCast(mobs[0]) && Menu.Item("jungleclearUseQ").GetValue<bool>())
            {
                Q.Cast(Q.GetPrediction(mobs[0]).CastPosition, Menu.Item("packet").GetValue<bool>());
            }
        }

        static void killsteal()
        {
            if (!Menu.Item("killsteal").GetValue<bool>())
                return;

            foreach (Obj_AI_Hero target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(Q.Range) && x.IsEnemy && !x.IsDead && x.IsTargetable && !x.HasBuffOfType(BuffType.Invulnerability)))
            {
                if (target != null)
                {
                    if (Q.CanCast(target) && Q.GetDamage(target) >= target.Health + target.HPRegenRate && Q.GetPrediction(target).Hitchance >= HitChance.VeryHigh)
                        Q.Cast(target, Menu.Item("packet").GetValue<bool>());
                    else
                    if (W.CanCast(target) && W.GetDamage(target) >= target.Health + target.HPRegenRate && W.GetPrediction(target).Hitchance >= HitChance.VeryHigh)
                        W.Cast(target, Menu.Item("packet").GetValue<bool>());
                    else
                    if (E.CanCast(target) && E.GetDamage(target) >= target.Health + target.HPRegenRate)
                    {
                        var vec = Player.ServerPosition.Extend(target.ServerPosition, 475);//Towards the enemy
                        if (vec.CountEnemysInRange(750) == 1 && !vec.IsWall() && !vec.UnderTurret(true))//Safe and valid check (E Missile range = 1225)
                            E.Cast(vec, Menu.Item("packet").GetValue<bool>());
                    }
                    else
                    if (R.CanCast(target) && !target.IsValidTarget(800) && R.GetDamage(target) >= (target.Health + (target.HPRegenRate * 2)) && R.GetPrediction(target).Hitchance >= HitChance.VeryHigh)
                        R.Cast(target, Menu.Item("packet").GetValue<bool>());
                }
            }
        }

    }
}