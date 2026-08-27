using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
//using UnityEngine.Physics;
using static Unity.Mathematics.math;

public struct SensorVisionJob : IJob
{
    [ReadOnly] public float3 zombiePos;
    [ReadOnly] public float3 zombieEye;
    [ReadOnly] public float3 zombieForward;
    [ReadOnly] public float sightRange;
    [ReadOnly] public float sightFOV;
    [ReadOnly] public float weaponRange;
    [ReadOnly] public float contestRange;
    [ReadOnly] public float vomitMin;
    [ReadOnly] public float vomitMax;
    [ReadOnly] public float3 enemyPos;
    [ReadOnly] public float3 enemyForward;

    public NativeArray<float> outDist;
    public NativeArray<float3> outDir;
    public NativeArray<uint> outFlags;

    public void Execute()
    {
        uint flags = 0;

        float3 toEnemy = enemyPos - zombieEye;
        float distSq = lengthsq(toEnemy);
        float dist = sqrt(distSq);

        float sightRangeSq = sightRange * sightRange;
        if (distSq > sightRangeSq)
        {
            outDist[0] = dist;
            outFlags[0] = flags;
            return;
        }

        float3 toEnemyNorm = normalize(toEnemy);
        float dot1 = dot(zombieForward, toEnemyNorm);
        dot1 = clamp(dot1, -1f, 1f);
        float angleToEnemy = degrees(acos(dot1));

        if (angleToEnemy > sightFOV)
        {
            outDist[0] = dist;
            outFlags[0] = flags;
            return;
        }

        outDist[0] = dist;
        outDir[0] = toEnemyNorm;

        // InWeaponRange
        if (dist < weaponRange)
            flags |= 1u;

        // InContestRange
        if (dist < contestRange)
            flags |= 2u;

        // Enemy angles
        float3 enemyForwardNorm = normalize(enemyForward);
        float dot2 = dot(enemyForwardNorm, -toEnemyNorm);
        dot2 = clamp(dot2, -1f, 1f);
        float angleEnemyToMe = degrees(acos(dot2));

        if (angleEnemyToMe < 10f)
            flags |= 4u; // EnemyLookingAtMe

        float dot3 = dot(toEnemyNorm, enemyForwardNorm);
        dot3 = clamp(dot3, -1f, 1f);
        float angleToEnemyForward = degrees(acos(dot3));

        if (angleToEnemyForward > 135f && angleToEnemyForward < 225f)
            flags |= 8u; // AheadOfEnemy

        if (angleToEnemy < 90f)
            flags |= 16u; // EnemyAheadOfMe

        // InVomitRange
        if (dist > vomitMin && dist < vomitMax)
            flags |= 32u;

        // SeeEnemy
        flags |= 64u;

        outFlags[0] = flags;
    }
}

public class SensorEyesAI : SensorBase
{
    private AgentHuman MyEnemy;
    private float NextImportantObjCheckTime;
    private NavMeshPath m_NavMeshPath;
    private Rigidbody m_CachedRigidbody;
    private GameObject m_CachedRigidbodyOwner;

    private NativeArray<float> visionDist;
    private NativeArray<float3> visionDir;
    private NativeArray<uint> visionFlags;
    private NativeArray<RaycastCommand> raycastCommands;
    private NativeArray<RaycastHit> raycastResults;

    private bool jobsInitialized = false;

    public SensorEyesAI(AgentHuman owner)
        : base(owner)
    {
        base.Owner.BlackBoard.VisibleTarget = null;
        base.Owner.BlackBoard.SetImportantObject(null);
        MyEnemy = ((!Player.Instance) ? null : Player.Instance.Owner);
        m_NavMeshPath = new NavMeshPath();
    }

