using System;
using System.Collections;
using UnityEngine;

[AddComponentMenu("Interaction/Break Object With Anim")]
public class BreakableObject : MonoBehaviour
{
	[Serializable]
	public class InteractionParticle
	{
		public float Delay;

		public bool LinkOnRoot;
	}

	[Serializable]
	public class InteractionSound
	{
		public AudioSource Audio;

		public float Delay;

		public float Life;

		public Transform Parent;
	}

	public float Health;

	public AnimationClip AnimBreak;

	public InteractionParticle[] Emitters;

	public InteractionSound[] Sounds;

	protected bool Active = true;

	protected Transform Root;

	private Animation Animation;

	private GameObject GameObject;

	public bool IsActive
	{
		get
		{
			return Active;
		}
	}

	public Vector3 Position
	{
		get
		{
			return Root.position;
		}
	}

	public void Initialize()
	{
		GameObject = base.gameObject;
		Root = base.transform;
		Animation = GameObject.GetComponent<Animation>();
		if ((bool)Animation)
		{
			Animation.wrapMode = WrapMode.Once;
		}
	}

	private void OnProjectileHit(Projectile projectile)
	{
		if (Active)
		{
			Health -= projectile.Damage();
			if (Health < 0f)
			{
				Break();
			}
		}
	}

	public virtual void Break()
	{
		Active = false;
		if ((bool)Animation && (bool)AnimBreak)
		{
			Animation.Play(AnimBreak.name);
		}
		int num = 0;
		while (Emitters != null && num < Emitters.Length)
		{
			num++;
		}
		int num2 = 0;
		while (Sounds != null && num2 < Sounds.Length)
		{
			Mission.Instance.StartCoroutine(SoundRun(Sounds[num2].Audio, Sounds[num2].Delay));
			Mission.Instance.StartCoroutine(SoundStop(Sounds[num2].Audio, Sounds[num2].Delay + Sounds[num2].Life));
			if ((bool)Sounds[num2].Parent)
			{
				Sounds[num2].Audio.transform.parent = Sounds[num2].Parent;
			}
			num2++;
		}
		OnStart();
	}

	protected virtual void OnStart()
	{
	}

	protected virtual void OnDone()
	{
	}

	public virtual void Reset()
	{
		if ((bool)Animation && (bool)AnimBreak)
		{
			Animation.Stop();
			AnimBreak.SampleAnimation(GameObject, 0f);
		}
		Active = true;
	}

	public void Enable()
	{
		GameObject._SetActiveRecursively(true);
	}

	public void Disable()
	{
		GameObject._SetActiveRecursively(false);
	}

	private IEnumerator ParticleStop(ParticleEmitter emitter, float delay)
	{
		yield return new WaitForSeconds(delay);
		//emitter.emit = false;
		//emitter.stop();
		//PROBLEM: Glossed over the particle system handler stuff here, but this function doesn't seem to be referenced?
	}

	private IEnumerator SoundRun(AudioSource audio, float delay)
	{
		yield return new WaitForSeconds(delay);
		audio.Play();
	}

	private IEnumerator SoundStop(AudioSource audio, float delay)
	{
		yield return new WaitForSeconds(delay);
		audio.Stop();
	}
}
