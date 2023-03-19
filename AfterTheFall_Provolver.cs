using System;
using System.IO;
using System.Text;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using Newtonsoft.Json;
using Vertigo.Snowbreed.Client;

namespace AfterTheFall_Provolver
{
    [BepInPlugin("org.bepinex.plugins.AfterTheFall_Provolver", "AfterTheFall_Provolver integration", "1.0")]
    public class AfterTheFall_Provolver : BepInEx.IL2CPP.BasePlugin
    {
        public static string configPath = Directory.GetCurrentDirectory() + "\\BepInEx\\plugins\\Provolver";
        public static bool dualWield = false;
        internal static new ManualLogSource Log;

        public override void Load()
        {
            Log = base.Log;
            InitializeProTube();
            // patch all functions
            var harmony = new Harmony("protube.patch.afterthefall");
            harmony.PatchAll();
        }

        public static void saveChannel(string channelName, string proTubeName)
        {
            string fileName = configPath + channelName + ".pro";
            File.WriteAllText(fileName, proTubeName, Encoding.UTF8);
        }

        public static string readChannel(string channelName)
        {
            string fileName = configPath + channelName + ".pro";
            if (!File.Exists(fileName)) return "";
            return File.ReadAllText(fileName, Encoding.UTF8);
        }
        
        public static void dualWieldSort()
        {
            ForceTubeVRInterface.FTChannelFile myChannels = JsonConvert.DeserializeObject<ForceTubeVRInterface.FTChannelFile>(ForceTubeVRInterface.ListChannels());
            var pistol1 = myChannels.channels.pistol1;
            var pistol2 = myChannels.channels.pistol2;
            if ((pistol1.Count > 0) && (pistol2.Count > 0))
            {
                dualWield = true;
                Log?.LogMessage("Two ProTube devices detected, player is dual wielding.");
                if ((readChannel("rightHand") == "") || (readChannel("leftHand") == ""))
                {
                    Log?.LogMessage("No configuration files found, saving current right and left hand pistols.");
                    saveChannel("rightHand", pistol1[0].name);
                    saveChannel("leftHand", pistol2[0].name);
                }
                else
                {
                    string rightHand = readChannel("rightHand");
                    string leftHand = readChannel("leftHand");
                    Log?.LogMessage("Found and loaded configuration. Right hand: " + rightHand + ", Left hand: " + leftHand);
                    // Channels 4 and 5 are ForceTubeVRChannel.pistol1 and pistol2
                    ForceTubeVRInterface.ClearChannel(4);
                    ForceTubeVRInterface.ClearChannel(5);
                    ForceTubeVRInterface.AddToChannel(4, rightHand);
                    ForceTubeVRInterface.AddToChannel(5, leftHand);
                }
            }
            else
            {
                Log.LogMessage("SINGLE WIELD");
            }
        }

        private async void InitializeProTube()
        {
            Log?.LogMessage("Initializing ProTube gear...");
            await ForceTubeVRInterface.InitAsync(true);
            Thread.Sleep(10000);
            dualWieldSort();
        }

        [HarmonyPatch(typeof(Gun), "FireBullet", new System.Type[] { typeof(bool), typeof(bool) })]
        public class protube_Fire
        {
            [HarmonyPostfix]
            public static void Postfix(Gun __instance)
            {
                if (!__instance.IsEquippedLocally)
                {
                    return;
                }

                bool isRight = (__instance.MainHandSide == Vertigo.VR.EHandSide.Right);
                ForceTubeVRChannel myChannel = (isRight)
                    ?
                    ForceTubeVRChannel.pistol1
                    :
                    ForceTubeVRChannel.pistol2;

                byte kickPower = 210;
                switch (__instance.GunData.Name)
                {
                    case "Pump Shotgun":
                        ForceTubeVRInterface.Shoot(220, 200, 100f, myChannel);
                        return;
                    case "Auto-Shotgun":
                        ForceTubeVRInterface.Shoot(220, 200, 100f, myChannel);
                        return;
                    case "MT-47":
                        kickPower = 200;
                        break;
                    case "Tommy Gun":
                        kickPower = 200;
                        break;
                    case "Arizona Falcon":
                        kickPower = 230;
                        break;
                    case "SMG":
                        kickPower = 200;
                        break;
                    case "LMG":
                        kickPower = 220;
                        break;
                    case "Revolver":
                        kickPower = 230;
                        break;
                    case "Service Pistol":
                        kickPower = 200;
                        break;
                    case "Assault Carbine":
                        kickPower = 200;
                        break;
                    default:
                        kickPower = 210;
                        break;
                }
                ForceTubeVRInterface.Kick(kickPower, myChannel);
            }
        }

        [HarmonyPatch(typeof(GunAmmoInserter), "HandleAmmoInsertedEvent")]
        public class protube_Reloading
        {
            [HarmonyPostfix]
            public static void Postfix(GunAmmoInserter __instance)
            {
                if (!__instance.gun.IsEquippedLocally)
                {
                    return;
                }

                bool isRight = (__instance.gun.MainHandSide == Vertigo.VR.EHandSide.Right);
                ForceTubeVRChannel myChannel = (isRight)
                    ?
                    ForceTubeVRChannel.pistol1
                    :
                    ForceTubeVRChannel.pistol2;

                ForceTubeVRInterface.Rumble(126, 20f, myChannel);
            }
        }
        
        [HarmonyPatch(typeof(GunAmmoInserter), "HandleMagInserterHandleFullyInsertedEvent")]
        public class protube_Reloaded
        {
            [HarmonyPostfix]
            public static void Postfix(GunAmmoInserter __instance)
            {
                if (!__instance.gun.IsEquippedLocally)
                {
                    return;
                }

                bool isRight = (__instance.gun.MainHandSide == Vertigo.VR.EHandSide.Right);
                ForceTubeVRChannel myChannel = (isRight)
                    ?
                    ForceTubeVRChannel.pistol1
                    :
                    ForceTubeVRChannel.pistol2;

                ForceTubeVRInterface.Rumble(180, 20f, myChannel);
            }
        }
        
        [HarmonyPatch(typeof(Gun), "OnMagazineEjected")]
        public class protube_EjectMagazine
        {
            [HarmonyPostfix]
            public static void Postfix(Gun __instance)
            {
                if (!__instance.IsEquippedLocally)
                {
                    return;
                }

                bool isRight = (__instance.MainHandSide == Vertigo.VR.EHandSide.Right);
                ForceTubeVRChannel myChannel = (isRight)
                    ?
                    ForceTubeVRChannel.pistol1
                    :
                    ForceTubeVRChannel.pistol2;

                ForceTubeVRInterface.Rumble(126, 20f, myChannel);
            }
        }
    }
}

