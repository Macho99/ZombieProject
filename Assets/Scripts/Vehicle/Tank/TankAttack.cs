﻿using Cinemachine;
using Fusion;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Windows;
using Random = UnityEngine.Random;

public class TankAttack : VehicleBehaviour
{
	const float MAX_DIST = 500f;

	[SerializeField] Transform turretTrans;
	[SerializeField] Transform barrelTrans;
	[SerializeField] Transform firePoint;
	[SerializeField] float turretRotSpeed = 40f;
	[SerializeField] float barrelRotSpeed = 20f;
	[SerializeField] AnimationCurve depressionCurve;
	[SerializeField] float elevationAngle = 25f;
	[SerializeField] GameObject fireVFX;
	[SerializeField] GameObject hitVFX;
	[SerializeField] float[] reloadTimes;
	[SerializeField] float intervalFireCooltime = 4f;
	[SerializeField] float fireRebound = 10000f;
	[SerializeField] TankAttackUI attackUIPrefab;
	[SerializeField] float finalAcc = 1f;
	[SerializeField] float minBodyAcc = 0.5f;
	[SerializeField] float maxBodyAcc = 3f;
	[SerializeField] float minTurretAcc = 0.5f;
	[SerializeField] float maxTurretAcc = 1f;
	[SerializeField] float aimingSpeed = 0.5f;
	[SerializeField] float velocityAccMul = 1f;
	[SerializeField] float turretRotAccMul = 0.05f;
	[SerializeField] float realAccMul = 0.05f;
	LayerMask hitMask;
	LayerMask damageableMask;
	int monsterLayer;
	int breakableLayer;

	TankAttackUI attackUI;
	Vector3 camOffset;
	bool spawned;
	int visualFireCnt;
	Rigidbody rb;

	Collider[] attackCols = new Collider[10];

	Vector3 targetPosition;
	float accuracy;
	float curTurretRotSpeed;
	TickTimer intervalFireTimer;

	[Networked, HideInInspector] public float CurBodyAcc { get; private set; }
	[Networked, HideInInspector] public float CurTurretAcc { get; private set; }
	[Networked, HideInInspector] public float TurretAngle { get; private set; }
	[Networked, HideInInspector] public float BarrelAngle { get; private set; }
	[Networked, HideInInspector] public int FireCnt { get; private set; }
	[Networked, HideInInspector] public Vector3 HitPosition { get; private set; }
	[Networked, HideInInspector] public Vector3 HitNormal { get; private set; }
	[Networked, HideInInspector] public int LoadedShell { get; private set; } = -1;
	[Networked, HideInInspector] public float LeftReloadTime { get; private set; } = 0f;

	protected override void Awake()
	{
		base.Awake();
		rb = GetComponentInParent<Rigidbody>();
		camOffset = new Vector3(0f, cam.transform.GetChild(0).transform.localPosition.y, 0f);
		hitMask = LayerMask.GetMask("Default", "Environment", "Breakable","Monster");
		damageableMask = LayerMask.GetMask("Breakable", "Monster");
		monsterLayer = LayerMask.NameToLayer("Monster");
		breakableLayer = LayerMask.NameToLayer("Breakable");
	}

	public override void Spawned()
	{
		base.Spawned();
		intervalFireTimer = TickTimer.CreateFromTicks(Runner, 1);
		if(Object.HasInputAuthority == false)
		{
			cam.gameObject.SetActive(false);
		}
		visualFireCnt = FireCnt;
		spawned = true;
	}

	public override void FixedUpdateNetwork()
	{
		base.FixedUpdateNetwork();
		if (GetInput(out TestInputData input) == false) return;

		Vector3 camForward = Quaternion.Euler(CamYAngle, CamXAngle, 0f) * Vector3.forward;

		SetTargetPos(camForward);
		CalcAccuracy();

		Vector3 fireForward = (targetPosition - barrelTrans.position).normalized;
		RotateTurret(fireForward);
		RotateBarrel(fireForward);
		ReloadShell();

		if (HasInputAuthority)
		{
			UpdateUI();
		}

		if (input.buttons.IsSet(Buttons.Fire) == true)
		{
			if(LoadedShell > 0)
			{
				Fire();
			}
		}
	}

