﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NetworkBaseState
{
	private NetworkStateMachine stateMachine;

	public void SetStateMachine(NetworkStateMachine stateMachine)
	{
		this.stateMachine = stateMachine;
		SetUp();
	}

	protected void ChangeState(string stateName)
	{
		stateMachine.ChangeState(stateName);
	}

	protected void ChangeState<T>(T stateType) where T : Enum
	{
		ChangeState(stateType.ToString());
	}

	protected Coroutine StartCoroutine(IEnumerator routine)
	{
		return stateMachine.StartCoroutine(routine);
	}

	protected void StopAllCoroutines()
	{
		stateMachine.StopAllCoroutines();
	}

	protected void StopCoroutine(Coroutine coroutine)
	{
		stateMachine.StopCoroutine(coroutine);
	}

	protected void print(object obj)
	{
		Debug.Log(obj);
	}

	public abstract void SetUp();
	public abstract void Enter();
	public abstract void Exit();
	public abstract void FixedUpdateNetwork();
	public abstract void Transition();
	public virtual void Render() { }
}