    public override void Update()
    {
        if (base.Owner.BlackBoard.Stop)
            return;

        WorldState ws = base.Owner.WorldState;
        BlackBoard bb = base.Owner.BlackBoard;

        ws.SetWSProperty(E_PropKey.SeeEnemy, false);
        ws.SetWSProperty(E_PropKey.LookingAtTarget, false);
        ws.SetWSProperty(E_PropKey.AheadOfEnemy, false);
        ws.SetWSProperty(E_PropKey.EnemyAheadOfMe, false);
        ws.SetWSProperty(E_PropKey.EnemyLookingAtMe, false);
        ws.SetWSProperty(E_PropKey.InWeaponRange, false);
        ws.SetWSProperty(E_PropKey.InContestRange, false);
        ws.SetWSProperty(E_PropKey.InVomitRange, false);
        ws.SetWSProperty(E_PropKey.CheckBait, false);
        ws.SetWSProperty(E_PropKey.DestroyObject, false);
        bb.VisibleTarget = null;

        // Check important objects less frequently
        if (Time.timeSinceLevelLoad >= NextImportantObjCheckTime
            && !bb.ActionPointOn)
        {
            IImportantObject importantObject = null;
            if ((importantObject = CheckForBait()) != null)
            {
                if (importantObject != bb.ImportantObject)
                    bb.SetImportantObject(importantObject);
                ws.SetWSProperty(E_PropKey.CheckBait, true);
            }
            else if ((importantObject = CheckForDestructibleObject()) != null)
            {
                if (importantObject != bb.ImportantObject)
                    bb.SetImportantObject(importantObject);
                ws.SetWSProperty(E_PropKey.DestroyObject, true);
            }
            else
            {
                bb.SetImportantObject(null);
            }
            NextImportantObjCheckTime = Time.timeSinceLevelLoad + 0.5f;
        }

        if (MyEnemy == null || !MyEnemy.IsAlive)
            return;

        // Initialize job arrays once
        if (!jobsInitialized)
        {
            visionDist = new NativeArray<float>(1, Allocator.Persistent);
            visionDir = new NativeArray<float3>(1, Allocator.Persistent);
            visionFlags = new NativeArray<uint>(1, Allocator.Persistent);
            raycastCommands = new NativeArray<RaycastCommand>(1, Allocator.Persistent);
            raycastResults = new NativeArray<RaycastHit>(1, Allocator.Persistent);
            jobsInitialized = true;
        }

        Vector3 eyePos = base.Owner.TransformEye.position;
        Vector3 toEnemy = MyEnemy.TransformTarget.position - eyePos;
        Vector3 forward = base.Owner.Forward;
        float distSq = toEnemy.sqrMagnitude;

        float sightRangeSq = bb.SightRange * bb.SightRange;

        if (distSq > sightRangeSq || Vector3.Angle(forward, toEnemy) > bb.SightFov)
        {
            SendLostEvent(MyEnemy);
            return;
        }

        // Run vision job
        var visionJob = new SensorVisionJob
        {
            zombiePos = (float3)base.Owner.Position,
            zombieEye = (float3)eyePos,
            zombieForward = (float3)forward,
            sightRange = bb.SightRange,
            sightFOV = bb.SightFov,
            weaponRange = bb.WeaponRange,
            contestRange = bb.ContestRange,
            vomitMin = bb.VomitRangeMin,
            vomitMax = bb.VomitRangeMax,
            enemyPos = (float3)MyEnemy.TransformTarget.position,
            enemyForward = (float3)MyEnemy.Forward,
            outDist = visionDist,
            outDir = visionDir,
            outFlags = visionFlags
        };

        JobHandle visionHandle = visionJob.Schedule();
        visionHandle.Complete();

        uint flags = visionFlags[0];
        float dist = visionDist[0];

        bb.DistanceToTarget = dist;
        bb.DirToTarget = (Vector3)visionDir[0];

        // Only do raycast if we can see enemy
        if ((flags & 64u) != 0)
        {
            SendSeeEvent(MyEnemy);

            toEnemy.Normalize();
            int layerMask = ~(ObjectLayerMask.IgnoreRaycast
                | ObjectLayerMask.Player
                | ObjectLayerMask.Enemy
                | ObjectLayerMask.EnemyBox);

            RaycastHit hitInfo;
            bool hasLOS = !Physics.Raycast(
                base.Owner.EyePosition, toEnemy, out hitInfo, dist, layerMask);

            if (hasLOS)
            {
                bb.VisibleTarget = MyEnemy;

                if ((flags & 1u) != 0 && !bb.ActionPointOn)
                    ws.SetWSProperty(E_PropKey.InWeaponRange, true);

                if ((flags & 2u) != 0 && !bb.ActionPointOn)
                    ws.SetWSProperty(E_PropKey.InContestRange, true);

                if ((flags & 4u) != 0)
                    ws.SetWSProperty(E_PropKey.EnemyLookingAtMe, true);

                if ((flags & 8u) != 0)
                    ws.SetWSProperty(E_PropKey.AheadOfEnemy, true);

                if ((flags & 16u) != 0)
                    ws.SetWSProperty(E_PropKey.EnemyAheadOfMe, true);

                if ((flags & 32u) != 0 && !bb.ActionPointOn)
                    ws.SetWSProperty(E_PropKey.InVomitRange, true);

                float angleToEnemy = Vector3.Angle(forward, toEnemy);
                if (angleToEnemy < Mathf.Lerp(10f, 60f, 1f - dist / bb.SightRange))
                    ws.SetWSProperty(E_PropKey.LookingAtTarget, true);

                ws.SetWSProperty(E_PropKey.SeeEnemy, true);

                // Contest logic
                if (angleToEnemy < 30f && (flags & 4u) == 0)
                {
                    if (base.Owner.CanDoContest(MyEnemy, true))
                        base.Owner.StartContest(MyEnemy);
                }
                else if (base.Owner.IsInContest() && !MyEnemy.IsInContest())
                {
                    base.Owner.StopContest(MyEnemy);
                }
            }
        }

        CheckContestValid();
    }

