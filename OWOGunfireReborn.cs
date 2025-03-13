using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using BepInEx.Unity.IL2CPP;
using DYPublic;
using UnityEngine;

namespace OWO_GunfireReborn
{
    [BepInPlugin("org.bepinex.plugins.OWOGunfireReborn", "OWO GunfireReborn integration", "0.0.1")]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;
        public static OWOSkin owoSkin;
        public static bool chargeWeaponCanShoot = false;
        public static bool continueWeaponCanShoot = false;
        private bool gameStarted = false;

        public override void Load()
        {
            Log = base.Log;
            owoSkin = new OWOSkin();

            // delay patching
            SceneManager.sceneLoaded += (UnityAction<Scene, LoadSceneMode>)new Action<Scene, LoadSceneMode>(OnSceneLoaded);
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (gameStarted) return;
            // patch all functions
            var harmony = new Harmony("owo.patch.OWOGunfireReborn");
            harmony.PatchAll();
            Plugin.owoSkin.StopAllHapticFeedback();
            gameStarted = true;
        }

        public static string getHandSide(int weaponId)
        {
            //dog case - Ao Bai
            if (HeroAttackCtrl.HeroObj.playerProp.SID == 201)
            {
                NewPlayerObject heroObj = HeroAttackCtrl.HeroObj;
                return (weaponId == heroObj.PlayerCom.CurWeaponID) ? "R" : "L";
            }
            return "R";
        }
    }

    #region Attacks

    /**
     * Many different classes for guns, not a single parent one.
    */

    //[HarmonyPatch(typeof(ASBaseShoot), "OnReload")]
    //public class OWO_OnReload
    //{
    //    [HarmonyPostfix]
    //    public static void Postfix(ASBaseShoot __instance)
    //    {
    //        if (Plugin.owoSkin.suitDisabled || __instance == null)
    //        {
    //            return;
    //        }
    //        if (__instance.ReloadComponent.m_IsReload)
    //        {
    //            Plugin.owoSkin.Feel("Reload " + Plugin.getHandSide(__instance.ItemID));
    //        }
    //    }
    //}

    /**
     * pistols and derivatives
     */
    [HarmonyPatch(typeof(ASAutoShoot), "AttackOnce")]
    public class OWO_OnFireAutoShoot
    {
        [HarmonyPostfix]
        public static void Postfix(ASAutoShoot __instance)
        {
            if (Plugin.owoSkin.suitDisabled || __instance == null) return;

            Plugin.owoSkin.Feel("Recoil " + Plugin.getHandSide(__instance.ItemID), 2);
        }
    }

    /**
     * Single shot weapons (snipers, some bows) 
     */
    [HarmonyPatch(typeof(ASSingleShoot), "AttackOnce")]
    public class OWO_OnFireSingleShoot
    {
        [HarmonyPostfix]
        public static void Postfix(ASSingleShoot __instance)
        {
            if (Plugin.owoSkin.suitDisabled || __instance == null) return;

            Plugin.owoSkin.Feel("Recoil " + Plugin.getHandSide(__instance.ItemID), 2);
        }
    }

    /**
     * On secondary attack
     */
    [HarmonyPatch(typeof(HeroAttackCtrl), "OnSecRepeatingFire")]
    public class OnSecRepeatingFire
    {
        [HarmonyBefore]
        public static void Postfix(int weaponid)
        {
           if (Plugin.owoSkin.suitDisabled) return;
                Plugin.owoSkin.Feel("Recoil R", 2);
        } 
    }

        /**
         * testing for charged attack once 
         */
        [HarmonyPatch(typeof(ASSingleChargeShoot), "ClearChargeAttack")]
        public class OWO_OnFireSingleChargeShoot
        {
            [HarmonyPostfix]
            public static void Postfix(ASSingleChargeShoot __instance)
            {
                if (Plugin.owoSkin.suitDisabled || __instance == null) return;

                Plugin.owoSkin.Feel("Charge " + Plugin.getHandSide(__instance.ItemID), 2);
            }
        }

        /**
         * Charging weapons effect when charging
         */
        [HarmonyPatch(typeof(ASAutoChargeShoot), "ShootCanAttack")]
        public class OWO_OnFireAutoChargeShoot
        {
            [HarmonyPostfix]
            public static void Postfix(ASAutoChargeShoot __instance, bool __result)
            {
                if (Plugin.owoSkin.suitDisabled || __instance == null) return;

                if (__result)
                {
                    Plugin.chargeWeaponCanShoot = true;
                    Plugin.owoSkin.StartChargingWeapon(Plugin.getHandSide(__instance.ItemID) == "R");
                }
            }
        }

        /**
         * Charging weapons post charging release
         */
        [HarmonyPatch(typeof(ASAutoChargeShoot), "OnUp")]
        public class OWO_OnChargingRelease
        {
            [HarmonyPostfix]
            public static void Postfix(ASAutoChargeShoot __instance)
            {
                if (Plugin.owoSkin.suitDisabled || __instance == null) return;

                if (Plugin.chargeWeaponCanShoot)
                {
                    Plugin.chargeWeaponCanShoot = false;
                    //stop thread
                    Plugin.owoSkin.StopChargingWeapon(Plugin.getHandSide(__instance.ItemID) == "R");

                    Plugin.owoSkin.Feel("Charged Release " + Plugin.getHandSide(__instance.ItemID), 2);
                }
            }
        }

        /**
         * Continue shoot weapons when activating
         */
        [HarmonyPatch(typeof(ASContinueShoot), "StartBulletSkill")]
        public class OWO_OnContinueShoot
        {
            [HarmonyPostfix]
            public static void Postfix(ASContinueShoot __instance)
            {
                if (Plugin.owoSkin.suitDisabled || __instance == null) return;

                Plugin.continueWeaponCanShoot = true;
                //start thread
                Plugin.owoSkin.StartContinueWeapon(Plugin.getHandSide(__instance.ItemID) == "R");
            }
        }

        /**
         * Continue shoot weapons when stop firing
         */
        [HarmonyPatch(typeof(ASContinueShoot), "EndContinueAttack")]
        public class OWO_OnContinueStop
        {
            [HarmonyPostfix]
            public static void Postfix(ASContinueShoot __instance)
            {
                if (Plugin.owoSkin.suitDisabled || __instance == null) return;

                if (Plugin.continueWeaponCanShoot)
                {
                    Plugin.continueWeaponCanShoot = false;
                    //stop thread
                    Plugin.owoSkin.StopContinueWeapon(Plugin.getHandSide(__instance.ItemID) == "R");
                }
            }
        }

        /**
         * DownUpShoot (Wild hunt) arms and vest 
         * using ASBaseShoot.StartBulletSkill to cover only the wildhunt itemSID == 1306
         */
        [HarmonyPatch(typeof(ASBaseShoot), "StartBulletSkill")]
        public class OWO_OnFireDownUpShoot
        {
            [HarmonyPostfix]
            public static void Postfix(ASBaseShoot __instance)
            {
                if (Plugin.owoSkin.suitDisabled || __instance == null)
                {
                    return;
                }
                if (__instance.ItemSID == 1306)
                {
                    Plugin.owoSkin.Feel("Recoil " + Plugin.getHandSide(__instance.ItemID), 2);
                }
            }
        }

        // this method will activate feedback only when cloud weaver
        // transitions from 1 sword held in hand (inactive state/entering new
        // zones or switching to cloud weaver) to active state in which the 5
        // cloud weaver swords begin spinng around the wrist,
        // this can only be activated by initiating sword spinning, it will
        // not activate again until cloud weaver is inactive (new zone or switching) 
        [HarmonyPatch(typeof(ASFlyswordShoot), "StartBulletSkill")]
        public class OWO_OnFireFlySwordStart
        {
            [HarmonyPostfix]
            public static void Postfix(ASFlyswordShoot __instance)
            {
                if (Plugin.owoSkin.suitDisabled || __instance == null) return;

                Plugin.owoSkin.StartCloudWeaver(Plugin.getHandSide(__instance.ItemID) == "R");
            }
        }

        /**
         * LANCE : Might not be enough and not covering
         * when downed, when ui on (scrolls, pause menu, etc)
         */
        [HarmonyPatch(typeof(ASFlyswordShoot), "Destroy")]
        public class OWO_OnFireFlySwordStopHaptics
        {
            [HarmonyPostfix]
            public static void Postfix(ASFlyswordShoot __instance)
            {
                if (Plugin.owoSkin.suitDisabled || __instance == null) return;

                Plugin.owoSkin.StopCloudWeaver(Plugin.getHandSide(__instance.ItemID) == "R");
            }
        }

        // this method will activate feedback only while cloud weaver is
        // actively hitting enemies, does not activate from any button presses,
        // may be ideal to change from flyswordvest and flyswordarmwristspinning
        // to recoil variants
        [HarmonyPatch(typeof(ASFlyswordShoot), "FlyswordOnDown")]
        public class OWO_OnFireFlySwordOnDown
        {
            [HarmonyPostfix]
            public static void Postfix(ASFlyswordShoot __instance)
            {
                if (Plugin.owoSkin.suitDisabled || __instance == null)
                {
                    return;
                }
                Plugin.owoSkin.Feel("Recoil " + Plugin.getHandSide(__instance.ItemID), 2);
            }
        }

        /**
         * On switching weapons
         */
        //[HarmonyPatch(typeof(HeroAttackCtrl), "OnSwitchWeapon")]
        //public class OWO_OnSwitchWeapon
        //{
        //    [HarmonyBefore]
        //    public static void Prefix()
        //    {
        //        if (Plugin.owoSkin.suitDisabled)
        //        {
        //            return;
        //        }

        //        Plugin.owoSkin.StopAllHapticFeedback();
        //        Plugin.owoSkin.Feel("Weapon Swap");
        //    }
        //}
        #endregion

        #region Primary skills (furies)

        /**
         * triggering skill on Down
         */
        [HarmonyPatch(typeof(HeroAttackCtrl), "StartActiveSkills")]
        public class OWO_OnPrimarySkillOnDown
        {
            public static bool continuousPrimaryStart = false;
            public static int kasuniState = 0;

        public static bool activatedSkill = false;

        [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.owoSkin.suitDisabled)
                {
                    return;
                }

                activatedSkill = true;

                //heroIds switch cases
                // Cat    -  Crown Prince
                // Dog    -  Ao Bai
                // Falcon -  Qing Yan
                // Tiger  -  Lei Luo
                // Bunny  -  Tao
                // Turtle -  Qian Sui
                //-------------------- PAID DLCs
                // Monkey -  Xing Zhe
                // Fox    -  Li
                // Owl    -  Zi Xiao
                // Panda  -  Nona
                // Goat   -  Lyn
                // Squirrel - Momo  
                switch (HeroAttackCtrl.HeroObj.playerProp.SID)
                {
                    //dog - Ao Bai
                    case 201:
                        Plugin.owoSkin.Feel("Buff", 3);
                        break;
                    //cat - Crown Prince
                    case 205:
                        Plugin.owoSkin.Feel("Throw", 3);
                        break;

                    // monkey - Xing Zhe
                    case 214:
                        Plugin.owoSkin.Feel("Throw", 3);
                        break;

                    //falcon - Qing Yan
                    case 206:
                        //Plugin.owoSkin.Feel("Jump Kick");
                        break;

                    //tiger - Lei Luo
                    case 207:
                        //Plugin.owoSkin.Feel("PrimarySkillTigerVest", true, 4.0f);
                        Plugin.owoSkin.Feel("Buff", 3);
                        break;

                    //turtle - Qian Sui
                    case 213:
                        if (!continuousPrimaryStart)
                        {
                            continuousPrimaryStart = true;
                            //start effect
                            Plugin.owoSkin.Feel("Buff", 3);
                            Plugin.owoSkin.StartQianPrimarySkill();
                        }
                        break;

                    //fox - Li
                    case 215:
                        //activation + continuous
                        if (kasuniState == 0)
                        {
                            Plugin.owoSkin.StartLiPrimarySkill();
                            kasuniState = 1;
                            break;
                        }
                        //release
                        if (kasuniState == 1)
                        {
                            //stop effect
                            Plugin.owoSkin.StopLiPrimarySkill();
                            Plugin.owoSkin.Feel("Throw 2Hands", 3);
                            kasuniState = 0;
                            break;
                        }
                        break;

                    //rabbit
                    case 212:
                        if (!continuousPrimaryStart)
                        {
                            continuousPrimaryStart = true;
                            //start effect
                            Plugin.owoSkin.StartTaoPrimarySkill();
                        }
                        break;

                    default:
                        activatedSkill = false;
                        return;
                }
            }
        }

        /**
         * Stop primary skills continuous effects turtle Qian Sui
         */
        [HarmonyPatch(typeof(HeroAttackCtrl), "BreakPower")]
        public class OWO_OnSkillBreak
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.owoSkin.suitDisabled) return;

                OWO_OnPrimarySkillOnDown.continuousPrimaryStart = false;
                Plugin.owoSkin.StopTurtlePrimarySkill();
                OWO_OnPrimarySkillOnDown.kasuniState = 0;
                Plugin.owoSkin.StopLiPrimarySkill();
            }
        }

        /**
        * Stop primary skills continuous effects turtle
        */
        [HarmonyPatch(typeof(SkillBolt.Cartoon1200405), "Active")]
        public class OWO_OnSkillEnd
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.owoSkin.suitDisabled || HeroAttackCtrl.HeroObj.playerProp.SID != 213)
                {
                    return;
                }

                if (OWO_OnPrimarySkillOnDown.continuousPrimaryStart)
                {
                    OWO_OnPrimarySkillOnDown.continuousPrimaryStart = false;
                    //stop effect
                    Plugin.owoSkin.StopTurtlePrimarySkill();
                    Plugin.owoSkin.Feel("Buff", 3);
                }
            }
        }


        /**
        * Stop primary skills continuous effects bunny
        */
        [HarmonyPatch(typeof(UIScript.HeroSKillLogicBase), "CommonColdDown")]
        public class OWO_OnSkillEndBunny
        {
            [HarmonyPostfix]
            public static void Postfix(UIScript.HeroSKillLogicBase __instance)
            {
                if (Plugin.owoSkin.suitDisabled)
                {
                    return;
                }
                if (HeroAttackCtrl.HeroObj.playerProp.SID == 212)
                {
                    if (OWO_OnPrimarySkillOnDown.continuousPrimaryStart)
                    {
                        OWO_OnPrimarySkillOnDown.continuousPrimaryStart = false;
                        //stop effect
                        Plugin.owoSkin.StopTaoPrimarySkill();
                    }
                }
                if (HeroAttackCtrl.HeroObj.playerProp.SID == 201)
                {
                    Plugin.owoSkin.StopChargingWeapon(false);
                }
            }
        }

        /**
         * Secondary skill on Down
         */
        [HarmonyPatch(typeof(HeroAttackCtrl), "ReadyThrowGrenade")]
        public class OWO_OnSecondarySkillOnDown
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.owoSkin.suitDisabled || !WarPanelManager.instance.m_canThrowGrenade)
                {
                    return;
                }

                //heroIds switch cases
                // Cat    -  Crown Prince
                // Dog    -  Ao Bai
                // Falcon -  Qing Yan
                // Tiger  -  Lei Luo
                // Bunny  -  Tao
                // Turtle -  Qian Sui
                //-------------------- PAID DLCs
                // Monkey -  Xing Zhe
                // Fox    -  Li
                // Owl    -  Zi Xiao
                // Panda  -  Nona
                // Goat   -  Lyn
                // Squirrel - Momo 
                switch (HeroAttackCtrl.HeroObj.playerProp.SID)
                {
                    //monkey - Xing Zhe
                    case 214:
                        Plugin.owoSkin.Feel("Throw", 3);
                        break;

                    //falcon - Qing Yan
                    case 206:
                        Plugin.owoSkin.Feel("Tail Blow", 3);
                        break;

                    //tiger - Lei Luo
                    case 207:
                        Plugin.owoSkin.Feel("Throw", 3);
                        break;

                    //turtle - Qian
                    case 213:
                        Plugin.owoSkin.Feel("Throw", 3);
                        break;

                    //rabbit - Tao
                    case 212:
                        Plugin.owoSkin.Feel("Throw", 3);
                        break;

                    default:
                        return;
                }
            }
        }

        /**
         * Secondary skill
         */
        [HarmonyPatch(typeof(HeroAttackCtrl), "ThrowGrenade")]
        public class OWO_OnSecondarySkillOnUp
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.owoSkin.suitDisabled || !WarPanelManager.instance.m_canThrowGrenade)
                {
                    return;
                }

                //heroIds switch cases
                switch (HeroAttackCtrl.HeroObj.playerProp.SID)
                {
                    //cat
                    case 205:
                        Plugin.owoSkin.Feel("Throw", 3);
                        break;

                    //dog 
                    case 201:
                        Plugin.owoSkin.Feel("Throw", 3);
                        break;
                    // - Li
                    case 215:
                        Plugin.owoSkin.Feel("Throw", 3);
                        break;

                    default:
                        return;
                }
            }
        }

        #endregion

        #region Moves

        /**
        * OnJumping
        */
        [HarmonyPatch(typeof(HeroMoveState.HeroMoveMotor), "OnJump")]
        public class OWO_OnJumping
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.owoSkin.suitDisabled) return;
                Plugin.owoSkin.Feel("Jump", 2);
            }
        }

        /**
         * After jumps when touching floor
         */
        [HarmonyPatch(typeof(HeroMoveManager), "OnLand")]
        public class OWO_OnLanding
        {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.owoSkin.suitDisabled) return;
                if (HeroAttackCtrl.HeroObj.playerProp.SID == 206 && OWO_OnPrimarySkillOnDown.activatedSkill)
                {
                    Plugin.owoSkin.Feel("Jump Land", 1);
                    Plugin.owoSkin.Feel("Jump Kick", 3);
                }
                else
                {
                    Plugin.owoSkin.Feel("Jump Land", 1);
                }
            }
        
        }

        /**
         * On Dashing
         */
        [HarmonyPatch(typeof(SkillBolt.CAction1310), "Action")]
        public class OWO_OnDashing
        {
            [HarmonyPostfix]
            public static void Postfix(SkillBolt.CSkillBase skill)
            {
                if (Plugin.owoSkin.suitDisabled) return;

                if (SkillBolt.CServerArg.IsHeroCtrl(skill))
                {
                    Plugin.owoSkin.Feel("Dash", 3);
                }
            }
        }

        #endregion

        #region Health and shield

        /**
         * When Shield breaks
         */
        [HarmonyPatch(typeof(BoltBehavior.CAction46), "Action")]
        public class OWO_OnShieldBreak
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.owoSkin.suitDisabled) return;

                Plugin.owoSkin.Feel("Shield Break", 4);
            }
        }

        /**
         * When low health starts
         */
        [HarmonyPatch(typeof(HeroBeHitCtrl), "PlayLowHpAndShield")]
        public class OWO_OnLowHealthStart
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.owoSkin.suitDisabled) return;

                if (HeroBeHitCtrl.NearlyDeadAction != -1)
                {
                    Plugin.owoSkin.StartHeartBeat();
                }
            }
        }

        /**
         * When low hp stops
         */
        [HarmonyPatch(typeof(HeroBeHitCtrl), "DelLowHpAndShield")]
        public class OWO_OnLowHealthStop
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.owoSkin.suitDisabled) return;

                Plugin.owoSkin.StopHeartBeat();
            }
        }

        /**
         * Can't find hit transform object, using static class
         * as an gydrator or factory of some sort in the original code, ugly
         * 
         * Use this function as well for geros with armor instead of shield
         * 
         * Death effect
         */
        [HarmonyPatch(typeof(HeroBeHitCtrl), "HeroInjured")]
        public class OWO_OnInjured
        {
            [HarmonyPostfix]
            public static void Postfix(int attid, Transform atttran)
            {
                if (Plugin.owoSkin.suitDisabled) return;

                //Plugin.owoSkin.LOG($"HeroInjured: {attid}");
            
            if (attid != 0)
                {
                    Plugin.owoSkin.Feel("Impact",4);
                }
            }
        }

        /**
         * When player gives up after death
         */
        [HarmonyPatch(typeof(UIScript.PCResurgencePanel_Logic), "GiveUp")]
        public class OWO_OnGiveUp
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.owoSkin.suitDisabled) return;

                Plugin.owoSkin.StopHeartBeat();
            }
        }

        /**
         * When player is NOT back to life, stop heartbeat
         */
        [HarmonyPatch(typeof(SalvationManager), "AskEnterWatch")]
        public class OWO_OnNotRevived
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.owoSkin.suitDisabled) return;

                Plugin.owoSkin.StopHeartBeat();
            }
        }

        /**
         * When player is back to life, stop heartbeat
         */
        [HarmonyPatch(typeof(NewPlayerManager), "PlayerRelife")]
        public class OWO_OnBackToLife
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.owoSkin.suitDisabled) return;

                Plugin.owoSkin.StopHeartBeat();
            }
        }

        /**
         * When healing item used
         */
        [HarmonyPatch(typeof(BoltBehavior.CAction1069), "Action")]
        public class OWO_OnHealing
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.owoSkin.suitDisabled) return;

                Plugin.owoSkin.Feel("Heal", 1);
            }
        }

        [HarmonyPatch(typeof(BoltBehavior.CAction4), "Action")]
        public class OnPlayerDeath
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (Plugin.owoSkin.suitDisabled) return;

                Plugin.owoSkin.StopAllHapticFeedback();
                Plugin.owoSkin.Feel("Death", 5);
            }
        }

        #endregion

        #region bug fixes

        /**
         * When defeating boss, stop all continuous haptics
         */
        //[HarmonyPatch(typeof(UIScript.EffectPanel_logic), "DefeatBoss")]
        //public class OWO_OnBossDefeat
        //{
        //    [HarmonyPostfix]
        //    public static void Postfix()
        //    {
        //        if (Plugin.owoSkin.suitDisabled) return;

        //        Plugin.owoSkin.StopAllHapticFeedback();
        //    }
        //}

        #endregion


        //[HarmonyPatch(typeof(AttackSkillBase), "SetFire")]
        //public class SetFire
        //{
        //    private static int lastEquipedWeaponId;

        //    [HarmonyPostfix]
        //    public static void Postfix(AttackSkillBase __instance)
        //    {
        //        if (__instance.ItemID != lastEquipedWeaponId)
        //        {
        //            Plugin.owoSkin.LOG($"SetFire - WeaponID: {__instance.ItemSID}");
        //            lastEquipedWeaponId = __instance.ItemSID;
        //        }
        //    }
        //}    
}

