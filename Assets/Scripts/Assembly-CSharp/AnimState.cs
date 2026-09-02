using UnityEngine;

public class AnimState
{
	public enum E_AnimEvent
	{
		Loop = 0
	}

	protected Animation Animation;

	private bool m_Finished = true;

	protected AgentHuman Owner;

	protected Transform Transform;

	public AnimState(Animation anims, AgentHuman owner)
	{
		Animation = anims;
		Owner = owner;
		Transform = Owner.transform;
	}

	public virtual void OnActivate(AgentAction action)
	{
		if (Owner.debugAnims)
		{
			Debug.Log(Time.timeSinceLevelLoad + " " + ToString() + " Activate  by " + ((action == null) ? "nothing" : action.ToString()));
		}
		SetFinished(false);
		Initialize(action);
	}

	public virtual void OnDeactivate()
	{
		if (Owner.debugAnims)
		{
			Debug.Log(Time.timeSinceLevelLoad + " " + ToString() + " DeActivate");
		}
	}

	public virtual void Reset()
	{
		if (Owner.debugAnims)
		{
			Debug.Log(Time.timeSinceLevelLoad + " " + ToString() + " Reset");
		}
	}

	public virtual void Release()
	{
		SetFinished(true);
	}

	public virtual bool HandleNewAction(AgentAction action)
	{
		return false;
	}

	public virtual void Update()
	{
	}

	public virtual bool IsFinished()
	{
		return m_Finished;
	}

	public virtual void SetFinished(bool finished)
	{
		m_Finished = finished;
	}

	public virtual void HandleAnimationEvent(E_AnimEvent animEvent)
	{
	}

	protected virtual void Initialize(AgentAction action)
	{
	}

	protected virtual bool SetTargetLocation(Vector3 pos)
	{
		//Important fix to stop zombies from getting stuck after an attack
		Owner.NavMeshAgent.isStopped = false;
		//Debug.LogError ("Navigate To: " + Owner.NavMeshAgent.SetDestination (pos));
		return Owner.NavMeshAgent.SetDestination(pos);
	}

	protected virtual bool Move(Vector3 velocity, bool slide = true)
	{
		if (Owner.CharacterController == null)
		{
			return false;
		}
		if (!Owner.CharacterController.isGrounded)
		{
			velocity.y -= 30f * Time.deltaTime;
		}
		Owner.CharacterController.Move(velocity);
		return true;
	}

	protected virtual bool MoveEx(Vector3 velocity)
	{
		if (Owner.CharacterController == null)
		{
			return false;
		}
		Vector3 position = Transform.position;
		Transform.position += Vector3.up * Time.deltaTime;
		velocity.y -= 9f * Time.deltaTime;
		RaycastHit hitInfo;
		if (Owner.CharacterController.Move(velocity) == CollisionFlags.None && !Physics.Raycast(Transform.position, -Vector3.up, out hitInfo, 3f, 8192))
		{
			Transform.position = position;
			return false;
		}
		return true;
	}

	protected bool IsGroundThere(Vector3 pos)
	{
		return Physics.Raycast(pos + Vector3.up, -Vector3.up, 5f, 16384);
	}

	protected void CrossFade(string anim, float fadeInTime, PlayMode mode)
	{
		if (Owner.debugAnims)
		{
			Debug.Log(Time.timeSinceLevelLoad + " " + ToString() + " cross fade anim: " + anim + " in " + fadeInTime + "s.");
		}
		if (Animation.IsPlaying(anim))
		{
			Animation.CrossFadeQueued(anim, fadeInTime, QueueMode.PlayNow);
		}
		else
		{
			Animation.CrossFade(anim, fadeInTime, mode);
		}
	}

	protected void Blend(string anim, float fadeInTime)
	{
		if (Owner.debugAnims)
		{
			Debug.Log(Time.timeSinceLevelLoad + " " + ToString() + " blend anim: " + anim + " in " + fadeInTime + "s.");
		}
		Animation.Blend(anim, 1f, fadeInTime);
	}

	protected void Blend(string anim, float weight, float fadeInTime)
	{
		if (Owner.debugAnims)
		{
			Debug.Log(Time.timeSinceLevelLoad + " " + ToString() + " blend anim: " + anim + " in " + fadeInTime + "s.");
		}
		Animation.Blend(anim, weight, fadeInTime);
	}

	public bool PlayingInjury()
	{
		return Owner.BlackBoard.PlayInjuryTime > Time.timeSinceLevelLoad;
	}

	protected void PlayInjuryAnimation(AgentActionInjury action)
	{
		if ((PlayingInjury() && Owner.BlackBoard.NextPlayInjuryTime > Time.timeSinceLevelLoad) || !action.PlayAnim)
		{
			action.SetSuccess();
			return;
		}
		string injuryAnim = Owner.AnimSet.GetInjuryAnim(action.BodyPart, action.Destroy, action.Direction);
		Animation[injuryAnim].blendMode = AnimationBlendMode.Blend;
		Animation[injuryAnim].layer = 4;
		float num = 0.3f;
		CrossFade(injuryAnim, num, PlayMode.StopSameLayer);
		Owner.BlackBoard.PlayInjuryTime = Time.timeSinceLevelLoad + Animation[injuryAnim].length - num;
		Owner.BlackBoard.NextPlayInjuryTime = Time.timeSinceLevelLoad + 0.5f;
		action.SetSuccess();
	}
}
