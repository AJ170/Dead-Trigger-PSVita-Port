using System;
using System.Collections;
using UnityEngine;

public class AnimStateContestZombieButtons : AnimState
{
    private enum E_State
    {
        Start = 0,
        Loop = 1,
        Injury = 2,
        Won = 3,
        Lost = 4,
        Death = 5,
        Finish = 6,
        End = 7
    }

    private AgentActionContest Action;

    private E_State State;

    private float EndOfStateTime;

    private float ContestBalance;

    private bool NavMeshUpdatePos = true;

    private string animBase;

    private string animGood;

    private string animBad;

    private AudioClip SpeechLoop;

    private ComponentEnemy EnemyComponent;

    public AnimStateContestZombieButtons(Animation anims, AgentHuman owner)
        : base(anims, owner)
    {
    }

    public override void OnActivate(AgentAction action)
    {
        base.OnActivate(action);
        if (Action != null)
        {
            Owner.BlackBoard.ReactOnHits = false;
            Owner.BlackBoard.BusyAction = true;
            Owner.BlackBoard.Desires.WeaponTriggerOn = false;
            Owner.BlackBoard.MotionType = E_MotionType.Contest;
            Owner.BlackBoard.MoveDir = Vector3.zero;
            Owner.BlackBoard.Speed = 0f;
            Owner.NavMeshAgent.speed = 0f;
            Owner.SetEyes(false);
            animBase = Owner.AnimSet.GetContestAnim(E_ContestState.LoopBase);
            animGood = Owner.AnimSet.GetContestAnim(E_ContestState.LoopWinning);
            animBad = Owner.AnimSet.GetContestAnim(E_ContestState.LoopLoosing);
            ContestStart();
        }
    }

    public override void OnDeactivate()
    {
        Owner.NavMeshAgent.updatePosition = Owner.IsAlive && NavMeshUpdatePos;
        Owner.NavMeshAgent.Resume();
        Owner.BlackBoard.ReactOnHits = true;
        Owner.BlackBoard.BusyAction = false;
        Owner.BlackBoard.PrevMotionType = Owner.BlackBoard.MotionType;
        Owner.BlackBoard.MotionType = E_MotionType.None;
        Owner.SetEyes(true);
        if (Action != null)
        {
            Action.SetSuccess();
            Action = null;
        }
        base.OnDeactivate();
    }

    public override void Reset()
    {
        Action.SetSuccess();
        Action = null;
        base.Reset();
    }

    public override bool HandleNewAction(AgentAction action)
    {
        if (action is AgentActionContest)
        {
            Debug.LogError("Another AgentActionContest arrived");
            action.SetFailed();
            return true;
        }
        if (action is AgentActionDeath)
        {
            Finish();
        }
        return false;
    }

    public override void Release()
    {
        SetFinished(true);
    }

    public override void Update()
    {
        switch (State)
        {
            case E_State.Start:
                if (EndOfStateTime <= Time.timeSinceLevelLoad)
                {
                    InitializeLoop();
                }
                break;
            case E_State.Loop:
                UpdateLoop();
                break;
            case E_State.Lost:
                ContestLost();
                break;
            case E_State.Won:
                ContestWon();
                break;
            case E_State.Death:
                if (EndOfStateTime <= Time.timeSinceLevelLoad)
                {
                    Finish(false, false);
                    Owner.TakeDamage(Owner, Owner.BlackBoard.Health * 2f, null, new Vector3(0f, 0f, 0f), E_WeaponID.None, E_WeaponType.Contest);
                }
                break;
            case E_State.Finish:
                if (EndOfStateTime <= Time.timeSinceLevelLoad)
                {
                    Finish();
                }
                break;
            case E_State.End:
                break;
            case E_State.Injury:
                break;
        }
    }

    private IEnumerator _SetFPVLayer(bool fpvOn, float delay)
    {
        yield return new WaitForSeconds(delay);
        Owner.SetFPVLayer(fpvOn);
    }

    protected override void Initialize(AgentAction action)
    {
        base.Initialize(action);
        Action = action as AgentActionContest;
        State = E_State.Start;
        if (!Owner.CanDoContest(Action.Enemy, false))
        {
            Owner.StopContest(Action.Enemy);
            action.SetFailed();
            Release();
            Action = null;
            return;
        }
        Owner.SetFPVLayer(true);
        EnemyComponent = Owner.GetComponent<ComponentEnemy>();
        if (EnemyComponent == null)
        {
            throw new MemberAccessException("ComponentEnemy not found!");
        }
        SpeechLoop = ((EnemyComponent.ContestRhythm.Length <= 0) ? null : EnemyComponent.ContestRhythm[0].SpeechLoop);
        Action.Enemy.StartContest(Owner);
        Owner.ToggleCollisions(false, false);
        Owner.NavMeshAgent.Stop(true);
        NavMeshUpdatePos = Owner.NavMeshAgent.updatePosition;
        Owner.NavMeshAgent.updatePosition = false;
        Transform transform = Action.Enemy.Transform.FindChildByName("ContestRoot");
        Transform.position = transform.position;
        Owner.NavMeshAgent.transform.position = transform.position;
        Transform.rotation = transform.rotation;
        Owner.NavMeshAgent.transform.rotation = transform.rotation;
        Owner.BlackBoard.Desires.Rotation = transform.rotation;
        Owner.BlackBoard.ContestBalance = (ContestBalance = 0f);
    }

