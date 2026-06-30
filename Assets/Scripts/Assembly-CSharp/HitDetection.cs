using System.Collections.Generic;
using UnityEngine;

public class HitDetection
{
	private static readonly int RPoolSize;

	private static readonly int HPoolInitSize;

	private static readonly int DefaultLayersMask;

	private static List<HitInfo>[] m_RPool;

	private static int m_RPoolIndex;

	private static List<HitInfo> m_HPool;

	private static int m_HPoolIndex;

	// Shared non-allocating buffer used by both RayCast and SphereCast.
	// Safe to share since neither method calls the other re-entrantly.
	// Size matches the original RPoolSize assumption of max hits per cast.
	private static RaycastHit[] m_RaycastBuffer;

	static HitDetection()
	{
		RPoolSize = 32;
		HPoolInitSize = 64;
		DefaultLayersMask = -5;
		m_RPool = new List<HitInfo>[RPoolSize];
		m_RPoolIndex = 0;
		for (int i = 0; i < RPoolSize; i++)
		{
			m_RPool[i] = new List<HitInfo>(24);
		}
		m_HPool = new List<HitInfo>(HPoolInitSize);
		m_HPoolIndex = 0;
		for (int j = 0; j < HPoolInitSize; j++)
		{
			m_HPool.Add(new HitInfo());
		}
		m_RaycastBuffer = new RaycastHit[RPoolSize];
	}

	public static List<HitInfo> RayCast(Vector3 Origin, Vector3 Direction, float Distance)
	{
		return RayCast(Origin, Direction, Distance, DefaultLayersMask);
	}

	public static List<HitInfo> RayCast(Vector3 Origin, Vector3 Direction, float Distance, int LayersMask)
	{
		HitInfo pooledHit = GetPooledHit();
		List<HitInfo> pooledResult = GetPooledResult();
		int hitCount = Physics.RaycastNonAlloc(Origin, Direction, m_RaycastBuffer, Distance, LayersMask);
		for (int i = 0; i < hitCount; i++)
		{
			RaycastHit raycastHit = (pooledHit.data = m_RaycastBuffer[i]);
			pooledHit.dummyCollider = null;
			pooledHit.dummyColliderCollection = raycastHit.collider.GetComponent<DummyColliderCollection>();
			if (pooledHit.dummyColliderCollection != null)
			{
				if (!pooledHit.dummyColliderCollection.VerifyRayCast(pooledHit, Origin, Direction, Distance))
				{
					continue;
				}
			}
			else
			{
				pooledHit.hitZone = raycastHit.collider.GetComponent<HitZone>();
			}
			pooledResult.Add(pooledHit);
			pooledHit = GetPooledHit();
		}
		if (pooledResult.Count > 1)
		{
			pooledResult.Sort(CompareHits);
		}
		return pooledResult;
	}

	public static List<HitInfo> SphereCast(Vector3 Origin, float Radius, Vector3 Direction, float Distance)
	{
		return SphereCast(Origin, Radius, Direction, Distance, DefaultLayersMask);
	}

	public static List<HitInfo> SphereCast(Vector3 Origin, float Radius, Vector3 Direction, float Distance, int LayersMask)
	{
		HitInfo pooledHit = GetPooledHit();
		List<HitInfo> pooledResult = GetPooledResult();
		int hitCount = Physics.SphereCastNonAlloc(Origin, Radius, Direction, m_RaycastBuffer, Distance, LayersMask);
		for (int i = 0; i < hitCount; i++)
		{
			RaycastHit raycastHit = (pooledHit.data = m_RaycastBuffer[i]);
			pooledHit.dummyCollider = null;
			pooledHit.dummyColliderCollection = raycastHit.collider.GetComponent<DummyColliderCollection>();
			if (pooledHit.dummyColliderCollection != null)
			{
				if (!pooledHit.dummyColliderCollection.VerifySphereCast(pooledHit, Origin, Radius, Direction, Distance))
				{
					continue;
				}
			}
			else
			{
				pooledHit.hitZone = raycastHit.collider.GetComponent<HitZone>();
			}
			pooledResult.Add(pooledHit);
			pooledHit = GetPooledHit();
		}
		if (pooledResult.Count > 1)
		{
			pooledResult.Sort(CompareHits);
		}
		return pooledResult;
	}

	public static void Update()
	{
		m_HPoolIndex = 0;
	}

	private static List<HitInfo> GetPooledResult()
	{
		List<HitInfo> list = m_RPool[m_RPoolIndex];
		m_RPoolIndex = (m_RPoolIndex + 1) % RPoolSize;
		list.Clear();
		return list;
	}

	private static HitInfo GetPooledHit()
	{
		if (m_HPoolIndex == m_HPool.Count)
		{
			int num = 32;
			while (num-- > 0)
			{
				m_HPool.Add(new HitInfo());
			}
		}
		return m_HPool[m_HPoolIndex++];
	}

	private static int CompareHits(HitInfo A, HitInfo B)
	{
		return A.data.distance.CompareTo(B.data.distance);
	}
}