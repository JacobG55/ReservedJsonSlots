﻿using Newtonsoft.Json;
using ReservedItemSlotCore.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JsonReservedSlots.JsonTypes
{
    public class ReservedSlotInfo
    {
        [JsonIgnore] public ReservedItemSlotData slotData;
        public string reservedSlotName;
        public string displayName = "";

        public int slotPriority = 500;
        public int purchasePrice = 120;

        public ReservedItemInfo[] itemsForSlot;

        [JsonIgnore] public InputAction useKeybindAction;
        public ReservedKeybindInfo? useKeybind = null;

        [JsonIgnore] public InputAction equipKeybindAction;
        public ReservedKeybindInfo? equipKeybind = null;
    }

    public class ReservedItemInfo
    {
        public string itemName;
        public PlayerBone bone = PlayerBone.None;
        public ReservedVector position = new();
        public ReservedVector rotation = new();
    }

    public class ReservedVector
    {
        public float x = 0;
        public float y = 0;
        public float z = 0;

        public Vector3 GetUnityVector()
        {
            return new Vector3 (x, y, z);
        }
    }

    public class ReservedKeybindInfo
    {
        public string defaultBind = "";
        public bool toggle = false;
        public bool repocket = false;
    }
}
