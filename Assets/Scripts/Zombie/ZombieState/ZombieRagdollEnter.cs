﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ZombieRagdollEnter : ZombieState
{
	float elapsed;
	float exitTime;

	bool transition;

	public ZombieRagdollEnter(Zombie owner) : base(owner)
	{
	}

	public override void Enter()
	{
		transition = false;
		elapsed = 0f;
		exitTime = 0.8f;

		owner.transform.position = owner.Position;
		owner.transform.rotation = owner.Rotation;

		owner.Anim.enabled = false;
		//BoneTransform[] boneTransforms = Zombie.BoneTransDict[owner.Anim.GetCurrentAnimatorClipInfo(0)[0].clip.name.GetHashCode()];
		//for (int i = 0; i < owner.Bones.Length; i++)
		//{
		//	owner.Bones[i].localPosition = boneTransforms[i].localPosition;
		//	owner.Bones[i].localRotation = boneTransforms[i].localRotation;
		//}
		owner.SetRbKinematic(false);
		Rigidbody rb = owner.BodyHitParts[(int)owner.RagdollBody].rb;
		rb.AddForce(owner.RagdollVelocity * rb.mass, ForceMode.Impulse);
	}

	public override void Exit()
	{
	}

	public override void FixedUpdateNetwork()
	{
		elapsed += owner.Runner.DeltaTime;
		if(transition == false)
			owner.CurRagdollState = RagdollState.Ragdoll;
	}

	public override void SetUp()
	{

	}

	public override void Transition()
	{
		if (transition == true) return;

		if(elapsed > exitTime)
		{
			if (owner.Hips.up.y > 0f)
			{
				if (owner.CurLegHp > 0)
					owner.CurRagdollState = RagdollState.FaceUpStand;
				else
					owner.CurRagdollState = RagdollState.FaceUpCrawl;
			}
			else
			{
				if (owner.CurLegHp > 0)
					owner.CurRagdollState = RagdollState.FaceDownStand;
				else
					owner.CurRagdollState = RagdollState.FaceDownCrawl;
			}
			transition = true;
		}
	}
}