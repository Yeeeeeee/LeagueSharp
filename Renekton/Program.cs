using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using Color = System.Drawing.Color;

class Program
{
   
    private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }//The Player as an Object

    private static Orbwalking.Orbwalker Orbwalker;//Orbwalker

    private static Spell Q, W, E, R;//Spells

    private static Items.Item Hydra;//Items
    private static Items.Item Tiamat;

    private static Menu Menu;//Shift-Menu

    private static int ETime = 0;//Counter for how long E has been in Dice state

    private static Stopwatch stopwatch = new Stopwatch();//Stopwatch to get ETime


    static void Main(string[] args)
    {

        CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        Game.OnGameUpdate += Game_OnGameUpdate;
        Orbwalking.AfterAttack += Orbwalking_OnAfterAttack;
        Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
    }

    private static void Game_OnGameLoad(EventArgs args)
    {
        if (Player.ChampionName != "Renekton")//Exit if Champion is not Renekton
            return;

        Menu = new Menu("[" + Player.ChampionName + "]", Player.ChampionName, true);//Define Menu

        Q = new Spell(SpellSlot.Q, 225); //Spells
        W = new Spell(SpellSlot.W); 
        E = new Spell(SpellSlot.E, 450);
        R = new Spell(SpellSlot.R);

        E.SetSkillshot(0.25f, 80f, 1800f, false, SkillshotType.SkillshotLine);//Define E as a skillshot

        Hydra = new Items.Item((int)ItemId.Ravenous_Hydra_Melee_Only, 185);//Item values
        Tiamat = new Items.Item((int)ItemId.Tiamat_Melee_Only, 185);

        Menu orbwalkerMenu = Menu.AddSubMenu(new Menu("[Orbwalker]", "Orbwalker"));//Menu for Orbwalker

        Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);

        Menu ts = Menu.AddSubMenu(new Menu("[Target Selector]", "Target Selector"));//Menu for Target selector

        TargetSelector.AddToMenu(ts);

        Menu spellMenu = Menu.AddSubMenu(new Menu("[Modes]", "Modes"));//Mode menu (Combo, Clear, Harass, etc.
        Menu Rmenu = Menu.AddSubMenu(new Menu("[R]", "Rmenu"));//Menu for usage of R in sticky situations
        Rmenu.AddItem(new MenuItem("aR", "Auto R").SetValue(true));
        Rmenu.AddItem(new MenuItem("eR", "Ult if x Enemies Around").SetValue(new Slider(2, 1, 5)));
        Rmenu.AddItem(new MenuItem("pR", "Ult if x Health Percent").SetValue(new Slider(30, 1, 99)));
        Menu comboMenu = spellMenu.AddSubMenu(new Menu("[Combo]", "Combo"));//Combo Menu
        comboMenu.AddItem(new MenuItem("useE", "Use E").SetValue(true));
        comboMenu.AddItem(new MenuItem("rangeE", "Use E only if out of Range").SetValue(true));
        comboMenu.AddItem(new MenuItem("delayE2", "Delay E2 after E").SetValue(true));
        comboMenu.AddItem(new MenuItem("itemsCombo", "Use Items").SetValue(true));
        Menu harassMenu = spellMenu.AddSubMenu(new Menu("[Harass]", "Harass"));//Harass Menu
        harassMenu.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
        harassMenu.AddItem(new MenuItem("useE", "Use E").SetValue(true));
        harassMenu.AddItem(new MenuItem("returnE2", "Use E2 to go back").SetValue(true));
        Menu clearMenu = spellMenu.AddSubMenu(new Menu("[Lane Clear]", "Lane Clear"));//Clear Menu
        clearMenu.AddItem(new MenuItem("useQClear", "Use Q").SetValue(true));
        clearMenu.AddItem(new MenuItem("useEClear", "Use E").SetValue(true));
        Menu drawMenu = spellMenu.AddSubMenu(new Menu("[Drawings]", "Drawings"));//Draw Menu
        drawMenu.AddItem(new MenuItem("drawE", "Draw E Range").SetValue(true));
     

        Menu.AddToMainMenu();//Adds entire Menu to Shift call

        Drawing.OnDraw += Drawing_OnDraw;

        Notifications.AddNotification(new Notification("Renekton by OuO Loaded", 3000));//Adds startup Notification (Notifications By L33T <3)
    }

    private static void Game_OnGameUpdate(EventArgs args)//Happens a few times a second
    {

        if (Player.IsDead)//Stop calculations if I'm dead
        {
            return;
        }

        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)//Call Combo
        {
            combo();
        }
        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)//Call Harass
        {
            harass();
        }
        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)//Call LaneClear
        {
            clear();
        }

        if (Menu.Item("aR").GetValue<bool>() == true)//Check if you enabled the option in settings
        {
            if (Player.Health / Player.MaxHealth * 100 < Menu.Item("pR").GetValue<Slider>().Value && !Player.InFountain())//If health value below set percentage and I'm not in fountain => cast Ult (Animation cancelling soon(tm))
            {
                R.Cast();
            }
        }
        if (Menu.Item("aR").GetValue<bool>() == true)//Check if you enabled the option in settings
        {
            if(Player.CountEnemiesInRange(700) >= Menu.Item("eR").GetValue<Slider>().Value && !Player.InFountain()){//If there are more enemies than set in options and I'm not in fountaion => cast Ult
                R.Cast();
            }
        }
       
    }

    private static void Drawing_OnDraw(EventArgs args)//Using this to draw my stuff
    {
    
        if (Player.IsDead)//Don't want to draw while dead
            return;

        if (E.IsReady())
        {
            Render.Circle.DrawCircle(Player.Position, E.Range, Color.Green);//Draws green circle if I have E up
        }
        else
        {
            Render.Circle.DrawCircle(Player.Position, E.Range, Color.DarkRed);//Draws red circle if I don't have E up
        }
    }

    private static void combo() {
        Obj_AI_Hero target900 = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);//Selects a target in 900 Range to potentially engage on with a dash to a minion and another dash to the champion
        Obj_AI_Hero target = TargetSelector.GetTarget(450, TargetSelector.DamageType.Physical);//Selects a target in 450 Range to potentially engage on with a dash 
        AttackableUnit targetAU = TargetSelector.GetTarget(900, TargetSelector.DamageType.Physical);//Same, but as an AttackableUnit
        if (Menu.Item("useE").GetValue<bool>() && !isEDice() && (Player.Distance(targetAU) < E.Range + 100))//Check if you enabled the option in settings
        {
            castE(target.ServerPosition);//Dash to the target we selected if we have Slice but don't want anyone in 900 range
            TargetSelector.SetTarget(target);
        }
        else//If Distance is too large to cross with one dash
        {
            if (!isEDice())//If E is not dice, we can dash twice
            {
                var dic = generateEngageOverMinion();//Creates a Dictionary/HashMap of champions I can dash to by hitting a minion with E
                if (dic[target900] != null)//Check if the target can be dashed to
                {
                    castE(dic[target900].ServerPosition);//Casts E to the position of the minion that lets me dash to my TargetSelector target
                }
            }
            else {
                castE(target.ServerPosition);//If E is Dice, we can only dash once. We'll use it on the best target in 450 range.
            }
        }
        if (Player.CountEnemiesInRange(225) > 0)//If an enemy can be hit by Q, do it fgt
        {
             Q.Cast();
        }
        /*if(Player.Distance(Orbwalker.GetTarget()) < 200)//Use W if we can stun someone
        {
            W.Cast();
        }*/
        
        if (Menu.Item("delayE2").GetValue<bool>() && isEDice())//Check if you enabled the option in settings
        {
            if (stopwatch.ElapsedMilliseconds > 3000)//If 3 seconds have passed since using Slice, we are free to use Dice if we specified the delay in the settings
            {
                castE(target.ServerPosition);
                TargetSelector.SetTarget(target);
                stopwatch.Stop();//Stop counting time since last E
            }
            else
            {

            }
        }
        else {
            castE(target.ServerPosition);//Just cast E straightaway if we don't want to wait
        }
        if (Menu.Item("itemsCombo").GetValue<bool>())//Check if you enabled the option in settings
        {
            if (Player.CountEnemiesInRange(200) > 0)//Use all items if there are any targets in a reasonable range
            {
                Hydra.Cast();
                Tiamat.Cast();
            }


        }
    }

    private static void harass() {
        Obj_AI_Hero target = TargetSelector.GetTarget(450, TargetSelector.DamageType.Physical);//Select best target to harass
        SharpDX.Vector3 harassPos = Player.ServerPosition;//Save a position to dash back to
        if (Menu.Item("useE").GetValue<bool>() && !isEDice())//Check if you enabled the option in settings
        {
            castE(target.ServerPosition);//Dash to enemy if we have Slice & Dice
        }
        if (Q.IsReady() && Player.CountEnemiesInRange(225) > 0)//If we can hit him with it, let's use Q
        {
            Q.Cast();
            Tiamat.Cast();
            Hydra.Cast();
        }
        if(!Q.IsReady() && !W.IsReady())//If we have no spells left, let's go back
        {
            if (Menu.Item("returnE2").GetValue<bool>())//Only if we want that of course
            {
                castE(harassPos);
            }
            else {
                castE(target.ServerPosition);//Else just go in harder
            }
        }
        
    }

    private static void clear() {
        var minions = MinionManager.GetMinions(E.Range + Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);//Get our small minion database to farm.
        if (minions.Count == 0)//If we have no minions, we can't farm so end clear()
            return;
        if (Q.IsReady() && E.IsReady() && Menu.Item("useQClear").GetValue<bool>())//Check if you enabled the option in settings
        {
            E.Cast(Q.GetCircularFarmLocation(minions).Position);//Dash to the best position for Q if we have Q and want to LaneClear with Q
            Q.Cast();
            Tiamat.Cast();
            Hydra.Cast();
        }
        if (Menu.Item("useEClear").GetValue<bool>())//Check if you enabled the option in settings
        {
            E.Cast(E.GetLineFarmLocation(minions).Position);//No Q? No problem, use our E to farm if we want that
            Tiamat.Cast();
            Hydra.Cast();
        }
    }

    private static void Orbwalking_OnAfterAttack(AttackableUnit unit, AttackableUnit target)//[Trigger Warning: Trigger] Triggered after every attack
    {
        if (!unit.IsValid || !unit.IsMe)//We only want to register our attacks, throw out the rest
        {
            return;
        }
        if (!target.IsValid<Obj_AI_Hero>())//Check if I'm valid
        {
            return;
        }
        if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)//If we wanna Combo, register our attacks
        {
            Utility.DelayAction.Add(50, () => W.Cast());//Cast W 50 milliseconds after the attack
            Utility.DelayAction.Add(100, () => Orbwalking.ResetAutoAttackTimer());//Reset our AA timer because W resets it
            Tiamat.Cast();//W has a long animation, cancel it
            Hydra.Cast();
        }
    }

    private static Dictionary<Obj_AI_Hero, Obj_AI_Minion> generateEngageOverMinion()//Documentation coming soon(tm), too tired am
    {
        Dictionary<Obj_AI_Hero, Obj_AI_Minion> heroes = new Dictionary<Obj_AI_Hero,Obj_AI_Minion>();
        var tmpHeroes = HeroManager.Enemies;
        List<Obj_AI_Base> tmpMinions;
        var minions = MinionManager.GetMinions(E.Range-20, MinionTypes.All, MinionTeam.Enemy);
        foreach(Obj_AI_Hero h in tmpHeroes){
            tmpMinions = MinionManager.GetMinions(h.ServerPosition, E.Range-100, MinionTypes.All, MinionTeam.Enemy);
            foreach (Obj_AI_Minion m in tmpMinions) {
                if (minions.Contains(m))
                {
                    heroes.Add(h, m);
                }
            }
        }
        return heroes;
    }

    private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)//[Trigger Warning: Trigger] Triggered after every spell
    {
        if (!sender.IsEnemy && args.SData.Name == "RenektonSliceAndDice")//If it's us, and we used Slice, start our expiration watch
        {
            Game.PrintChat("Used e1");
            stopwatch.Start();
        }
        if (sender == Player && args.SData.Name == "renektondice")//If it's us, and we used Dice, stop our expiration watch
        {
            stopwatch.Stop();
        }
    }

    private static bool isEDice()//Check if our E is Dice at the moment
    {
        if(E.IsReady() && Q.Instance.Name == "renektondice"){
            return true;
        }
        return false;
    }

    private static void castE(SharpDX.Vector3 pos)//Unified E casting method, code was messy and duplicate at most points so it was removed
    {
        E.Cast(pos);
        /*if (Menu.Item("rangeE").GetValue<bool>())//Check if you enabled the option in settings
        {
            if (Player.Distance(pos, true) >= 200)
            {
                E.Cast(pos);
                if (isEDice())
                {
                    stopwatch.Stop();
                }
            }
        } else {
            E.Cast(pos);
            if(isEDice()){
                stopwatch.Stop();
            }
        }*/
    }
}