    private void ContestStart()
    {
        Animation.Stop();
        Animation[animBase].layer = 7;
        Animation[animGood].layer = 7;
        Animation[animBad].layer = 7;
        Animation.SyncLayer(7);
        string contestAnim = Owner.AnimSet.GetContestAnim(E_ContestState.Start);
        CrossFade(contestAnim, 0.05f, PlayMode.StopSameLayer);
        Owner.AddFactToMemory(E_EventTypes.ContestStart, Action.Enemy, 0.2f, 0f);
        EndOfStateTime = Time.timeSinceLevelLoad + Animation[contestAnim].length * 0.9f + Time.deltaTime;
    }

    private void InitializeLoop()
    {
        State = E_State.Loop;
        CrossFade(animBase, 0.1f, PlayMode.StopSameLayer);

        if (SpeechLoop != null)
            Owner.PlayLoopedSound(SpeechLoop, 0f, 1000f, 0.1f, 0.1f);

        GameObject[] array = new GameObject[5];
        EnemyComponent.ContestHit.CopyTo(0, array, 0, 5);

        GuiHUD.Instance.StartContest(array, ContestKeys, OnButtonHit); // 💡 Use your updated overload

        EndOfStateTime = Time.timeSinceLevelLoad + Action.Time;
    }


    private void OnButtonHit(GameObject contestObject, GuiHUD.ContestResult result)
    {
        switch (result)
        {
            case GuiHUD.ContestResult.Fail:
                ContestBalance = 1f;
                break;
            case GuiHUD.ContestResult.PartialSucess:
                ContestBalance -= ((EnemyComponent.ContestHit.Count <= 0) ? 1f : (1f / (float)EnemyComponent.ContestHit.Count));
                break;
            case GuiHUD.ContestResult.Success:
                ContestBalance = -1f;
                break;
        }
    }

    private void UpdateLoop()
    {
        string text = null;
        string text2 = null;
        float num = 0f;
        Owner.BlackBoard.Desires.Rotation.SetLookRotation(Action.Enemy.Transform.position - Owner.Position);
        Owner.BlackBoard.ContestBalance = ContestBalance;
        if (ContestBalance < 0f)
        {
            num = 0f - ContestBalance;
            text = animBad;
            text2 = animGood;
        }
        else
        {
            num = ContestBalance;
            text = animGood;
            text2 = animBad;
        }
        if (text != null && num > 0.01f)
        {
            Animation[animBase].weight = 1f - num;
            Blend(text, num, 0f);
            if (Animation.IsPlaying(text2))
            {
                Animation.Stop(text2);
            }
        }
        else
        {
            if (Animation.IsPlaying(animGood))
            {
                Animation.Stop(animGood);
            }
            if (Animation.IsPlaying(animBad))
            {
                Animation.Stop(animBad);
            }
        }
        if (ContestBalance <= -1f)
        {
            State = E_State.Lost;
            StopAnims();
        }
        else if (ContestBalance >= 1f)
        {
            State = E_State.Won;
            StopAnims();
        }
    }

    private void ContestLost()
    {
        Owner.AddFactToMemory(E_EventTypes.ContestLost, Action.Enemy, 1f, 0f);
        State = E_State.Death;
        EndOfStateTime = Time.timeSinceLevelLoad;
        Owner.BlackBoard.MotionType = E_MotionType.Death;
        Owner.StartCoroutine(_SetFPVLayer(false, 2f));
    }

    private void ContestWon()
    {
        Owner.AddFactToMemory(E_EventTypes.ContestWon, Action.Enemy, 1f, 0f);
        State = E_State.Finish;
        string contestAnim = Owner.AnimSet.GetContestAnim(E_ContestState.Won);
        CrossFade(contestAnim, 0.1f, PlayMode.StopSameLayer);
        EndOfStateTime = Time.timeSinceLevelLoad + Animation[contestAnim].length * 0.9f;
        Action.Enemy.TakeDamage(Owner, Owner.BlackBoard.AttackSetup.MeleeAttackDamage, null, new Vector3(0f, 0f, 0f), E_WeaponID.None, E_WeaponType.Contest);
        Owner.StartCoroutine(_SetFPVLayer(false, 1.8f));
    }

    private void Finish(bool enableCollisions = true, bool callRelease = true)
    {
        State = E_State.End;
        GuiHUD.Instance.StopContest();
        if (SpeechLoop != null)
        {
            Owner.StopLoopedSound();
        }
        if (enableCollisions)
        {
            Owner.ToggleCollisions(true, true);
        }
        if (callRelease)
        {
            Release();
        }
    }
    private static readonly KeyCode[] ContestKeys = new KeyCode[]
{
    KeyCode.E,
    KeyCode.R,
    KeyCode.F,
    KeyCode.Space,
    KeyCode.Q
};

    private void StopAnims()
    {
        if (Animation.IsPlaying(animBase))
        {
            Animation.Stop(animBase);
        }
        if (Animation.IsPlaying(animGood))
        {
            Animation.Stop(animGood);
        }
        if (Animation.IsPlaying(animBad))
        {
            Animation.Stop(animBad);
        }
    }
}
