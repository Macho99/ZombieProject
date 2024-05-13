using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimEvent : MonoBehaviour
{
    public event Action onStartDrawWeapon;
    public event Action onEndDrawWeapon;
    public event Action onStartPutWeapon;
    public event Action onEndPutWeapon;
    public void OnStartDrawWeapon()
    {
        // equipment;

        onStartDrawWeapon?.Invoke();
    }
    public void OnEndDrawWeapon()
    {
        onEndDrawWeapon?.Invoke();

        onStartDrawWeapon = null;
        onEndDrawWeapon = null;
    }
    public void OnStartPutWeapon()
    {
        onStartPutWeapon?.Invoke();
        Debug.Log("star!!");
    }
    public void OnEndPutWeapon()
    {
        onEndPutWeapon?.Invoke();
        Debug.Log("event");
        onStartPutWeapon = null;
        onEndPutWeapon = null;
    }
}
