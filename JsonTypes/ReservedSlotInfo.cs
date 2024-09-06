using ReservedItemSlotCore.Data;
using UnityEngine;

namespace JsonReservedSlots.JsonTypes
{
    public class ReservedSlotInfo
    {
        public string reservedSlotName;

        public int slotPriority = 500;
        public int purchasePrice = 120;

        public ReservedItemInfo[] itemsForSlot;
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
}