    private void CheckContestValid()
    {
        if (base.Owner.WorldState.GetWSProperty(E_PropKey.Contest).GetBool()
            && !MyEnemy.WorldState.GetWSProperty(E_PropKey.Contest).GetBool()
            && !base.Owner.CanDoContest(MyEnemy, false))
        {
            base.Owner.StopContest(MyEnemy);
        }
    }

    public override void Reset()
    {
        BlackBoard bb = base.Owner.BlackBoard;
        WorldState ws = base.Owner.WorldState;

        bb.VisibleTarget = null;
        bb.SetImportantObject(null);
        MyEnemy = ((!Player.Instance) ? null : Player.Instance.Owner);

        ws.SetWSProperty(E_PropKey.SeeEnemy, false);
        ws.SetWSProperty(E_PropKey.LookingAtTarget, false);
        ws.SetWSProperty(E_PropKey.AheadOfEnemy, false);
        ws.SetWSProperty(E_PropKey.EnemyAheadOfMe, false);
        ws.SetWSProperty(E_PropKey.EnemyLookingAtMe, false);
        ws.SetWSProperty(E_PropKey.InWeaponRange, false);
        ws.SetWSProperty(E_PropKey.InContestRange, false);
        ws.SetWSProperty(E_PropKey.InVomitRange, false);
        ws.SetWSProperty(E_PropKey.CheckBait, false);
        ws.SetWSProperty(E_PropKey.DestroyObject, false);
        ws.SetWSProperty(E_PropKey.Contest, false);
    }

    private void SendLostEvent(AgentHuman target)
    {
        Fact fact = base.Owner.Memory.GetFact(E_EventTypes.EnemySee);
        if (fact != null)
        {
            base.Owner.Memory.RemoveFact(E_EventTypes.EnemySee);
            fact = FactsFactory.Create(E_EventTypes.EnemyLost);
            fact.Agent = target;
            fact.Position = target.Position;
            fact.LiveTime = 180f;
            fact.Delay = UnityEngine.Random.Range(0.2f, 0.6f);
            base.Owner.AddFactToMemory(fact);
        }
    }

    private void SendSeeEvent(AgentHuman target)
    {
        base.Owner.Memory.RemoveFact(E_EventTypes.EnemyLost);
        base.Owner.Memory.RemoveFact(E_EventTypes.EnemyHideInCover);
        Fact fact = FactsFactory.Create(E_EventTypes.EnemySee);
        fact.Type = E_EventTypes.EnemySee;
        fact.Position = target.Position;
        fact.Delay = 0f;
        fact.LiveTime = 100f;
        fact.Agent = target;
        base.Owner.AddFactToMemory(fact);
        if (base.Owner.Memory.GetValidFact(E_EventTypes.EnemySee) != null)
            base.Owner.WorldState.SetWSProperty(E_PropKey.SeeEnemy, true);
    }

    private float GetSqrSpeed(GameObject obj)
    {
        if (obj == null) return 0f;
        if (m_CachedRigidbodyOwner != obj)
        {
            m_CachedRigidbody = obj.GetComponent<Rigidbody>();
            m_CachedRigidbodyOwner = obj;
        }
        return m_CachedRigidbody != null ? m_CachedRigidbody.velocity.sqrMagnitude : 0f;
    }

