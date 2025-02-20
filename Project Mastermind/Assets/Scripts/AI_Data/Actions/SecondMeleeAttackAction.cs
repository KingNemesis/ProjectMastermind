﻿using System;
using UnityEngine;
using UnityEngine.AI;

public class SecondMeleeAttackAction : GoapAction
{

    //dummy for now
    //only added to be able to create plan

    private bool severeDamagedEnemy = false;
    private GameObject enemy; // what enemy we attack
    private string playerTag = "Player";

    public SecondMeleeAttackAction()
    {
        addPrecondition("hasWeapon", true); // don't bother attacking when no weapon in hands
        addPrecondition("damagedEnemy", true);
        addEffect("severeDamagedEnemy", true); // kick his ass
    }


    public override void reset()
    {
        severeDamagedEnemy = false;
        enemy = null;
    }

    public override bool isDone()
    {
        return severeDamagedEnemy; 
    }

    public override bool requiresInRange()
    {
        return true; // yes we need to be near an enemy to attack.
    }

    public override bool checkProceduralPrecondition(GameObject agent)
    {
        // find the nearest player
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag); //can support multiplayer
        GameObject closest = null;
        float closestDist = 0;

        foreach (GameObject player in players)
        {
            if (closest == null)
            {
                // first one, so choose it for now
                closest = player;
                closestDist = (player.transform.position - agent.transform.position).magnitude;
            }
            else
            {
                // is this one closer than the last?
                float dist = (player.transform.position - agent.transform.position).magnitude;
                if (dist < closestDist)
                {
                    // we found a closer one, use it
                    closest = player;
                    closestDist = dist;
                }
            }
        }
        if (closest == null)
            return false;

        enemy = closest;
        target = enemy;  //target is defined in GoapAction

        return closest != null;
    }

    public override bool perform(GameObject agent)
    {
        //TODO: WILL OPTIMIZE ANIM/NAVAGENT REFS IN LATER VERSION.
        Animator anim = (Animator)agent.GetComponentInChildren(typeof(Animator));
        NavMeshAgent navAgent = (NavMeshAgent)agent.GetComponentInChildren(typeof(NavMeshAgent));

        //Becomes true during the period of attack
        if (anim.GetBool("isAnimating_AI") != true) //Did we start animating an action...
        {
            navAgent.isStopped = true;             //...lets stop the agent for a bit shall we?
            //Becomes true only on exit 
            if (anim.GetBool("actionSuccess_AI")) //...if action is complete and successful
            {
                /*
                 * Here we are sure we finished the animation
                 * so it's possible we can get actual damagedEnemy
                 * status from player and evaluate attack success.
                 */
                severeDamagedEnemy = true; //... effect is true so we can move to next action
                navAgent.isStopped = false;
                anim.SetBool("actionSuccess_AI", false);
                Debug.Log("Attack has ended!");

                return true;
            }

            anim.CrossFade("Attack 2", 0.25f);
            //PLAY SOUND/UI STUFF HERE
            Debug.Log("Attack 2 at: " + Time.time);
        }
        return true;
    }
}
