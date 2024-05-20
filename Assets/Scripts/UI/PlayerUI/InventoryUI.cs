using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private ItemSlotUI itemSlotUIPrefab;
    [SerializeField] private RectTransform slotRoot;
    [SerializeField] private Image weightFillAmountImg;
    private ItemSlotUI[] slots;
    private Inventory inventory;
    private Equipment equipment;

    public void Init(Inventory inventory, Equipment equipment)
    {
        this.inventory = inventory;
        this.equipment = equipment;
        slots = new ItemSlotUI[inventory.MaxCount];
        for (int i = 0; i < slots.Length; i++)
        {
            ItemSlotUI itemSlotUI = Instantiate(this.itemSlotUIPrefab, slotRoot.transform);
            slots[i] = itemSlotUI;

            slots[i].Init(ItemSlotType.Dynamic, i);
            itemSlotUI.onItemRightClick += OnItemEquipment;
            itemSlotUI.gameObject.SetActive(false);

        }
        inventory.onItemUpdate += UpdateInventorySlot;
    }
    public void UpdateInventorySlot(int index, Item itemInstance)
    {
        slots[index].SetItem(itemInstance);
        weightFillAmountImg.fillAmount = inventory.Weight / inventory.MaxWeight;
    }

    public void OnItemEquipment(int index)
    {
        Item item = inventory.GetItem(index);
        if (item is UseItem)
        {
            inventory.UseItem(index);
        }
        else if (item is EquipmentItem)
        {
            equipment.RPC_Equipment(index, item);
        }
    }



}