    private float GetSqrDistance(GameObject obj, Vector3 pos)
    {
        return (obj != null) ? (obj.transform.position - pos).sqrMagnitude : float.PositiveInfinity;
    }

    private IImportantObject CompareDistance(IImportantObject first, IImportantObject second)
    {
        Vector3 ownerPos = base.Owner.transform.position;
        float sqrA = (first.GetGameObject().transform.position - ownerPos).sqrMagnitude;
        float sqrB = (second.GetGameObject().transform.position - ownerPos).sqrMagnitude;
        return sqrA < sqrB ? first : second;
    }

    private IImportantObject CheckForBait()
    {
        List<IImportantObject> objects = Mission.Instance.CurrentGameZone.ImportantObjects;

        for (int i = 0; i < objects.Count; i++)
        {
            IImportantObject item = objects[i];
            if (item.GetGameObject() == null) continue;

            E_ImportantObjectType type = item.GetImportantObjectType();
            if (type != E_ImportantObjectType.Bait && type != E_ImportantObjectType.GrenadeBait)
                continue;

            if (GetSqrSpeed(item.GetGameObject()) < 2f
                && IsPointReachable(item.GetGameObject().transform.position, base.Owner.BlackBoard.BaitRange))
            {
                return item;
            }
        }
        return null;
    }

    private IImportantObject CheckForDestructibleObject()
    {
        float sqrDistToEnemy = (MyEnemy.Transform.position - base.Owner.Transform.position).sqrMagnitude;

        if (sqrDistToEnemy < 25f)
        {
            Fact fact = base.Owner.Memory.GetFact(E_EventTypes.EnemyInjuredMe);
            if (fact != null && fact.Belief > 0.2f)
                return null;
        }

        DestructibleObject destructibleObject = base.Owner.BlackBoard.ImportantObject as DestructibleObject;
        IImportantObject result = null;

        if (destructibleObject != null && destructibleObject.IsAlive && destructibleObject.GetGameObject() != null)
        {
            result = destructibleObject;
        }
        else
        {
            List<IImportantObject> objects = Mission.Instance.CurrentGameZone.ImportantObjects;

            for (int i = 0; i < objects.Count; i++)
            {
                IImportantObject item = objects[i];
                if (item.GetImportantObjectType() != E_ImportantObjectType.DestructibleObject)
                    continue;

                if (item.GetGameObject() == null) continue;

                destructibleObject = (DestructibleObject)item;
                if (!destructibleObject.IsAlive || destructibleObject.GetRegisteredAgentsCount() >= 1)
                    continue;

                AttackPoint attackPoint = destructibleObject.FindAttackPoint(null);
                if (attackPoint != null
                    && IsPointReachable(attackPoint.Transform.position, base.Owner.BlackBoard.DestructibleObjectRange))
                {
                    result = (result != null) ? CompareDistance(result, item) : item;
                }
            }
        }

        if (result != null && MyEnemy != null)
        {
            float sqrDistToObj = GetSqrDistance(result.GetGameObject(), base.Owner.Transform.position) * 0.4f;
            if (sqrDistToEnemy > 4f && sqrDistToObj < sqrDistToEnemy)
                return result;
        }

        return null;
    }

    private bool IsPointReachable(Vector3 pos, float dist)
    {
        if (base.Owner.NavMeshAgent == null || !base.Owner.NavMeshAgent.enabled)
            return false;

        if ((base.Owner.Position - pos).sqrMagnitude <= dist * dist)
            return true;

        m_NavMeshPath.ClearCorners();
        bool flag = base.Owner.NavMeshAgent.CalculatePath(pos, m_NavMeshPath);

        if (!flag && Debug.isDebugBuild)
            Debug.Log("IsPointReachable: result=" + flag + " pos=" + pos + " status=" + m_NavMeshPath.status);

        return flag;
    }

    private void OnDestroy()
    {
        if (visionDist.IsCreated) visionDist.Dispose();
        if (visionDir.IsCreated) visionDir.Dispose();
        if (visionFlags.IsCreated) visionFlags.Dispose();
        if (raycastCommands.IsCreated) raycastCommands.Dispose();
        if (raycastResults.IsCreated) raycastResults.Dispose();
    }
}