﻿using System;
using UnityEngine;
using UnityEngine.AI;

public class A_ThirdDodgeAction : GoapAction
{
    private bool trickEnemy = false;
    private GameObject enemy; // what enemy we attack
    private string playerTag = "Player";
    private string animAction = "roll_backwards_left";
    private bool actionFlag = false;
    private float recoveryTimer;

    public float costRaisePerUse = 10f;

    public A_ThirdDodgeAction()
    {
        addPrecondition("hasWeapon", true); // don't bother attacking when no weapon in hands
        addPrecondition("damagedEnemy", true);
        addPrecondition("severeDamagedEnemy", true);
        addEffect("trickEnemy", true); // ARE YOU KIDDING ME MOMENT
    }


    public override void reset()
    {
        trickEnemy = false;
        enemy = null;

        actionFlag = false;
    }

    public override bool isDone()
    {
        return trickEnemy; // TODO: TRACK LATER ISDONE CONDITION
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
        //GameObject damageCollider = agent.GetComponent<GoapCore>().damageCollider;
        AnimatorHook animatorHook = agent.GetComponentInChildren<AnimatorHook>();
        GoapMemory goapM = agent.GetComponentInChildren<GoapMemory>();

        //navAgent.enabled = false;

        //anim.SetFloat("movement", 0f, 0.1f, Time.deltaTime);
        //anim.SetFloat("sideways", 0f, 0.1f, Time.deltaTime);

        //Becomes true only on animator exit script... 
        if (anim.GetBool("actionSuccess_AI")) //...if action is complete and successful
        {
            /*
             * Here we are sure we finished the animation
             * so it's possible we can get actual damagedEnemy
             * status from player and evaluate attack success.
             */
            trickEnemy = true; //... effect is true so we can move to next action
            cost += costRaisePerUse;
            navAgent.enabled = true;
            animatorHook.CloseDamageCollider();
            anim.SetBool("actionSuccess_AI", false);
            goapM.AddAgentAction(animAction);
            //Debug.Log("Attack 3 has ended!");

            return true;
        }

        //Becomes true during the period of attack OR getting disabled by enemy.
        if (anim.GetBool("isInteracting") != true) //Did we start animating an action...
        {
            navAgent.enabled = false;             //...lets stop the agent for a bit shall we?

            anim.SetFloat("movement", 0f, 0.1f, Time.deltaTime);
            anim.SetFloat("sideways", 0f, 0.1f, Time.deltaTime);

            if (actionFlag)                                  //Check if action is happening...
            {
                navAgent.enabled = false;             //...lets stop the agent for a bit shall we?

                anim.SetFloat("movement", 0f, 0.1f, Time.deltaTime);
                anim.SetFloat("sideways", 0f, 0.1f, Time.deltaTime);

                recoveryTimer -= Time.deltaTime;
                if (recoveryTimer <= 0)
                {
                    Debug.Log("Action Flag finished.");
                    actionFlag = false;
                }
            }
            else                                              //...else do my action
            {
                Vector3 dir = target.transform.position - agent.transform.position;
                dir.y = 0;
                dir.Normalize();
                float dot = Vector3.Dot(transform.forward, dir);

                //Debug.Log(animAction + " " + dot);

                if (dot < 0) //Checking if target is in behind so we turn...
                {
                    Transform mTransform = agent.transform;

                    navAgent.enabled = true;
                    navAgent.SetDestination(target.transform.position);

                    Vector3 relativeDirection = mTransform.InverseTransformDirection(navAgent.desiredVelocity);
                    relativeDirection.Normalize();

                    anim.SetFloat("movement", relativeDirection.z, 0.1f, Time.deltaTime);
                    anim.SetFloat("sideways", relativeDirection.x, 0.1f, Time.deltaTime);


                    mTransform.rotation = navAgent.transform.rotation;
                    return true;
                }
                else      //...otherwise perform action
                {
                    /*
                     * TODO: Calculate with navMesh preview a place where dodge could be done 
                     * and make helper method to set proper animation.
                     */

                    agent.GetComponent<GoapCore>().PlayTargetAnimation(this.animAction, true);
                    actionFlag = true;
                    animatorHook.CloseDamageColliders();
                    recoveryTimer = agent.GetComponent<GoapCore>().GetCurrentAnimationTime();
                    if(recoveryTimer >= 1f)
                    {
                        recoveryTimer = 1f;
                    }
                    SoundManager.PlaySound(SoundManager.Sound.Roll, this.transform.position);
                }
            }
        }

        return true;
    }
}