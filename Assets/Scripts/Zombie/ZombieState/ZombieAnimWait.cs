﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class ZombieAnimWait : ZombieState
{
	AnimWaitStruct waitStruct;
	bool animEntered;

	public ZombieAnimWait(Zombie owner) : base(owner)
	{
	}

	public override void Enter()
	{
		if (owner.AnimWaitStruct.HasValue == false)
		{
			Debug.LogError("AnimWaitStruct를 설정하세요");
			return;
		}

		animEntered = false;
		waitStruct = owner.AnimWaitStruct.Value;
		waitStruct.startAction?.Invoke();
		owner.AnimWaitStruct = null;
	}

	public override void Exit()
	{
		waitStruct.animEndAction?.Invoke();
	}

	public override void SetUp()
	{

	}

	public override void Transition()
	{
		if (animEntered == true)
		{
			if(owner.IsAnimName(waitStruct.animName) == false)
			{
				ChangeState(waitStruct.nextState);
			}
		}
	}

	public override void FixedUpdateNetwork()
	{
		waitStruct.updateAction?.Invoke();
		if(animEntered == true) { return; }


		if(owner.IsAnimName(waitStruct.animName) == true)
		{
			animEntered = true;
			waitStruct.animStartAction?.Invoke();
		}
	}
}