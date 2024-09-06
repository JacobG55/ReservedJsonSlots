using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JsonReservedSlots.JsonTypes;
using Newtonsoft.Json;
using ReservedItemSlotCore.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace JsonReservedSlots
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("FlipMods.ReservedItemSlotCore")]
    public class JsonReservedSlotsCore : BaseUnityPlugin
    {
        private const string modGUID = "JacobG5.JsonReservedSlots";
        private const string modName = "JsonReservedSlots";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        public static JsonReservedSlotsCore Instance;

        public ManualLogSource mls;

        public static ConfigEntry<bool> createDefaults;

        public readonly string JsonReservedSlotsPath = $"{BepInEx.Paths.ConfigPath}\\JsonReservedSlots";

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            mls.LogInfo(JsonReservedSlotsPath);

            createDefaults = Config.Bind("Core", "CreateDefaults", true, "Creates an example reserved slot json for Belt bags when directory doesn't exist.");

            if (!Directory.Exists(JsonReservedSlotsPath))
            {
                mls.LogInfo("Generating Defaults.");
                CreateDefaultJsonSlots();
            }

            mls.LogInfo("Reading Json Files...");
            ReadJsonSlots();
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

            ReservedSlotInfo toolBagInfo = new() { reservedSlotName = "utility_belt", itemsForSlot = [ new ReservedItemInfo() { itemName = "Belt bag" }] };

            string toolBagJson = JsonConvert.SerializeObject(toolBagInfo, Formatting.Indented);

            File.WriteAllText(JsonReservedSlotsPath + "\\UtilityBeltSlot.json", toolBagJson);
        }

        public void ReadJsonSlots()
        {
            List<ReservedSlotInfo> reservedSlots = new();

            try
            {
                foreach (string file in Directory.GetFiles(JsonReservedSlotsPath))
                {
                    mls.LogInfo($"{file}");

                    if (file.EndsWith(".json"))
                    {
                        ReservedSlotInfo? slotInfo = JsonConvert.DeserializeObject<ReservedSlotInfo>(File.ReadAllText(file));
                        if (slotInfo != null)
                        {
                            reservedSlots.Add(slotInfo);
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

            foreach (ReservedSlotInfo slotInfo in reservedSlots)
            {
                ReservedItemSlotData slotData = ReservedItemSlotData.CreateReservedItemSlotData(slotInfo.reservedSlotName, slotInfo.slotPriority, slotInfo.purchasePrice);

                mls.LogInfo($"{slotInfo.itemsForSlot.Length}");

                for (int i = 0; i < slotInfo.itemsForSlot.Length; i++)
                {
                    mls.LogInfo($"{slotInfo.itemsForSlot[i].bone}");
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
        }
    }
}
