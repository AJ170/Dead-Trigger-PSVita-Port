#define DEBUG
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class SpawnManager
{
	public enum E_SpawnPointSelection
	{
		Randomized = 0,
		Closest = 1,
		LeastRecentlyUsed_SpawnPoints = 2,
		LeastRecentlyUsed_SpawnZones = 3
	}

	public interface Director
	{
		void Init();

		void Update(float DeltaTime);

		bool IsEnemySpawnAllowed();

		void OnEnemySpawn(AgentHuman Enemy);

		void OnEnemyDeath(AgentHuman Enemy);

		void OnEnemyDespawn(AgentHuman Enemy);

		void OnEnemyInjured(AgentHuman Enemy, float Damage);

		void OnPlayerHealthChange(float Change);

		E_SpawnPointSelection GetSpawnPointSelection();
	}

	[Serializable]
	public class EnemyInitData : AgentsCache2.EnemyData
	{
		public AgentHuman.ModData m_Mods;
	}

	[Serializable]
	public class InitData
	{
		public List<EnemyInitData> m_Enemies;
	}

	[Serializable]
	public class EnemySpawnData
	{
		public E_AgentType m_Type;

		public float m_Prob;
	}

	[Serializable]
	public class SpawnData
	{
		public Director m_Director;

		public int m_EnemiesLimit;

		public List<AgentHuman.ModData> m_Mods;
	}

	private class EnemyTypeData
	{
		public E_AgentType m_Type;

		public AgentHuman.ModData m_Mods;

		public float m_SpawnProb;

		public float m_SpawnProbAccum;

		public int m_SpawnCounter;
	}

	private class SpawnPointRec
	{
		public SpawnPointEnemy m_SpawnPoint;

		public SpawnZoneRec m_SpawnZone;

		public int m_SpawnCounter;

		public float m_LastSpawnTime;

		public float m_Evaluation;

		public SpawnPointRec(SpawnPointEnemy Point, SpawnZoneRec ZoneRec)
		{
			m_SpawnPoint = Point;
			m_SpawnZone = ZoneRec;
			m_SpawnCounter = 0;
			m_LastSpawnTime = float.MinValue;
		}
	}

	private class SpawnZoneRec
	{
		public SpawnZone m_SpawnZone;

		public List<SpawnPointRec> m_SpawnPoints;

		public int m_SpawnCounter;

		public float m_LastSpawnTime;

		public SpawnZoneRec(SpawnZone Zone)
		{
			m_SpawnZone = Zone;
			m_SpawnPoints = new List<SpawnPointRec>();
			m_SpawnCounter = 0;
			m_LastSpawnTime = float.MinValue;
		}
	}

	public const int AliveEnemies = 6;

	public const int TotalEnemies = 8;

	private const int SchedulingCount = 10;

	private Director m_Director;

	private int m_EnemyLimit;

	private List<SpawnZoneRec> m_SpawnZones = new List<SpawnZoneRec>();

	private List<SpawnPointRec> m_SpawnPoints = new List<SpawnPointRec>();

	private Queue<int> m_EnemyQueue = new Queue<int>();

	private EnemyTypeData[] m_EnemyData;

	private int m_CounterAlive;

	private int m_CounterDead;

	private int m_CounterSpawned;

	private int m_PlanningIndex;

	private int m_VisLayerMask;

	private bool m_Enabled;

	private float m_SpawnFailTimer;

	private bool m_WasSpawnAllowed;

	public static SpawnManager Instance { get; private set; }

	public int EnemiesNum
	{
		get
		{
			return m_CounterAlive;
		}
	}

	[method: MethodImpl(32)]
	public event Action<AgentHuman> OnEnemySpawn;

	[method: MethodImpl(32)]
	public event Action<AgentHuman, float> OnEnemyInjured;

	[method: MethodImpl(32)]
	public event Action<AgentHuman> OnEnemyDead;

	[method: MethodImpl(32)]
	public event Action<AgentHuman> OnEnemyDespawn;

	public static void CreateInstance(InitData Data)
	{
		if (Instance == null)
		{
			Instance = new SpawnManager();
			Instance.Init(Data);
		}
	}

	public static void DestroyInstance()
	{
		if (Instance != null)
		{
			Instance.Done();
			Instance = null;
		}
	}

	private void Init(InitData Data)
	{
		DebugUtils.Assert(!m_Enabled);
		DebugUtils.Assert(true);
		float num = 0f;
		EnemyTypeData enemyTypeData = null;
		EnemyInitData enemyInitData = null;
		List<AgentsCache2.EnemyData> list = new List<AgentsCache2.EnemyData>(Data.m_Enemies.Count);
		foreach (EnemyInitData enemy in Data.m_Enemies)
		{
			float num2 = Mathf.Abs(enemy.m_MaxSpawnProb);
			num += num2;
			list.Add(enemy);
		}
		m_EnemyData = new EnemyTypeData[Data.m_Enemies.Count];
		for (int i = 0; i < m_EnemyData.Length; i++)
		{
			enemyInitData = Data.m_Enemies[i];
			float num2 = Mathf.Abs(enemyInitData.m_MaxSpawnProb) / num;
			enemyInitData.m_MaxSpawnProb = num2;
			enemyInitData.m_Mods.m_Type = enemyInitData.m_Type;
			enemyTypeData = new EnemyTypeData();
			enemyTypeData.m_Type = enemyInitData.m_Type;
			enemyTypeData.m_Mods = enemyInitData.m_Mods;
			enemyTypeData.m_SpawnCounter = 0;
			enemyTypeData.m_SpawnProb = 0f;
			enemyTypeData.m_SpawnProbAccum = 0f;
			m_EnemyData[i] = enemyTypeData;
		}
		Array.Sort(m_EnemyData, CompareSpawnProb);
		Mission.Instance.AgentsCache.InitEnemies(8, list);
		m_Enabled = false;
		m_PlanningIndex = 0;
		m_VisLayerMask = ~(ObjectLayerMask.Enemy | ObjectLayerMask.IgnoreRaycast);
	}

	private void Done()
	{
		m_Director = null;
		m_EnemyData = null;
		m_SpawnZones.Clear();
		m_SpawnPoints.Clear();
	}

	public void SetSpawnProbability(List<EnemySpawnData> Probs)
	{
		float num = 0f;
		foreach (EnemySpawnData Prob in Probs)
		{
			num += Mathf.Abs(Prob.m_Prob);
		}
		EnemyTypeData[] enemyData = m_EnemyData;
		foreach (EnemyTypeData enemyTypeData in enemyData)
		{
			enemyTypeData.m_SpawnProb = 0f;
			enemyTypeData.m_SpawnProbAccum = 0f;
			foreach (EnemySpawnData Prob2 in Probs)
			{
				if (Prob2.m_Type == enemyTypeData.m_Type)
				{
					enemyTypeData.m_SpawnProb = Prob2.m_Prob / num;
					break;
				}
			}
		}
		Array.Sort(m_EnemyData, CompareSpawnProb);
		if (Mission.Instance != null && Mission.Instance.CurrentGameZone != null)
		{
			foreach (Agent enemy in Mission.Instance.CurrentGameZone.Enemies)
			{
				EnemyTypeData[] enemyData2 = m_EnemyData;
				foreach (EnemyTypeData enemyTypeData2 in enemyData2)
				{
					if (enemy.AgentType == enemyTypeData2.m_Type)
					{
						enemyTypeData2.m_SpawnProbAccum -= enemyTypeData2.m_SpawnProb;
						break;
					}
				}
			}
		}
		m_PlanningIndex = 0;
		m_EnemyQueue.Clear();
	}

	public void StartSpawning(SpawnData Data)
	{
		DebugUtils.Assert(!m_Enabled);
		if (Data.m_Director == null)
		{
			return;
		}
		if (m_Director != null)
		{
			this.OnEnemySpawn = (Action<AgentHuman>)Delegate.Remove(this.OnEnemySpawn, new Action<AgentHuman>(m_Director.OnEnemySpawn));
			this.OnEnemyInjured = (Action<AgentHuman, float>)Delegate.Remove(this.OnEnemyInjured, new Action<AgentHuman, float>(m_Director.OnEnemyInjured));
			this.OnEnemyDead = (Action<AgentHuman>)Delegate.Remove(this.OnEnemyDead, new Action<AgentHuman>(m_Director.OnEnemyDeath));
			this.OnEnemyDespawn = (Action<AgentHuman>)Delegate.Remove(this.OnEnemyDespawn, new Action<AgentHuman>(m_Director.OnEnemyDespawn));
		}
		m_Director = Data.m_Director;
		m_Director.Init();
		//PROBLEM: We'll want to put a clamp on this for the Vita as Skinned Mesh renderers murderlize performance - Kano!
		m_EnemyLimit = Data.m_EnemiesLimit;
		m_CounterSpawned = 0;
		m_Enabled = true;
		m_WasSpawnAllowed = false;
		this.OnEnemySpawn = (Action<AgentHuman>)Delegate.Combine(this.OnEnemySpawn, new Action<AgentHuman>(m_Director.OnEnemySpawn));
		this.OnEnemyInjured = (Action<AgentHuman, float>)Delegate.Combine(this.OnEnemyInjured, new Action<AgentHuman, float>(m_Director.OnEnemyInjured));
		this.OnEnemyDead = (Action<AgentHuman>)Delegate.Combine(this.OnEnemyDead, new Action<AgentHuman>(m_Director.OnEnemyDeath));
		this.OnEnemyDespawn = (Action<AgentHuman>)Delegate.Combine(this.OnEnemyDespawn, new Action<AgentHuman>(m_Director.OnEnemyDespawn));
		if (Data.m_Mods != null)
		{
			foreach (AgentHuman.ModData mod in Data.m_Mods)
			{
				EnemyTypeData[] enemyData = m_EnemyData;
				foreach (EnemyTypeData enemyTypeData in enemyData)
				{
					if (enemyTypeData.m_Type == mod.m_Type)
					{
						enemyTypeData.m_Mods = mod;
						break;
					}
				}
			}
		}
		m_SpawnZones.Shuffle();
		m_SpawnPoints.Shuffle();
	}

	public void StopSpawning()
	{
		m_Enabled = false;
		m_EnemyQueue.Clear();
	}

	public void Update(float DeltaTime)
	{
		if (!m_Enabled)
		{
			return;
		}
		m_Director.Update(DeltaTime);
		bool flag = m_EnemyLimit <= 0 || m_CounterSpawned < m_EnemyLimit;
		if (!(flag & (m_CounterAlive < 6 && m_CounterAlive + m_CounterDead < 8)))
		{
			return;
		}
		if (m_WasSpawnAllowed || m_Director.IsEnemySpawnAllowed())
		{
			if (!SpawnEnemy())
			{
				m_WasSpawnAllowed = true;
				if ((m_SpawnFailTimer += DeltaTime) > 2.5f)
				{
					m_EnemyQueue.Dequeue();
				}
				return;
			}
			m_WasSpawnAllowed = false;
		}
		m_SpawnFailTimer = 0f;
	}

	private void ScheduleNextEnemies()
	{
		int num = 0;
		while (num < 10)
		{
			EnemyTypeData enemyTypeData = m_EnemyData[m_PlanningIndex];
			enemyTypeData.m_SpawnProbAccum += UnityEngine.Random.Range(0f, enemyTypeData.m_SpawnProb);
			if (enemyTypeData.m_SpawnProbAccum >= 1f)
			{
				enemyTypeData.m_SpawnProbAccum -= 1f;
				num++;
				m_EnemyQueue.Enqueue(m_PlanningIndex);
			}
			m_PlanningIndex = (m_PlanningIndex + 1) % m_EnemyData.Length;
		}
	}

	public bool SpawnEnemy(E_AgentType Type, SpawnPointEnemy Location)
	{
		EnemyTypeData[] enemyData = m_EnemyData;
		foreach (EnemyTypeData enemyTypeData in enemyData)
		{
			if (enemyTypeData.m_Type == Type)
			{
				return SpawnEnemy(enemyTypeData, Location);
			}
		}
		return false;
	}

	private bool SpawnEnemy()
	{
		DebugUtils.Assert(m_CounterAlive < 6);
		if (m_EnemyQueue.Count == 0)
		{
			ScheduleNextEnemies();
		}
		int num = m_EnemyQueue.Peek();
		EnemyTypeData enemyTypeData = m_EnemyData[num];
		SpawnPointRec spawnPointRec = SelectSpawnPoint(enemyTypeData.m_Type);
		if (spawnPointRec == null)
		{
			return false;
		}
		if (!SpawnEnemy(enemyTypeData, spawnPointRec.m_SpawnPoint))
		{
			return false;
		}
		m_SpawnFailTimer = 0f;
		m_EnemyQueue.Dequeue();
		enemyTypeData.m_SpawnCounter++;
		spawnPointRec.m_SpawnCounter++;
		spawnPointRec.m_LastSpawnTime = Time.timeSinceLevelLoad;
		spawnPointRec.m_SpawnZone.m_SpawnCounter++;
		spawnPointRec.m_SpawnZone.m_LastSpawnTime = Time.timeSinceLevelLoad;
		m_CounterAlive++;
		m_CounterSpawned++;
		return true;
	}

	private bool SpawnEnemy(EnemyTypeData Data, SpawnPointEnemy Location)
	{
		if (Location == null)
		{
			return false;
		}
		GameObject agentFromCache = Mission.Instance.GetAgentFromCache(Data.m_Type);
		if (agentFromCache == null)
		{
			return false;
		}
		agentFromCache.SendMessage("ApplyModifications", Data.m_Mods);
		agentFromCache.SendMessage("Activate", Location);
		if (this.OnEnemySpawn != null)
		{
			AgentHuman component = agentFromCache.GetComponent<AgentHuman>();
			this.OnEnemySpawn(component);
		}
		return true;
	}

	private bool CanBeUsed(SpawnPointRec Rec, E_AgentType EnemyType)
	{
		float num = Time.timeSinceLevelLoad - Rec.m_LastSpawnTime;
		if (num < Rec.m_SpawnPoint.RespawnTime)
		{
			return false;
		}
		if (Rec.m_SpawnPoint.m_AllowedTypes != null && Rec.m_SpawnPoint.m_AllowedTypes.Length > 0)
		{
			bool flag = false;
			E_AgentType[] allowedTypes = Rec.m_SpawnPoint.m_AllowedTypes;
			foreach (E_AgentType e_AgentType in allowedTypes)
			{
				flag = flag || e_AgentType == EnemyType;
			}
			if (!flag)
			{
				return false;
			}
		}
		Vector3 position = Rec.m_SpawnPoint.Transform.position;
		if (Vector3.Distance(position, Player.Pos) < 1.5f || Mission.Instance.CurrentGameZone.GetDistanceToNearestEnemy(position, null) < 1.5f)
		{
			return false;
		}
		if (Rec.m_SpawnPoint.m_CheckVisibility)
		{
			Vector3 eyePosition = Player.Instance.Owner.EyePosition;
			Vector3 dir = Player.Dir;
			Vector3 rhs = position - eyePosition;
			if (Vector3.Dot(dir, rhs) >= 0f && !Physics.Linecast(eyePosition, position, m_VisLayerMask))
			{
				return false;
			}
		}
		return true;
	}

	private static int CompareByEvaluation(SpawnPointRec A, SpawnPointRec B)
	{
		return A.m_Evaluation.CompareTo(B.m_Evaluation);
	}

	private SpawnPointRec SelectSpawnPoint(E_AgentType EnemyType)
	{
		switch (m_Director.GetSpawnPointSelection())
		{
		case E_SpawnPointSelection.Closest:
			EvaluateClosest();
			break;
		case E_SpawnPointSelection.LeastRecentlyUsed_SpawnPoints:
			EvaluateLRU();
			break;
		case E_SpawnPointSelection.LeastRecentlyUsed_SpawnZones:
			EvaluateZoneLRU();
			break;
		default:
			EvaluateRandom();
			break;
		}
		m_SpawnPoints.Sort(CompareByEvaluation);
		foreach (SpawnPointRec spawnPoint in m_SpawnPoints)
		{
			if (CanBeUsed(spawnPoint, EnemyType))
			{
				return spawnPoint;
			}
		}
		return null;
	}

	private void EvaluateRandom()
	{
		foreach (SpawnPointRec spawnPoint in m_SpawnPoints)
		{
			spawnPoint.m_Evaluation = UnityEngine.Random.value;
		}
	}

	private void EvaluateLRU()
	{
		foreach (SpawnPointRec spawnPoint in m_SpawnPoints)
		{
			spawnPoint.m_Evaluation = spawnPoint.m_LastSpawnTime;
		}
	}

	private void EvaluateZoneLRU()
	{
		foreach (SpawnPointRec spawnPoint in m_SpawnPoints)
		{
			spawnPoint.m_Evaluation = spawnPoint.m_SpawnZone.m_LastSpawnTime + 2f * spawnPoint.m_LastSpawnTime;
		}
	}

	private void EvaluateClosest()
	{
		Vector3 pos = Player.Pos;
		foreach (SpawnPointRec spawnPoint in m_SpawnPoints)
		{
			spawnPoint.m_Evaluation = Vector3.SqrMagnitude(pos - spawnPoint.m_SpawnPoint.Transform.position);
		}
	}

	public void EnemyInjured(AgentHuman Enemy, float Damage)
	{
		if (this.OnEnemyInjured != null)
		{
			this.OnEnemyInjured(Enemy, Damage);
		}
	}

	public void EnemyDied(AgentHuman Enemy)
	{
		m_CounterDead++;
		m_CounterAlive--;
		if (this.OnEnemyDead != null)
		{
			this.OnEnemyDead(Enemy);
		}
	}

	public void EnemyDespawned(AgentHuman Enemy)
	{
		m_CounterDead--;
		if (this.OnEnemyDespawn != null)
		{
			this.OnEnemyDespawn(Enemy);
		}
	}

	public void PlayerActivated(AgentHuman Agent)
	{
		if (Agent != null)
		{
			Agent.OnHealthChanged += PlayerHealthChanged;
		}
	}

	public void PlayerDeactivated(AgentHuman Agent)
	{
		if (Agent != null)
		{
			Agent.OnHealthChanged -= PlayerHealthChanged;
		}
	}

	private void PlayerHealthChanged(AgentHuman Agent, float HealthChange)
	{
		if (m_Director != null)
		{
			m_Director.OnPlayerHealthChange(HealthChange);
		}
	}

	public void RegisterSpawnZone(SpawnZone Zone)
	{
		SpawnZoneRec spawnZoneRec = GetSpawnZoneRec(Zone);
		if (spawnZoneRec == null)
		{
			spawnZoneRec = new SpawnZoneRec(Zone);
			m_SpawnZones.Add(spawnZoneRec);
			SpawnPointEnemy[] componentsInChildren = Zone.GetComponentsInChildren<SpawnPointEnemy>();
			SpawnPointEnemy[] array = componentsInChildren;
			foreach (SpawnPointEnemy point in array)
			{
				SpawnPointRec item = new SpawnPointRec(point, spawnZoneRec);
				spawnZoneRec.m_SpawnPoints.Add(item);
			}
		}
	}

	public void OnSpawnZoneStateChange(SpawnZone Zone)
	{
		SpawnZoneRec spawnZoneRec = GetSpawnZoneRec(Zone);
		if (Zone.gameObject.activeInHierarchy)
		{
			foreach (SpawnPointRec spawnPoint in spawnZoneRec.m_SpawnPoints)
			{
				m_SpawnPoints.AddUnique(spawnPoint);
			}
			return;
		}
		foreach (SpawnPointRec spawnPoint2 in spawnZoneRec.m_SpawnPoints)
		{
			m_SpawnPoints.Remove(spawnPoint2);
		}
	}

	private SpawnZoneRec GetSpawnZoneRec(SpawnZone Zone)
	{
		DebugUtils.Assert(Zone != null);
		foreach (SpawnZoneRec spawnZone in m_SpawnZones)
		{
			if (spawnZone.m_SpawnZone == Zone)
			{
				return spawnZone;
			}
		}
		return null;
	}

	private static int CompareSpawnProb(EnemyTypeData A, EnemyTypeData B)
	{
		return A.m_SpawnProb.CompareTo(B.m_SpawnProb);
	}
}
