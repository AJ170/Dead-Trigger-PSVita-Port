using System;
using System.Collections.Generic;
using System.Text;

public class E_EventTypesComparer : IEqualityComparer<E_EventTypes>
{
    public static readonly E_EventTypesComparer Instance =
        new E_EventTypesComparer();

    public bool Equals(E_EventTypes x, E_EventTypes y)
    {
        return x == y;
    }

    public int GetHashCode(E_EventTypes obj)
    {
        return (int)obj;
    }
}

[Serializable]
public class Memory
{
    private Dictionary<E_EventTypes, Fact> Facts;
    private List<E_EventTypes> m_FactsToRemove = new List<E_EventTypes>();

    public Memory(AgentHuman owner)
    {
        Facts = new Dictionary<E_EventTypes, Fact>(
            E_EventTypesComparer.Instance);
    }

    private void DeleteFact(Fact f)
    {
        f.Deleted = false;
        FactsFactory.Return(f);
    }

    public void AddFact(Fact fact)
    {
        Fact existing;
        if (Facts.TryGetValue(fact.Type, out existing))
        {
            fact.Delay = existing.Delay;
            DeleteFact(existing);
            Facts[fact.Type] = fact;
        }
        else
        {
            Facts.Add(fact.Type, fact);
        }
    }

    public void RemoveFact(E_EventTypes type)
    {
        Fact f;
        if (Facts.TryGetValue(type, out f))
        {
            DeleteFact(f);
            Facts.Remove(type);
        }
    }

    public bool HaveValidFact(E_EventTypes type, float minBelief)
    {
        Fact fact;
        if (!Facts.TryGetValue(type, out fact))
            return false;
        if (fact.Delay > 0f)
            return false;
        if (fact.Belief < minBelief)
            return false;
        return true;
    }

    public Fact GetValidFact(E_EventTypes type)
    {
        Fact fact;
        if (!Facts.TryGetValue(type, out fact))
            return null;
        if (fact.Delay > 0f)
            return null;
        return fact;
    }

    public Fact GetFact(E_EventTypes type)
    {
        if (Facts == null)
            return null;
        Fact fact;
        Facts.TryGetValue(type, out fact);
        return fact;
    }

    public void Reset()
    {
        foreach (Fact f in Facts.Values)
            DeleteFact(f);
        Facts.Clear();
    }

    public void Update()
    {
        m_FactsToRemove.Clear();

        foreach (KeyValuePair<E_EventTypes, Fact> fact in Facts)
        {
            fact.Value.Update();
            if (fact.Value.Belief == 0f)
                m_FactsToRemove.Add(fact.Key);
        }

        for (int i = 0; i < m_FactsToRemove.Count; i++)
            RemoveFact(m_FactsToRemove[i]);
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder("Memory : ");
        foreach (KeyValuePair<E_EventTypes, Fact> fact in Facts)
        {
            sb.Append(" ");
            sb.Append(fact.Value.Type);
            sb.Append(" belief + ");
            sb.Append(fact.Value.Belief);
            sb.Append(" delay ");
            sb.Append(fact.Value.Delay);
        }
        return sb.ToString();
    }
}