using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EquipmentItem : Item
{

    [Networked, HideInInspector] public PlayerController owner { get; protected set; }

    public virtual void Equip(PlayerController owner)
    {
        this.owner = owner;

    }
    public virtual void UnEquip()
    {
        owner = null;
    }

}