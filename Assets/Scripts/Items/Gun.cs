using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Gun : Weapon
{
    //���������� 
    [SerializeField] protected Transform muzzlePoint;
    [Networked] protected int currentAmmoCount { get; set; }
    protected Transform targetPoint;

    public virtual bool CanAttack()
    {
        if (currentAmmoCount <= 0)
            return false;


        return true;
    }
    public virtual void Reload()
    {
        //  currentAmmoCount = ((GunItemSO)itemInstance.ItemData).MaxAmmoCount;


    }
}
