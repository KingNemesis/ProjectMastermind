using Packages.Rider.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GoapShortTermMemory))]
public class GoapMemory : MonoBehaviour
{
    private PlayerLogManager plm;
    private StatsManager statsManager;
    private GoapShortTermMemory goapSTM;

    private int plansCreated;
    private int plansCompleted;
    private int plansInterrupted;
    private int playerActions;

    private int combatStart;
    private int combatEnd;
    private List<string> playerActionList = new List<string>(); //current actions performed by the player
    private List<string> combatLog = new List<string>();        //all combat performed during the fight


    //On AI Aware
    public void Init()
    {
        //setting up refs
        plm = GameObject.FindGameObjectWithTag("Manager").GetComponent<PlayerLogManager>();
        statsManager = GameObject.FindGameObjectWithTag("Manager").GetComponent<StatsManager>();
        goapSTM = this.gameObject.GetComponent<GoapShortTermMemory>();
        goapSTM.Init(); //Initiating short term memory as well

        ApplyAsObserver(plm);
    }    
    #region AgentOperations
    public void AddAgentAction(string action)
    {
        combatLog.Add(action);
    }
    public void AddAgentPlan(string plan)
    {
        combatLog.Add(plan);
        plansCreated++;
    }
    public void AddAgentPlanComplete() 
    {
        plansCompleted++;
    }
    public void AddAgentPlanInterrupted() //TODO
    {
        plansInterrupted++;
    }
    public void AgentLowHealth()
    {
        //Debug.Log("GoapMemory interrupt");
        combatLog.Add("Agent Low HP");
        goapSTM.AgentLowHealth();
    }
    #endregion
    #region PlayerInteractions
    public void AddPlayerAction(string action)
    {
        playerActionList.Add(action);
        combatLog.Add(action);
        playerActions++;

        goapSTM.FilterPlayerAction(action);
    }
    public void ApplyAsObserver(PlayerLogManager plm) //On combat start
    {
        plm.AddObserver(this);
        combatStart = (int)Time.time;       
    }
    public void RemoveAsObserver(PlayerLogManager plm) //On AI death
    {
        plm.RemoveObserver(this);
        combatEnd = (int)Time.time;

        //UpdateStatsManager(); //Send all recorded data to stats manager.
    }
    #endregion

    public int GetCombatDuration()
    {
        return combatStart - combatEnd;
    }
    public void AgentDeath()
    {
        RemoveAsObserver(plm);
        UpdateStatsManager();
    }
    public void UpdateStatsManager()
    {
        int agentID = GetComponentInParent<GoapCore>().GetAgentID();
        string agentS = "Agent " + agentID;

        statsManager.LogAgent(agentS, plansCreated,plansCompleted,plansInterrupted,playerActions,GetCombatDuration(),combatLog);        
    }
}
