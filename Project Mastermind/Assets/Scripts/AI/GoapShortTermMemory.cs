using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoapShortTermMemory : MonoBehaviour
{
    private GoapCore goapC;
    private HashSet<GoapAction> availableActions;

    public string AI_Behaviour = "Offensive"; //Also defensive - passive
    public int playerAttacksTH = 3;
    public int playerHealsTH = 1;
    public int playerDodgeTH = 2;

    private bool healOnce = true;
    public bool playerAtkTriggerOnce = true;
    public bool playerDodgeTriggerOnce = true;
    public bool playerHealTriggerOnce = true;
    private int playerAttacks = 0;
    private int playerHeals = 0;
    private int playerDodges = 0;
    /*
     * Could also add shield vars (if shield play was available),
     * movement vars, responsive vars (how player responds to AI actions),
     * alliesInCombat vars, etc...
     */

    public void Init() //Init from memory module
    {
        goapC = this.GetComponentInParent<GoapCore>();
        availableActions = goapC.GetAvailableActions();        
    }

    public void FilterPlayerAction(string action)
    {

        if(action == "P:BasicAttack1" || action == "P:BasicAttack2" || action == "P:BasicAttack3"
            || action == "P:PunchAttack1" || action == "P:KickAttack1" || action == "P:ParryAttack")
        {
            playerAttacks++;
            if(playerAttacks >= playerAttacksTH)
            {
                PlayerAttacksTrigger();
            }
        }
        else if(action == "P:RollAction" || action == "P:StepAction")
        {
            playerDodges++;
            if(playerDodges >= playerDodgeTH)
            {
                PlayerDodgesTrigger();
            }
        }
        else if (action == "P:EstusDrinkAction")
        {
            playerHeals++;
            if(playerHeals >= playerHealsTH)
            {
                PlayerHealsTrigger();
            }
        }
        else
        {
            Debug.Log("GOAP STM -> Player did something unexpected.");
        }
    }
    public void PlayerAttacksTrigger()
    {
        //DO STUFF BASED ON AVAILABLE ACTIONS
        //what if player is offensive and we are offensive?
        if (playerAtkTriggerOnce)
        {
            if (AI_Behaviour == "Offensive")
            {
                bool berserkFound = false;

                foreach (GoapAction a in availableActions)
                {
                    if (a.GetType().Name == "BerserkAction")
                    {
                        a.cost -= 100;
                        goapC.IsInterruptedFromPlayer(); //Sets the Agent to replan after current action is finished.                
                        berserkFound = true;
                    }
                }
                if (!berserkFound)
                {
                    foreach (GoapAction a in availableActions)
                    {
                        if (a.GetType().Name == "StepBackAction")
                        {
                            a.cost -= 100;
                            goapC.IsInterruptedFromPlayer();  
                        }
                    }
                }
            }
            else if (AI_Behaviour == "Defensive")
            {
                foreach (GoapAction a in availableActions)
                {
                    if (a.GetType().Name == "StepBackAction")
                    {
                        a.cost -= 100;
                        goapC.IsInterruptedFromPlayer();  
                    }
                }
            }
            else if (AI_Behaviour == "Passive")
            {
                foreach (GoapAction a in availableActions)
                {
                    if (a.GetType().Name == "DodgyDodge") //Maybe run away here or add 2 dodgydodges
                    {
                        a.cost -= 100;
                        goapC.IsInterruptedFromPlayer();  
                    }
                }
            }

            //Debug.Log("GOAP STM -> P Attacks Trigger");
            playerAtkTriggerOnce = false;
        }        
    }    
    public void PlayerDodgesTrigger()
    {
        //player is defensive
        if(playerDodgeTriggerOnce)
        {
            if (AI_Behaviour == "Offensive")
            {
                foreach (GoapAction a in availableActions)
                {
                    if (a.GetType().Name == "ForwardRollAction") //Forward roll should be towards player in future version
                    {
                        a.cost -= 100;
                        goapC.IsInterruptedFromPlayer();
                    }
                }
            }
            else if (AI_Behaviour == "Defensive")
            {
                foreach (GoapAction a in availableActions)
                {
                    if (a.GetType().Name == "SuperAttackAction") 
                    {
                        a.cost -= 100;
                        goapC.IsInterruptedFromPlayer();
                    }
                }
            }
            else if (AI_Behaviour == "Passive")
            {
                foreach (GoapAction a in availableActions)
                {
                    if (a.GetType().Name == "DodgyDodge") 
                    {
                        a.cost -= 100;
                        goapC.IsInterruptedFromPlayer();
                    }
                }
            }
            //Debug.Log("GOAP STM -> P Dodges Trigger");
            playerDodgeTriggerOnce = false;
        }        
    }
    public void PlayerHealsTrigger()
    {
        //player cares
        if (playerHealTriggerOnce)
        {
            if (AI_Behaviour == "Offensive")
            {
                foreach (GoapAction a in availableActions)
                {
                    if (a.GetType().Name == "SuperAttackAction")
                    {
                        a.cost -= 100;
                        goapC.IsInterruptedFromPlayer();
                    }
                }
            }
            else if (AI_Behaviour == "Defensive")
            {
                bool berserkFound = false;

                foreach (GoapAction a in availableActions)
                {
                    if (a.GetType().Name == "JogBackAction")
                    {
                        a.cost -= 100;
                        goapC.IsInterruptedFromPlayer(); //Sets the Agent to replan after current action is finished.                
                        berserkFound = true;
                    }
                }
                if (!berserkFound)
                {
                    foreach (GoapAction a in availableActions)
                    {
                        if (a.GetType().Name == "ForwardRollAction")
                        {
                            a.cost -= 100;
                            goapC.IsInterruptedFromPlayer();
                        }
                    }
                }
            }
            else if (AI_Behaviour == "Passive")
            {
                //FUCK OFF DO NOTHING JESUS
            }
            //Debug.Log("GOAP STM -> P Heals Trigger");
            playerHealTriggerOnce = false;
        }        
    }
    public void AgentLowHealth() 
    {
        if (healOnce)
        {
            bool healFound = false;

            foreach (GoapAction a in availableActions)
            {
                if (a.GetType().Name == "HealAction")
                {
                    //Debug.Log("STM->Interrupt");
                    a.cost -= 100;
                    goapC.IsInterruptedFromPlayer(); //Sets the Agent to replan after current action is finished.                
                    healFound = true;
                }
            }

            if (!healFound)
            {
                //1. Add heal, update available actions and re-plan ???
                //2. Play dodgy
                foreach (GoapAction a in availableActions)
                {
                    if (a.GetType().Name == "JogBackAction")
                    {
                        a.cost -= 100;
                        goapC.IsInterruptedFromPlayer(); //Sets the Agent to replan after current action is finished.                                      
                    }
                }
            }

            healOnce = false;
        }        
    }
   
}
