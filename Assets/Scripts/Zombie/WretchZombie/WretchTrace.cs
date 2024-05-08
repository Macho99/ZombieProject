﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class WretchTrace : WretchZombieState
{
	public WretchTrace(WretchZombie owner) : base(owner)
	{
	}

	public override void Enter()
	{

	}

	public override void Exit()
	{

	}

	public override void FixedUpdateNetwork()
	{
		owner.Trace(owner.TargetData.IsTargeting ? 3f : 1f, 60f, 2f, 2f);
	}

	public override void SetUp()
	{

	}

	public override void Transition()
	{
		if (owner.TargetData.IsTargeting == false)
		{
			if (owner.Agent.hasPath && owner.Agent.remainingDistance < 1f)
			{
				owner.Agent.ResetPath();
				ChangeState(BruteZombie.State.Idle);
				return;
			}
		}

		if (owner.AttackTargetMask.IsLayerInMask(owner.TargetData.Layer))
		{
			if (owner.PoisonTimer.ExpiredOrNotRunning(owner.Runner))
			{
				PoisonPrepare();
				return;
			}

			if(owner.TargetData.Distance < 3.5f && owner.TargetData.AbsAngle < 20f && owner.Anim.GetFloat("SpeedY") > 2f)
			{
				DashAttack();
				return;
			}
			else if(owner.TargetData.Distance < 2f && owner.TargetData.AbsAngle < 45f)
			{
				CloseAttack();
				return;
			}
			else if(owner.TargetData.Distance < 0.5f)
			{
				CloseAttack();
				return;
			}
		}
	}

	private void DashAttack()
	{
		Attack(2);
	}

	private void CloseAttack()
	{
		Attack(Random.Range(0, 2));
	}

	private void PoisonPrepare()
	{
		owner.SetAnimFloat("ActionShifter", Random.Range(15, 18));
		owner.SetAnimTrigger("Action");
		owner.AnimWaitStruct = new AnimWaitStruct("Action", WretchZombie.State.Trace.ToString(), updateAction: () =>
			{
				owner.Decelerate();
				owner.LookToward(owner.TargetData.Direction, 1f);
			},
			nextStateAction: PoisonAttack
			);
		ChangeState(WretchZombie.State.AnimWait);
	}

	private void PoisonAttack()
	{
		owner.SetAnimFloat("ActionShifter", Random.Range(3, 5));
		owner.SetAnimTrigger("Attack");
		owner.AnimWaitStruct = new AnimWaitStruct("Attack", WretchZombie.State.Trace.ToString(), updateAction: owner.Decelerate);
		ChangeState(WretchZombie.State.AnimWait);
	}

	private void Attack(int actionShifter)
	{
		owner.SetAnimFloat("ActionShifter", actionShifter);
		owner.SetAnimTrigger("Attack");
		owner.AnimWaitStruct = new AnimWaitStruct("Attack", WretchZombie.State.Trace.ToString(), updateAction: owner.Decelerate);
		ChangeState(WretchZombie.State.AnimWait);
	}
}