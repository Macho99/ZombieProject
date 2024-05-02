using Fusion;
using RayFire;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

public class ShatterObstacle : BreakableObstacle
{
	const float fadeWaitDuration = 3f;
	const float fadeDuration = 5f;
	public enum ColliderType { Mesh, Box }

	[SerializeField] public int fragmentCnt = 5;
	[SerializeField] public ColliderType colType = ColliderType.Mesh;
	[SerializeField] public string cacheName;
	[SerializeField] public DebrisRoot debrisRootPrefab;
	public const string cachePath = "Assets/Resources/RayfireCache";
	public const string resourcePath = "RayfireCache";

	public MeshFilter MeshFilter { get { return meshFilter; } }

	DebrisRoot debrisRoot;
	float curScale;

	protected override void OnValidate()
	{
		base.OnValidate();
		if(cacheName.IsNullOrEmpty())
		{
			cacheName = $"{meshFilter.sharedMesh.name}";
		}
		if (debrisRootPrefab == null)
		{
			debrisRootPrefab = Resources.Load<DebrisRoot>($"{resourcePath}/{cacheName}");
			if(debrisRootPrefab == null)
			{
				print($"{cacheName} 프리팹을 캐시해주세요");
			}
		}
	}

	private IEnumerator CoFade()
	{
		yield return new WaitForSeconds(fadeWaitDuration);
		curScale = 0.9f;

		while (true)
		{
			float nextScale = curScale - Time.deltaTime / fadeDuration;
			if(nextScale < 0)
			{
				break;
			}

			debrisRoot.SetChildrenScale(nextScale);
			curScale = nextScale;
			yield return null;
		}

		Destroy(debrisRoot);
		debrisRoot = null;
	}

	protected override void Break(bool immediately = false)
	{
		base.Break(immediately);
		foreach(MeshRenderer renderer in childRenderers)
		{
			renderer.enabled = false;
		}
		foreach(Collider col in childCols)
		{
			col.enabled = false;
		}

		if(immediately == false)
		{
			debrisRoot = Instantiate(debrisRootPrefab, transform.position, transform.rotation, transform);
			StartCoroutine(CoFade());
		}
	}

	public override void BreakEffect(BreakableObjBehaviour.BreakData breakData)
	{
		if(debrisRoot == null)
		{
			Debug.LogError("debrisRoot가 만들어지기 전에 BreakEffect가 호출됨");
			return;
		}

		if(breakData.type == BreakableObjBehaviour.BreakType.AddForce)
		{
			debrisRoot.AddForceAtPosition(breakData.velocityOrForceAndRadius, breakData.position);
		}
		else if(breakData.type == BreakableObjBehaviour.BreakType.Explosion)
		{
			debrisRoot.AddExplosionForce(breakData.velocityOrForceAndRadius.x, 
				breakData.position, breakData.velocityOrForceAndRadius.y);
		}
	}
}
