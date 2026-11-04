using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class ItemInstance : INetworkSerializable, IEquatable<ItemInstance>
{
    public string itemId = "";
    public int amount = 0;
    public int durability = 0;
    public List<string> enchantments = new();

    public ItemInstance() { }

    public ItemInstance(string id, int amount = 1, int durability = 0)
    {
        this.itemId = id;
        this.amount = amount;
        this.durability = durability;
        this.enchantments = new List<string>();
    }

    public bool IsEmpty => string.IsNullOrEmpty(itemId) || amount <= 0;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref itemId);
        serializer.SerializeValue(ref amount);
        serializer.SerializeValue(ref durability);

        int count = enchantments != null ? enchantments.Count : 0;
        serializer.SerializeValue(ref count);
        if (serializer.IsReader)
        {
            enchantments = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                string ench = "";
                serializer.SerializeValue(ref ench);
                enchantments.Add(ench);
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                string ench = enchantments[i];
                serializer.SerializeValue(ref ench);
            }
        }
    }

    public bool Equals(ItemInstance other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return itemId == other.itemId && amount == other.amount && durability == other.durability;
    }

    public override bool Equals(object obj) => obj is ItemInstance other && Equals(other);

    public override int GetHashCode() => (itemId, amount, durability).GetHashCode();
}