	private void CalcAccuracy()
	{
		accuracy = CurBodyAcc + CurTurretAcc;

		CurBodyAcc -= Runner.DeltaTime * aimingSpeed;
		CurBodyAcc += rb.velocity.magnitude * Runner.DeltaTime * velocityAccMul;
		CurBodyAcc = Mathf.Clamp(CurBodyAcc, minBodyAcc, maxBodyAcc);

		CurTurretAcc -= Runner.DeltaTime * aimingSpeed;
		CurTurretAcc += Mathf.Abs(curTurretRotSpeed) * Runner.DeltaTime * turretRotAccMul;
		CurTurretAcc = Mathf.Clamp(CurTurretAcc, minTurretAcc, maxTurretAcc);
	}

	private void SetTargetPos(Vector3 camForward)
	{
		if(Physics.Raycast(cam.transform.position + camOffset, camForward, 
			out RaycastHit hitInfo, MAX_DIST, hitMask) == true)
		{
			targetPosition = hitInfo.point;
		}
	}

	public override void Render()
	{
		base.Render();
		Quaternion prevRot = turretTrans.localRotation;
		Quaternion nextRot = Quaternion.Lerp(prevRot, Quaternion.Euler(0f, TurretAngle, 0f), Time.deltaTime * 2f);
		turretTrans.localRotation = nextRot;

		float prevYAngle = prevRot.eulerAngles.y;
		float curYAngle = nextRot.eulerAngles.y;
		curTurretRotSpeed = Mathf.DeltaAngle(prevYAngle, curYAngle) / Time.deltaTime;

		barrelTrans.localRotation = Quaternion.Lerp(barrelTrans.localRotation, 
			Quaternion.Euler(BarrelAngle, 0f, 0f), Time.deltaTime * 2f);

		if(FireCnt != visualFireCnt)
		{
			GameManager.Resource.Instantiate(fireVFX, firePoint.position, firePoint.rotation, true);
			if(HitPosition != Vector3.zero)
			{
				GameManager.Resource.Instantiate(hitVFX, HitPosition, Quaternion.LookRotation(HitNormal), true);
			}
			visualFireCnt = FireCnt;
		}
	}

	private void ReloadShell()
	{
		if (LoadedShell > reloadTimes.Length) { return; }

		LeftReloadTime -= Runner.DeltaTime;
		if (LeftReloadTime < 0f)
		{
			LoadedShell++;
			if (LoadedShell < reloadTimes.Length)
			{
				LeftReloadTime = reloadTimes[LoadedShell];
			}
		}
	}


	private void UpdateUI()
	{
		print(LoadedShell);

		Vector3 curHitPos;
		if (Physics.Raycast(barrelTrans.position, barrelTrans.forward, out RaycastHit hitInfo, MAX_DIST, hitMask) == true)
		{
			curHitPos = hitInfo.point;
		}
		else
		{
			curHitPos = barrelTrans.position + barrelTrans.forward * MAX_DIST;
		}
		Vector3 screenPos = Camera.main.WorldToScreenPoint(curHitPos);
		attackUI?.UpdateAimUI(screenPos, accuracy);

		float largeTime;
		float smallTime;
		float barRatio;
		bool fireReady;

		//탄간 장전 중일때
		if(intervalFireTimer.ExpiredOrNotRunning(Runner) == false)
		{
			fireReady = false;
			if(LoadedShell == 0)
			{
				smallTime = LeftReloadTime;
				largeTime = LeftReloadTime;
				barRatio = largeTime / reloadTimes[0];
			}
			else
			{
				smallTime = LeftReloadTime;
				largeTime = intervalFireTimer.RemainingTime(Runner).Value;
				barRatio = largeTime / intervalFireCooltime;
			}
		}
		//탄간 장전 아닐때
		else
		{
			if(LoadedShell == 0)
			{
				fireReady = false;
				smallTime = LeftReloadTime;
				largeTime = LeftReloadTime;
				barRatio = largeTime / reloadTimes[0];
			}
			else if(LoadedShell == 1)
			{
				fireReady = true;
				smallTime = LeftReloadTime;
				largeTime = intervalFireCooltime;
				barRatio = 0f;
			}
			else
			{
				fireReady = true;
				smallTime = LeftReloadTime;
				largeTime = intervalFireCooltime;
				barRatio = 0f;
			}
		}

		attackUI?.UpdateReloadUI(smallTime, largeTime, barRatio, fireReady);
	}

