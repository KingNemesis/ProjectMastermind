using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class JogBackAction : GoapAction
{
    private bool damagedEnemy = false;
    private GameObject enemy; // what enemy we attack
    private string playerTag = "Player";
    private string animAction = "Jog_backwards";
    private bool actionFlag = false;
    private float recoveryTimer;

    public float costRaisePerUse = 100f;

    public JogBackAction()
    {
        addPrecondition("hasWeapon", true); // don't bother attacking when no weapon in hands
        addEffect("damagedEnemy", true); // Not the proper way but works!!!
    }


    public override void reset()
    {
        damagedEnemy = false;
        enemy = null;

        actionFlag = false;
    }

    public override bool isDone()
    {
        return damagedEnemy; // TODO: TRACK LATER ISDONE CONDITION
    }

    public override bool requiresInRange()
    {
        return false; // yes we need to be near an enemy to attack.
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
        GoapCore goapC = agent.GetComponentInParent<GoapCore>();

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
            damagedEnemy = true; //... effect is true so we can move to next action
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
                /*
                 * TODO: Calculate with navMesh preview a place where dodge could be done 
                 * and make helper method to set proper animation.
                 */
                agent.GetComponent<GoapCore>().PlayTargetAnimation(this.animAction, true);
                actionFlag = true;
                animatorHook.CloseDamageColliders(); //close because heal
                recoveryTimer = agent.GetComponent<GoapCore>().GetCurrentAnimationTime();
                if (recoveryTimer >= 1f)
                {
                    recoveryTimer = 1f;
                }
                SoundManager.PlaySound(SoundManager.Sound.GenericStep, this.transform.position);
            }
        }
        return true;
    }
}
