using System.Collections;
using UnityEngine;
using static Unity.Mathematics.math;

public abstract class Award : MonoBehaviour
{
	private const float SafeToDestroyCheckPeriod = 0.1f;

	private const float PlayerProxymityCheckPeriod = 0.2f;

	public GameObject m_RootObject;

	private Transform m_Transform;

	private Animation m_Anims;

	private AudioSource m_Audio;

	public float m_PickupDelay;

	public string m_OnPickupAnim;

	public AudioClip m_OnPickupSound;

	public ParticleSystem m_OnPickupParticle;

	private float m_Time2Check;

	public float m_DetectionDistance = 1.5f;

	private void Start()
	{
		GameObject gameObject = base.gameObject;
		m_Transform = gameObject.transform;
		m_Anims = gameObject.GetComponent<Animation>();
		m_Audio = gameObject.GetComponent<AudioSource>();
		m_Time2Check = 0f;
		if (!(m_RootObject == null))
		{
		}
	}

	private void Update()
	{
		if (!((m_Time2Check -= Time.deltaTime) <= 0f))
		{
			return;
		}
		m_Time2Check = 0.2f;
		if (!(Player.Instance != null))
		{
			return;
		}
		Vector3 rhs = m_Transform.position - Player.Pos;
		float num = rhs.x * rhs.x + rhs.z * rhs.z;
		if (num > m_DetectionDistance * m_DetectionDistance || rhs.y < -0.1f || rhs.y > 2f)
		{
			return;
		}
		float num2 = sqrt(num + rhs.y * rhs.y);
		float f = Vector3.Dot(Camera.main.transform.forward, rhs /= num2);
		if (!(Mathf.Acos(f) > 1.3962634f))
		{
			f = Vector3.Dot(m_Transform.up, rhs);
			if (!(Mathf.Acos(f) > 1.2217305f))
			{
				PickedUp();
				m_Time2Check = float.MaxValue;
			}
		}
	}

	private void PickedUp()
	{
		float time = Mathf.Max(0f, m_PickupDelay);
		Invoke("OnPickedUp", time);
		if (m_Audio != null && m_OnPickupSound != null)
		{
			m_Audio.Stop();
			m_Audio.clip = m_OnPickupSound;
			m_Audio.Play();
		}
		if (m_Anims != null && m_OnPickupAnim != string.Empty)
		{
			m_Anims.Play(m_OnPickupAnim);
		}
		if (m_OnPickupParticle != null)
		{
			m_OnPickupParticle.Play();
		}
		StartCoroutine(DestroyItself());
	}

	protected virtual void OnPickedUp()
	{
	}

	private IEnumerator DestroyItself()
	{
		if (m_Anims != null && m_OnPickupAnim != null)
		{
			while (m_Anims.isPlaying)
			{
				yield return new WaitForEndOfFrame();
			}
		}
		if (m_Audio != null && m_OnPickupSound != null)
		{
			while (m_Audio.isPlaying)
			{
				yield return new WaitForEndOfFrame();
			}
		}
		if (m_OnPickupParticle != null)
		{
			while (m_OnPickupParticle.isPlaying || m_OnPickupParticle.IsAlive())
			{
				yield return new WaitForEndOfFrame();
			}
		}
		if (IsInvoking("OnPickedUp"))
		{
			CancelInvoke("OnPickedUp");
			OnPickedUp();
		}
		m_RootObject._SetActiveRecursively(false);
	}
}
