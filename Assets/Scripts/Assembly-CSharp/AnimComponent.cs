using System;
using UnityEngine;

public class AnimComponent : MonoBehaviour
{
	public E_AnimFSMTypes TypeOfFSM;

	private AnimFSM FSM;

	private AgentHuman Owner;

	private Animation Animation;

	private Transform ContactPlatfrom;

	private Vector3 ContactPoint;

	public AnimState CurrentAnimState
	{
		get
		{
			return (FSM == null) ? null : FSM.CurrentAnimState;
		}
	}

	public void SetFSM(E_AnimFSMTypes fsmType)
	{
		if (FSM != null)
		{
			Deactivate();
		}
		TypeOfFSM = fsmType;
		switch (TypeOfFSM)
		{
		case E_AnimFSMTypes.Player:
			FSM = new AnimFSMPlayer(Animation, Owner);
			break;
		case E_AnimFSMTypes.ZombieNormal:
			FSM = new AnimFSMZombieNormal(Animation, Owner);
			break;
		case E_AnimFSMTypes.ZombieVomit:
			FSM = new AnimFSMZombieVomit(Animation, Owner);
			break;
		case E_AnimFSMTypes.ZombieBoss1:
			FSM = new AnimFSMZombieBoss1(Animation, Owner);
			break;
		case E_AnimFSMTypes.ZombieBossSanta:
			FSM = new AnimFSMZombieBossSanta(Animation, Owner);
			break;
		default:
			Debug.LogError(base.name + " unkown type of FSM");
			break;
		}
		FSM.Reset();
		FSM.Initialize();
		FSM.Activate();
	}

	public void Awake()
	{
		Owner = GetComponent<AgentHuman>();
		Animation = base.GetComponent<Animation>();
		switch (TypeOfFSM)
		{
		case E_AnimFSMTypes.Player:
			FSM = new AnimFSMPlayer(Animation, Owner);
			break;
		case E_AnimFSMTypes.ZombieNormal:
			FSM = new AnimFSMZombieNormal(Animation, Owner);
			break;
		case E_AnimFSMTypes.ZombieVomit:
			FSM = new AnimFSMZombieVomit(Animation, Owner);
			break;
		case E_AnimFSMTypes.ZombieBoss1:
			FSM = new AnimFSMZombieBoss1(Animation, Owner);
			break;
		case E_AnimFSMTypes.ZombieBossSanta:
			FSM = new AnimFSMZombieBossSanta(Animation, Owner);
			break;
		default:
			Debug.LogError(base.name + " unkown type of FSM");
			break;
		}
		FSM.Initialize();
		base.enabled = false;
	}

	private void Start()
	{
		BlackBoard blackBoard = Owner.BlackBoard;
		blackBoard.ActionHandler = (BlackBoard.AgentActionHandler)Delegate.Combine(blackBoard.ActionHandler, new BlackBoard.AgentActionHandler(HandleAction));
	}

	private static readonly Vector3 s_MoveUp = new Vector3(0f, 0.1f, 0f);
	private static readonly Vector3 s_MoveDown = new Vector3(0f, -0.1f, 0f);

	private void Update()
	{
		if (Time.timeScale == 0f)
		{
			return;
		}
		if ((bool)ContactPlatfrom)
		{
			Vector3 vector = ContactPlatfrom.position - ContactPoint;
			ContactPlatfrom = null;
			if ((bool)Owner.CharacterController)
			{
				Owner.CharacterController.Move(vector);
			}
			else
			{
				Owner.Transform.position += vector;
			}
		}
		else if ((bool)Owner.CharacterController && Owner.CharacterController.enabled)
		{
			Owner.CharacterController.Move(s_MoveUp);
			Owner.CharacterController.Move(s_MoveDown);
		}
		if ((bool)Owner.CharacterController)
		{
		}
		FSM.UpdateAnimStates();
	}

	private void LateUpdate()
	{
	}

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		if ((bool)hit.rigidbody && hit.rigidbody.isKinematic && hit.gameObject.GetComponent<Projectile>() == null)
		{
			ContactPlatfrom = hit.transform;
			ContactPoint = ContactPlatfrom.position;
		}
	}

	public void HandleAction(AgentAction action)
	{
		if (Owner.debugAnims)
		{
			Debug.Log(Time.timeSinceLevelLoad + " " + base.gameObject.name + " handle action " + action.ToString());
		}
		if (base.enabled && !action.IsFailed() && !FSM.DoAction(action))
		{
			action.SetFailed();
		}
	}

	public void Activate(SpawnPoint spawn)
	{
		if (Owner.debugAnims)
		{
			Debug.Log(base.gameObject.name + " activated");
		}
		base.enabled = true;
		FSM.Activate();
	}

	public void Deactivate()
	{
		Animation.Stop();
		Animation.Rewind();
		FSM.Reset();
		base.enabled = false;
		ContactPlatfrom = null;
		if (Owner.debugAnims)
		{
			Debug.Log(base.gameObject.name + "deactivated");
		}
	}

	public void OnTeleport()
	{
		Animation.Stop();
		FSM.Reset();
		FSM.Activate();
	}

	private void HandleAnimationEvent(AnimationEvent animEvent)
	{
		FSM.CurrentAnimState.HandleAnimationEvent((AnimState.E_AnimEvent)animEvent.intParameter);
	}
}
