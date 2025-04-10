using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JLL.API;
using JsonReservedSlots.JsonTypes;
using JsonReservedSlots.Keybinds;
using Newtonsoft.Json;
using ReservedItemSlotCore.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JsonReservedSlots
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("FlipMods.ReservedItemSlotCore")]
    [BepInDependency("com.rune580.LethalCompanyInputUtils")]
    [BepInDependency("JacobG5.JLL")]
    public class JsonReservedSlotsCore : BaseUnityPlugin
    {
        private const string modGUID = "JacobG5.JsonReservedSlots";
        private const string modName = "JsonReservedSlots";
        private const string modVersion = "1.2.0";

        private readonly Harmony harmony = new(modGUID);

        public static ManualLogSource mls;

        public ConfigEntry<bool> createDefaults;

        public static readonly string JsonReservedSlotsPath = Path.Combine(Paths.ConfigPath, "JsonReservedSlots");

        private static bool loadedJsons = false;

        public static readonly List<ReservedSlotInfo> reservedSlotInfos = new();

        private static ReservedJsonKeybinds jsonKeybinds;

        void Awake()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            createDefaults = Config.Bind("Core", "CreateDefaults", true, "Creates an example reserved slot json for Belt bags when directory doesn't exist.");

            if (!Directory.Exists(JsonReservedSlotsPath))
            {
                mls.LogInfo("Generating Defaults.");
                CreateDefaultJsonSlots();
            }

            harmony.PatchAll(typeof(JsonReservedSlotsCore));
        }

        public void CreateDefaultJsonSlots()
        {
            Directory.CreateDirectory(JsonReservedSlotsPath);

            string playerBones = "";

            foreach (PlayerBone bone in Enum.GetValues(typeof(PlayerBone)))
            {
                playerBones += $"{bone}: {(int)bone}\n";
            }

            File.WriteAllText(JsonReservedSlotsPath + "\\PlayerBones.txt", playerBones);

            if (!createDefaults.Value)
            {
                return;
            }

            ReservedSlotInfo toolBagInfo = new() 
            { 
                reservedSlotName = "utility_belt", 
                displayName = "Belt Slot", 
                slotPriority = 50, 
                purchasePrice = 150, 
                itemsForSlot = [ new ReservedItemInfo() { itemName = "Belt bag" }], 
                equipKeybind = new ReservedKeybindInfo() { defaultBind = "b" }
            };

            string toolBagJson = JsonConvert.SerializeObject(toolBagInfo, Formatting.Indented);

            File.WriteAllText(JsonReservedSlotsPath + "\\UtilityBeltSlot.json", toolBagJson);
        }

        [HarmonyPatch(typeof(PreInitSceneScript), "Awake")]
        [HarmonyPostfix]
        private static void ReadJsonSlots()
        {
            if (loadedJsons) return;
            mls.LogInfo("Reading Json Files...");
            loadedJsons = true;
            try
            {
                foreach (string file in Directory.GetFiles(JsonReservedSlotsPath))
                {
                    if (file.EndsWith(".json"))
                    {
                        mls.LogInfo($"Reading {Path.GetFileName(file)}");
                        ReservedSlotInfo? slotInfo = JsonConvert.DeserializeObject<ReservedSlotInfo>(File.ReadAllText(file));
                        if (slotInfo != null)
                        {
                            if (slotInfo.reservedSlotName.IsNullOrWhiteSpace()) continue;
                            if (slotInfo.requiredMods.Length > 0 && slotInfo.requiredMods.FirstOrDefault((x) => JCompatabilityHelper.IsLoaded(x)) == null) continue;
                            if (slotInfo.displayName.IsNullOrWhiteSpace()) slotInfo.displayName = Path.GetFileNameWithoutExtension(file);
                            slotInfo.reservedSlotName = slotInfo.reservedSlotName.ToLower().Replace(' ', '_');

                            var itemInfos = slotInfo.itemsForSlot.ToList();
                            itemInfos.RemoveAll((itemInfo) => itemInfo.itemName.IsNullOrWhiteSpace());
                            slotInfo.itemsForSlot = itemInfos.ToArray();

                            reservedSlotInfos.Add(slotInfo);
                        }
                        else
                        {
                            mls.LogInfo($"{Path.GetFileName(file)} could not be parsed!");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                mls.LogWarning($"Something went wrong reading JSON files!\n{e}");
            }

            foreach (ReservedSlotInfo slotInfo in reservedSlotInfos)
            {
                ReservedItemSlotData slotData = ReservedItemSlotData.CreateReservedItemSlotData(slotInfo.reservedSlotName, slotInfo.slotPriority, slotInfo.purchasePrice);
                for (int i = 0; i < slotInfo.itemsForSlot.Length; i++)
                {
                    if (slotInfo.itemsForSlot[i].bone == PlayerBone.None)
                    {
                        slotData.AddItemToReservedItemSlot(new ReservedItemData(slotInfo.itemsForSlot[i].itemName));
                    }
                    else
                    {
                        slotData.AddItemToReservedItemSlot(new ReservedItemData(slotInfo.itemsForSlot[i].itemName, slotInfo.itemsForSlot[i].bone, slotInfo.itemsForSlot[i].position.GetUnityVector(), slotInfo.itemsForSlot[i].rotation.GetUnityVector()));
                    }
                }
            }

            jsonKeybinds = new ReservedJsonKeybinds();
            jsonKeybinds.BindKeys();
        }
    }
}
