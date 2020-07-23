using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class animLogger : StateMachineBehaviour
{
    /*
     * A class created for the purposes of logging player actions based on animation.
     * Sends data to player log manager and then the data goes to active agents goap memory.
     * Also, they are logged in a .csv file located in project_files/report/data_report.csv . 
     */
    private string managerTag = "Manager";    
    public string playerActionLogText;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(playerActionLogText != null)
        {
            PlayerLogManager plm = GameObject.FindGameObjectWithTag(managerTag).GetComponent<PlayerLogManager>();
            plm.LogPlayerAction(playerActionLogText);
            //Debug.Log("Player attempt " + playerActionLogText);
        }
        else
        {
            Debug.Log("Failed to log player action. Text missing.");
        }
    }
}
