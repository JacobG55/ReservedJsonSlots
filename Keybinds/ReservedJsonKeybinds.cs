using BepInEx;
using GameNetcodeStuff;
using HarmonyLib;
using JsonReservedSlots.JsonTypes;
using LethalCompanyInputUtils.Api;
using ReservedItemSlotCore;
using ReservedItemSlotCore.Data;
using System;
using UnityEngine.InputSystem;

namespace JsonReservedSlots.Keybinds
{
    internal class ReservedJsonKeybinds : LcInputActions
    {
        public override void CreateInputActions(in InputActionMapBuilder builder)
        {
            foreach (ReservedSlotInfo slotInfo in JsonReservedSlotsCore.Instance.reservedSlotInfos)
            {
                if (slotInfo.useKeybind != null)
                {
                    Bind(builder, "Use", slotInfo, slotInfo.useKeybind);
                }
                if (slotInfo.equipKeybind != null)
                {
                    Bind(builder, "Equip", slotInfo, slotInfo.equipKeybind);
                }
            }
        }

        public void BindKeys()
        {
            foreach (ReservedSlotInfo slotInfo in JsonReservedSlotsCore.Instance.reservedSlotInfos)
            {
                if (slotInfo.useKeybind != null)
                {
                    slotInfo.useKeybindAction = Asset[GetBindId(slotInfo, "Use")];

                    if (slotInfo.useKeybind.toggle)
                    {
                        slotInfo.useKeybindAction.performed += (context) => ToggleUse(slotInfo, context);
                    }
                    else
                    {
                        slotInfo.useKeybindAction.performed += (context) => ActivateUse(slotInfo, context, true);
                        slotInfo.useKeybindAction.canceled += (context) => ActivateUse(slotInfo, context, false);
                    }
                }
                if (slotInfo.equipKeybind != null)
                {
                    slotInfo.equipKeybindAction = Asset[GetBindId(slotInfo, "Equip")];

                    if (slotInfo.equipKeybind.toggle)
                    {
                        slotInfo.equipKeybindAction.performed += (context) => ToggleEquip(slotInfo, context);
                    }
                    else
                    {
                        slotInfo.equipKeybindAction.performed += (context) => ToggleEquip(slotInfo, context);
                        slotInfo.equipKeybindAction.canceled += (context) => ToggleEquip(slotInfo, context, false);
                    }
                }
            }
        }

        private void Bind(InputActionMapBuilder builder, string name, ReservedSlotInfo slotInfo, ReservedKeybindInfo bindInfo)
        {
            string id = GetBindId(slotInfo, name);
            JsonReservedSlotsCore.Instance.mls.LogInfo($"Creating {name} Keybind for {slotInfo.displayName} (ID: {id})");
            var useBind = builder.NewActionBinding();
            useBind.WithActionId(id).WithActionType(InputActionType.Button).WithBindingName($"{name} {slotInfo.displayName}");

            if (bindInfo.defaultBind.IsNullOrWhiteSpace()) useBind.WithKbmPathUnbound();
            else useBind.WithKbmPath($"<Keyboard>/{bindInfo.defaultBind}");

            useBind.WithGamepadPathUnbound().Finish();
        }

        private string GetBindId(ReservedSlotInfo slotInfo, string name) => $"reserved_json_{slotInfo.reservedSlotName}_{name.ToLower()}";

        private static bool KeybindsDisabled(PlayerControllerB localPlayer)
            => localPlayer == null || !localPlayer || !localPlayer.isPlayerControlled || (localPlayer.IsServer && !localPlayer.isHostPlayerObject) || localPlayer.isTypingChat || localPlayer.quickMenuManager.isMenuOpen 
            || localPlayer.isPlayerDead || ShipBuildModeManager.Instance.InBuildMode || ReservedPlayerData.localPlayerData.timeSinceSwitchingSlots < 0.08f;

        public static GrabbableObject? GetActiveOrSlotItem(PlayerControllerB player, ReservedSlotInfo slotInfo)
        {
            GrabbableObject? item = player.currentlyHeldObjectServer;

            if (item != null && item.itemProperties != null)
            {
                foreach (ReservedItemInfo itemInfo in slotInfo.itemsForSlot)
                {
                    if (item.itemProperties.itemName.Equals(itemInfo.itemName, StringComparison.InvariantCultureIgnoreCase)) return item;
                }
            }

            return GetSlotItem(player, slotInfo);
        }

        public static GrabbableObject? GetSlotItem(PlayerControllerB player, ReservedSlotInfo slotInfo)
        {
            if (SessionManager.TryGetUnlockedItemSlotData(slotInfo.reservedSlotName, out ReservedItemSlotData itemSlot) && ReservedPlayerData.allPlayerData.TryGetValue(player, out ReservedPlayerData playerData))
            {
                return playerData.GetReservedItem(itemSlot);
            }
            return null;
        }

        private static void ActivateUse(ReservedSlotInfo slotInfo, InputAction.CallbackContext context, bool active)
        {
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            if ((active ? !context.performed : !context.canceled) || KeybindsDisabled(localPlayer)) return;

            GrabbableObject? targetItem = GetActiveOrSlotItem(localPlayer, slotInfo);

            if (targetItem != null)
            {
                TriggerItem(localPlayer, slotInfo, targetItem, active);
            }
        }

        private static void ToggleUse(ReservedSlotInfo slotInfo, InputAction.CallbackContext context)
        {
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            if (!context.performed || KeybindsDisabled(localPlayer)) return;

            GrabbableObject? targetItem = GetActiveOrSlotItem(localPlayer, slotInfo);

            if (targetItem != null)
            {
                TriggerItem(localPlayer, slotInfo, targetItem, !targetItem.isBeingUsed);
            }
        }

        private static void TriggerItem(PlayerControllerB player, ReservedSlotInfo slotInfo, GrabbableObject item, bool active)
        {
            //JsonReservedSlotsCore.Instance.mls.LogInfo($"Toggle use {slotInfo.displayName} {active} {(player.currentlyHeldObjectServer == item ? "Held" : "Pocketed")}");

            item.UseItemOnClient(active);
            Traverse.Create(player).Field("timeSinceSwitchingSlots").SetValue(0);
            if (slotInfo.useKeybind != null && slotInfo.useKeybind.repocket) item.PocketItem();
        }

        private static void ToggleEquip(ReservedSlotInfo slotInfo, InputAction.CallbackContext context, bool performed = true)
        {
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            if ((performed ? !context.performed : !context.canceled) || KeybindsDisabled(localPlayer)) return;

            if (SessionManager.TryGetUnlockedItemSlotData(slotInfo.reservedSlotName, out ReservedItemSlotData itemSlot))
            {
                ReservedHotbarManager.ForceToggleReservedHotbar([itemSlot]);
            }
        }
    }
}