	private void RotateTurret(Vector3 forward)
	{
		//transform.up을 법선벡터로 가지는 평면에 cam.forward를 투영
		Vector3 turretCamForward = forward - Vector3.Dot(boarder.transform.up, forward) * boarder.transform.up;
		float turretAngle = Vector3.SignedAngle(boarder.transform.forward, turretCamForward, boarder.transform.up);
		if (turretAngle < 0f)
		{
			turretAngle += 360f;
		}
		TurretAngle = Mathf.MoveTowardsAngle(TurretAngle, turretAngle, Runner.DeltaTime * turretRotSpeed);
	}

	private void RotateBarrel(Vector3 forward)
	{
		float depressionAngle = depressionCurve.Evaluate(Mathf.PingPong(TurretAngle, 180f) / 180f);
		Vector3 barrelCamForward = forward - Vector3.Dot(turretTrans.right, forward) * turretTrans.right;
		float barrelAngle = Vector3.SignedAngle(turretTrans.forward, barrelCamForward, turretTrans.right);
		barrelAngle = Mathf.Clamp(barrelAngle, -elevationAngle, -depressionAngle);
		if (barrelAngle < 0f)
		{
			barrelAngle += 360f;
		}
		BarrelAngle = Mathf.MoveTowardsAngle(BarrelAngle, barrelAngle, Runner.DeltaTime * barrelRotSpeed);
	}

	private void Fire()
	{
		LoadedShell--;
		LeftReloadTime = reloadTimes[LoadedShell];

		FireCnt++;
		intervalFireTimer = TickTimer.CreateFromSeconds(Runner, intervalFireCooltime);

		rb.AddForceAtPosition(-barrelTrans.forward * fireRebound, barrelTrans.position, ForceMode.Impulse);

		Random.InitState(Runner.Tick * unchecked((int)Object.Id.Raw));
		CurBodyAcc = maxBodyAcc;
		CurTurretAcc = maxTurretAcc;
		if (Physics.Raycast(firePoint.position, barrelTrans.forward + Random.insideUnitSphere * accuracy * realAccMul, 
			out RaycastHit hit, 200f, hitMask))
		{
			HitPosition = hit.point;
			HitNormal = hit.normal;
			int result = Physics.OverlapSphereNonAlloc(hit.point, 3f, attackCols, damageableMask);
			for(int i = 0; i < result; i++)
			{
				int layer = attackCols[i].gameObject.layer;
				if (layer == monsterLayer)
				{

				}
				else if(layer == breakableLayer)
				{
					attackCols[i].gameObject.GetComponent<BreakableObstacle>().
						ExplosionBreakRequest(5000f, hit.point);
				}
			}
		}
		else
		{
			HitPosition = Vector3.zero;
			HitNormal = Vector3.zero;
		}
	}

	protected override void OnAssign(TestPlayer player)
	{
		if (player.HasInputAuthority && Runner.IsForward)
		{
			attackUI = GameManager.UI.ShowSceneUI(attackUIPrefab);
			attackUI.Init(finalAcc);
		}
	}

	protected override void OnGetOff()
	{
		if (HasInputAuthority && Runner.IsForward)
		{
			GameManager.UI.CloseSceneUI(attackUI);
			attackUI = null;
		}
	}

	private void OnDrawGizmos()
	{
		if (spawned == true)
		{
			Gizmos.DrawWireSphere(HitPosition, 3f);
		}
	}
}