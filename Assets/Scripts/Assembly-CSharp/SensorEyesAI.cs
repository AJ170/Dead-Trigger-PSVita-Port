using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SensorEyesAI : SensorBase
{
    private AgentHuman MyEnemy;
    private float NextImportantObjCheckTime;

    // Cached NavMeshPath to avoid per-call allocation
    // in IsPointReachable
    private NavMeshPath m_NavMeshPath;

    // Cached Rigidbody reference for GetSqrSpeed
    // avoids double GetComponent lookup
    private Rigidbody m_CachedRigidbody;
    private GameObject m_CachedRigidbodyOwner;

    public SensorEyesAI(AgentHuman owner)
        : base(owner)
    {
        base.Owner.BlackBoard.VisibleTarget = null;
        base.Owner.BlackBoard.SetImportantObject(null);
        MyEnemy = ((!Player.Instance) ? null : Player.Instance.Owner);

        // Pre-allocate NavMeshPath once
        m_NavMeshPath = new NavMeshPath();
    }

    public override void Update()
    {
        if (base.Owner.BlackBoard.Stop)
            return;

        // Cache WorldState and BlackBoard refs to avoid
        // repeated property lookups throughout this method
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
            else if ((importantObject =
                CheckForDestructibleObject()) != null)
            {
                if (importantObject != bb.ImportantObject)
                    bb.SetImportantObject(importantObject);
                ws.SetWSProperty(E_PropKey.DestroyObject, true);
            }
            else
            {
                bb.SetImportantObject(null);
            }
        }

        if (MyEnemy == null || !MyEnemy.IsAlive)
            return;

        Vector3 eyePos = base.Owner.TransformEye.position;
        Vector3 toEnemy = MyEnemy.TransformTarget.position - eyePos;
        Vector3 forward = base.Owner.Forward;

        // Use sqrMagnitude where possible to avoid sqrt
        float distSq = toEnemy.sqrMagnitude;
        float dist = Mathf.Sqrt(distSq);

        // Distance calculation for destroy object case
        float num = ws.GetWSProperty(E_PropKey.DestroyObject).GetBool()
            ? (bb.ImportantObject.GetGameObject().transform.position
                - base.Owner.Transform.position).magnitude
            : dist;

        bb.DistanceToTarget = num;
        bb.DirToTarget = toEnemy;

        float sightRangeSq = bb.SightRange * bb.SightRange;

        if (distSq > sightRangeSq
            || Vector3.Angle(forward, toEnemy) > bb.SightFov)
        {
            SendLostEvent(MyEnemy);
            return;
        }

        if (!bb.ActionPointOn)
        {
            if (num < bb.WeaponRange)
                ws.SetWSProperty(E_PropKey.InWeaponRange, true);

            if (num < bb.ContestRange)
                ws.SetWSProperty(E_PropKey.InContestRange, true);
        }

        toEnemy.Normalize();
        SendSeeEvent(MyEnemy);

        int layerMask = ~(ObjectLayerMask.IgnoreRaycast
            | ObjectLayerMask.Player
            | ObjectLayerMask.Enemy
            | ObjectLayerMask.EnemyBox);

        RaycastHit hitInfo;
        bool hasLOS = !Physics.Raycast(
            base.Owner.EyePosition, toEnemy, out hitInfo, num, layerMask);

        if (ws.GetWSProperty(E_PropKey.SeeEnemy).GetBool())
        {
            bb.VisibleTarget = MyEnemy;

            float angleToEnemy = Vector3.Angle(forward, toEnemy);
            if (angleToEnemy < Mathf.Lerp(10f, 60f,
                1f - bb.DistanceToTarget / bb.SightRange))
            {
                ws.SetWSProperty(E_PropKey.LookingAtTarget, true);
            }

            Vector3 enemyForward = MyEnemy.Forward;
            float angleEnemyToMe = Vector3.Angle(enemyForward, -toEnemy);

            if (angleEnemyToMe < 10f)
                ws.SetWSProperty(E_PropKey.EnemyLookingAtMe, true);

            float angleToEnemyForward = Vector3.Angle(toEnemy, enemyForward);
            if (angleToEnemyForward > 135f && angleToEnemyForward < 225f)
                ws.SetWSProperty(E_PropKey.AheadOfEnemy, true);

            if (angleToEnemy < 90f)
                ws.SetWSProperty(E_PropKey.EnemyAheadOfMe, true);

            if (hasLOS
                && angleEnemyToMe < 45f
                && num < bb.VomitRangeMax
                && num > bb.VomitRangeMin
                && !bb.ActionPointOn)
            {
                ws.SetWSProperty(E_PropKey.InVomitRange, true);
            }

            if ((bb.MovementSkill & F_MovementSkill.Berserk) != 0
                && num > Random.Range(7f, 8f)
                && angleEnemyToMe < 20f
                && hasLOS)
            {
                ws.SetWSProperty(E_PropKey.Berserk, true);
            }

            if (angleToEnemy < 30f && angleEnemyToMe > 100f)
            {
                if (base.Owner.CanDoContest(MyEnemy, true))
                    base.Owner.StartContest(MyEnemy);
            }
            else if (base.Owner.IsInContest()
                && !MyEnemy.IsInContest())
            {
                base.Owner.StopContest(MyEnemy);
            }
        }

        CheckContestValid();
    }

    private void CheckContestValid()
    {
        if (base.Owner.WorldState.GetWSProperty(
                E_PropKey.Contest).GetBool()
            && !MyEnemy.WorldState.GetWSProperty(
                E_PropKey.Contest).GetBool()
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
            fact.Delay = Random.Range(0.2f, 0.6f);
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

        // Cache Rigidbody lookup to avoid double GetComponent
        if (m_CachedRigidbodyOwner != obj)
        {
            m_CachedRigidbody = obj.GetComponent<Rigidbody>();
            m_CachedRigidbodyOwner = obj;
        }

        return m_CachedRigidbody != null
            ? m_CachedRigidbody.velocity.sqrMagnitude
            : 0f;
    }

    private float GetSqrDistance(GameObject obj, Vector3 pos)
    {
        return (obj != null)
            ? (obj.transform.position - pos).sqrMagnitude
            : float.PositiveInfinity;
    }

    private IImportantObject CompareDistance(
        IImportantObject first, IImportantObject second)
    {
        Vector3 ownerPos = base.Owner.transform.position;
        float sqrA = (first.GetGameObject().transform.position
            - ownerPos).sqrMagnitude;
        float sqrB = (second.GetGameObject().transform.position
            - ownerPos).sqrMagnitude;
        return sqrA < sqrB ? first : second;
    }

    private IImportantObject CheckForBait()
    {
        List<IImportantObject> objects =
            Mission.Instance.CurrentGameZone.ImportantObjects;

        // Use index loop to avoid enumerator allocation
        for (int i = 0; i < objects.Count; i++)
        {
            IImportantObject item = objects[i];
            if (item.GetGameObject() == null) continue;

            E_ImportantObjectType type = item.GetImportantObjectType();
            if (type != E_ImportantObjectType.Bait
                && type != E_ImportantObjectType.GrenadeBait)
                continue;

            if (GetSqrSpeed(item.GetGameObject()) < 2f
                && IsPointReachable(
                    item.GetGameObject().transform.position,
                    base.Owner.BlackBoard.BaitRange))
            {
                return item;
            }
        }
        return null;
    }

    private IImportantObject CheckForDestructibleObject()
    {
        float sqrDistToEnemy = (MyEnemy.Transform.position
            - base.Owner.Transform.position).sqrMagnitude;

        if (sqrDistToEnemy < 25f)
        {
            Fact fact = base.Owner.Memory
                .GetFact(E_EventTypes.EnemyInjuredMe);
            if (fact != null && fact.Belief > 0.2f)
                return null;
        }

        DestructibleObject destructibleObject =
            base.Owner.BlackBoard.ImportantObject as DestructibleObject;
        IImportantObject result = null;

        if (destructibleObject != null
            && destructibleObject.IsAlive
            && destructibleObject.GetGameObject() != null)
        {
            result = destructibleObject;
        }
        else
        {
            List<IImportantObject> objects =
                Mission.Instance.CurrentGameZone.ImportantObjects;

            // Use index loop to avoid enumerator allocation
            for (int i = 0; i < objects.Count; i++)
            {
                IImportantObject item = objects[i];
                if (item.GetImportantObjectType()
                    != E_ImportantObjectType.DestructibleObject)
                    continue;

                if (item.GetGameObject() == null) continue;

                destructibleObject = (DestructibleObject)item;
                if (!destructibleObject.IsAlive
                    || destructibleObject.GetRegisteredAgentsCount() >= 1)
                    continue;

                AttackPoint attackPoint =
                    destructibleObject.FindAttackPoint(null);
                if (attackPoint != null
                    && IsPointReachable(
                        attackPoint.Transform.position,
                        base.Owner.BlackBoard.DestructibleObjectRange))
                {
                    result = (result != null)
                        ? CompareDistance(result, item)
                        : item;
                }
            }
        }

        if (result != null && MyEnemy != null)
        {
            float sqrDistToObj = GetSqrDistance(
                result.GetGameObject(),
                base.Owner.Transform.position) * 0.4f;

            if (sqrDistToEnemy > 4f && sqrDistToObj < sqrDistToEnemy)
                return result;
        }

        return null;
    }

    private bool IsPointReachable(Vector3 pos, float dist)
    {
        if (base.Owner.NavMeshAgent == null
            || !base.Owner.NavMeshAgent.enabled)
            return false;

        if ((base.Owner.Position - pos).sqrMagnitude <= dist * dist)
            return true;

        // Reuse cached NavMeshPath to avoid per-call allocation
        m_NavMeshPath.ClearCorners();
        bool flag = base.Owner.NavMeshAgent
            .CalculatePath(pos, m_NavMeshPath);

        if (!flag && Debug.isDebugBuild)
            Debug.Log("IsPointReachable: result=" + flag
                + " pos=" + pos
                + " status=" + m_NavMeshPath.status);

        return flag;
    }
}