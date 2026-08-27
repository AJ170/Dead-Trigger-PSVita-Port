using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;

public static class HitUtils
{
	public struct HitData
	{
		public Vector3 hitPos;

		public GameObject hitObj;

		public float distance;
	}

	public delegate bool IsProperHit(RaycastHit hit, HitZone zoneHit, bool checkAlsoDead = false);

	public static bool FirstCollisionOnRay(Ray ray, float distance, GameObject ignoreGO, IsProperHit Judge, out HitData data)
	{
		List<HitInfo> list = HitDetection.RayCast(ray.origin, ray.direction, distance);
		bool flag = false;
		data.hitObj = null;
		data.distance = 100000000f;
		data.hitPos = Vector3.zero;
		foreach (HitInfo item in list)
		{
			RaycastHit data2 = item.data;
			GameObject gameObject = data2.transform.gameObject;
			if (!(gameObject == ignoreGO) && !data2.collider.isTrigger && (data.hitObj == null || data.distance > data2.distance))
			{
				flag = Judge(data2, item.hitZone);
				data.hitPos = data2.point;
				data.hitObj = gameObject;
				data.distance = data2.distance;
			}
		}
		if (data.hitObj == null || !flag)
		{
			return false;
		}
		return true;
	}

	public static bool SphereCollisionOnRayAccurate(Ray ray, float distance, float radius, GameObject ignoreGO, IsProperHit Judge, out HitData data, float distanceOffset = 0f)
	{
		Vector3 origin = ray.origin;
		Vector3 direction = ray.direction;
		if (distanceOffset > 0f)
		{
			origin += direction * distanceOffset;
			distance -= distanceOffset;
		}
		data.hitObj = null;
		data.distance = 100000000f;
		data.hitPos = Vector3.zero;
		List<HitInfo> list = HitDetection.SphereCast(origin, radius, direction, distance);
		foreach (HitInfo item in list)
		{
			RaycastHit data2 = item.data;
			GameObject gameObject = data2.transform.gameObject;
			if (!(gameObject == ignoreGO) && Judge(data2, item.hitZone))
			{
				direction = (ray.direction = data2.point - ray.origin);
				HitData data3;
				if (FirstCollisionOnRay(ray, length(direction) + 1f, ignoreGO, Judge, out data3) && data3.hitObj == gameObject)
				{
					data.hitPos = data2.point;
					data.hitObj = gameObject;
					data.distance = data2.distance + distanceOffset;
					return true;
				}
			}
		}
		return false;
	}
}
