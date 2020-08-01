using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using UnityEngine.UI;

//V3.0
//FINAL VERSION FOR PROJECT MASTERMIND (2.4 AND PRIOR WAS NAMED PROJECT SOUL)
public sealed class GoapCore : MonoBehaviour, ILockable, IDamageable, IDamageEntity, IParryable 
{
    /*
     * One Job.
     * Create an experience.
     */

    //Tactical vars
    private FSM stateMachine;        //The state machine that will manage our AI behaviour cycle.
    private FSM.FSMState idleState;       
    private FSM.FSMState moveToState;
    private FSM.FSMState performActionState;

    private HashSet<GoapAction> availableActions; //Action pool. This is fully extendable.
    private Queue<GoapAction> currentActions;     //Current action queue.

    private IGoap dataProvider;  //Current AI class that provides world data and listens to feedback on planning.
    private GoapPlanner planner;
    private int goalGeneratorID = 1;

    private GoapMemory goapMemory; //The memory class we store information about player/agent actions.

    //Animation vars
    new Rigidbody rigidbody;
    Animator animator;
    NavMeshAgent agent;
    AnimatorHook animatorHook;
    Transform mTransform;
    Vector3 lookPosition;
    private bool planInterrupted = false; //bool for change plans after action finished!

    public float rotationSpeed = 1;
    public float moveSpeed = 2;
    
    public bool isInInterruption; //Disruption mechanics (e.g. backstab and parry)
    public bool openToBackstab = true;
    bool isInteracting;  //Performing any action
    bool actionFlag;
    public float recoveryTimer;
    public float parriedDistance = 1.5f;

    //Perception vars
    public float fovRadius = 5; 
    private LayerMask detectionLayer;
    public int targetLayer = 8; //8th layer is the player.

    //Combat vars
    public FastStats stats; //temp maybe
    public int health = 30;
    private int startingHealth;
    public int healThreshold;
    private bool isHit; 
    private float hitTimer;
    [HideInInspector]
    public Controller currentTarget;
    ActionContainer _lastAction;
    //public GameObject damageCollider; //The collider we enable/disable to deal damage.


    //Helper vars
    public int agentID = 1;
    public int lootBonusScore = 800;
    public bool enableConsoleMessages = true;
    public Transform lockOnTarget;
    private bool isAwareOnce = true;
    
    void Start()
    {
        //setting up refs
        detectionLayer = (1 << targetLayer); //Setting layer mask for player detection
        mTransform = this.transform;
        rigidbody = GetComponentInChildren<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        agent = GetComponentInChildren<NavMeshAgent>();
        rigidbody.isKinematic = false;
        animatorHook = GetComponentInChildren<AnimatorHook>();
                
        //agentID = UnityEngine.Random.Range(0, 100);
        stateMachine = new FSM();
        availableActions = new HashSet<GoapAction>();
        currentActions = new Queue<GoapAction>();
        planner = new GoapPlanner();

        FindDataProvider(); //Assigns a script as data provider for our AI.
        createIdleState();
        createMoveToState();
        createPerformActionState();

        stateMachine.pushState(idleState);
        loadActions();

        goapMemory = GetComponentInChildren<GoapMemory>();
        healThreshold =(int)(health * 0.4f);
        startingHealth = health;
    }
    private void Update()
    {
        float delta = Time.deltaTime;

        isInInterruption = animator.GetBool("interrupted");
        isInteracting = animator.GetBool("isInteracting");

        if (isHit)  //Invincible          ---TODO: Make source specific.---
        {
            if(hitTimer > 0)
            {
                hitTimer -= delta;
            }
            else
            {
                //ready to take damage again
                isHit = false;
            }
        }

        if (currentTarget == null) //means is NOT aware of player - do default goap goal here
        {
            //TODO: if dist from spawn > range threshold -> return to spawn (and reset)

            HandleDetection();

            //stateMachine.Update(this.gameObject);

            //anim stuff
        }
        else if (isInInterruption)
        {
            //HeadIK
            lookPosition = currentTarget.mTransform.position;
            lookPosition.y += 1.2f;
            animatorHook.lookAtPosition = lookPosition;

            //Move with Root Motion 
            Vector3 targetVel = animatorHook.deltaPosition * moveSpeed; //todo:draw movespeed from dataprovider
            rigidbody.velocity = targetVel;
        }
        else              //means IS aware of player 
        {            
            stateMachine.Update(this.gameObject);

            openToBackstab = true; //TODO: priority check

            //HeadIK
            lookPosition = currentTarget.mTransform.position;
            lookPosition.y += 1.2f;
            animatorHook.lookAtPosition = lookPosition;

            //Move with Root Motion 
            Vector3 targetVel = animatorHook.deltaPosition * moveSpeed; //todo:draw movespeed from dataprovider
            rigidbody.velocity = targetVel;
        }
    }

    void LateUpdate()
    {
        //Validating agent
        agent.transform.localPosition = Vector3.zero;
        agent.transform.localRotation = Quaternion.identity;

        //probably fine here
        stats.health = GetCurrentHealth();
    }

    private void FindDataProvider()  //Looks for a component with IGoap implemented, assigns it as dataProvider and then returns.
    {
        foreach(Component comp in gameObject.GetComponents(typeof(Component)))
        {
            //Type.IsAssignableFrom = Determines whether an instance of a specified type can be assigned to a variable of the current type.
            if (typeof(IGoap).IsAssignableFrom(comp.GetType()))
            {
                dataProvider = (IGoap)comp;
                return;
            }
        }
    }
    private void createIdleState() //Idle state refers to GOAP planning state.
    {
        idleState = (fsm, gameObj) => {   //lambda expression saves the day
                                          
                                          // get the world state and the goal we want to plan for
            HashSet<KeyValuePair<string, object>> worldState = dataProvider.getWorldState(); //Gets WorldState from BaseClass:InherritedClass script.
            HashSet<KeyValuePair<string, object>> goal = dataProvider.createGoalState(goalGeneratorID);

            //Plan
            Queue<GoapAction> plan = planner.plan(gameObject, availableActions, worldState, goal);
            if(plan != null)
            {
                //we have a plan, hooray!
                currentActions = plan;
                dataProvider.planFound(goal, plan);

                fsm.popState(); //move to PerformAction state
                fsm.pushState(performActionState);
            }
            else
            {
                //ugh, we couldn't get a plan
                if (enableConsoleMessages)
                {
                    Debug.Log("<color=orange>Failed Plan:</color>" + prettyPrint(goal));
                }
                dataProvider.planFailed(goal);
                fsm.popState();    //move back to IdleAction state
                fsm.pushState(idleState);
            }
        };
    }

    private void createMoveToState() //Move state refers to GOAP current action target pathfinding.
    {
        moveToState = (fsm, gameobj) => {

            GoapAction action = currentActions.Peek();
            if(action.requiresInRange() && action.target == null)
            {
                if (enableConsoleMessages)
                {
                    Debug.Log("<color=red>Fatal error:</color> Action requires a target but has none. Planning failed." +
                        "You did not assign the target in your Action.checkProceduralPrecondition()");
                }
                fsm.popState(); //remove move
                fsm.popState(); //remove perform
                fsm.pushState(idleState); //try again rtrd
                return;
            }

            //Tell the archetype class to move the agent
            //Returns true only when we are on current action target position.
            if (dataProvider.moveAgent(action))
            {
                fsm.popState();
            }
        };
    }

    private void createPerformActionState() //Perform state refers to GOAP current action target pathfinding.
    {
        performActionState = (fsm, gameObj) =>
        {
            if (!hasActionPlan())
            {
                //no actions to perform
                if (enableConsoleMessages)
                {
                    Debug.Log("<color=red>Done actions</color>");
                }
                fsm.popState();
                fsm.pushState(idleState); //reset and re-plan
                dataProvider.actionsFinished();
                return;
            }

            GoapAction action = currentActions.Peek();
            if (action.isDone())
            {
                //the action is done. Remove it so we can perform the next one
                if (planInterrupted)
                {
                    fsm.popState();
                    fsm.pushState(idleState); //reset and re-plan
                    planInterrupted = false;
                    return; //test
                }
                else
                {
                    currentActions.Dequeue();
                }             
            }

            if (hasActionPlan())
            {
                //perform the next action
                action = currentActions.Peek();

                //ternary conditional operator, evaluates a Boolean expression and returns the result of one of the two expressions
                bool inRange = action.requiresInRange() ? action.isInRange() : true;

                if (inRange)
                {
                    //we are in range, so perform the action
                    bool success = action.perform(gameObj);

                    if (!success)
                    {
                        //action failed, we need to plan again
                        fsm.popState();
                        fsm.pushState(idleState);
                        dataProvider.planAborted(action);
                    }
                }
                else
                {
                    //we need to move there first 
                    //push moveTo state
                    fsm.pushState(moveToState);
                }
            }
            else
            {
                //no actions left, move to idle state for new plan
                fsm.popState();
                fsm.pushState(idleState);
                dataProvider.actionsFinished();
            }
        };
    }
    private void loadActions() //Creating a pool of current actions from this AI.
    {
        GoapAction[] actions = gameObject.GetComponentsInChildren<GoapAction>(); //TODO: Reference child that contains the actions
        foreach (GoapAction a in actions)
        {
            availableActions.Add(a);
        }
        if (enableConsoleMessages)
        {
            //Debug.Log("Found actions on agent " + agentID + ": " + prettyPrint(actions));
        }
    }
    private bool hasActionPlan()
    {
        return currentActions.Count > 0;
    }

    /*
     * The most important method is Play Target Animation because all actions end up here.
     * GOAP Actions ARE able to call it directly.
     */
    public void PlayTargetAnimation(string targetAnim, bool toBeInteracting, float crossfadeTime = 0.2f, bool playInstantly = false)
    {
        animator.SetBool("isInteracting", toBeInteracting);

        if (!playInstantly)
        {
            animator.CrossFadeInFixedTime(targetAnim, crossfadeTime);
        }
        else
        {
            animator.Play(targetAnim);
        }
    }
    public float GetCurrentAnimationTime()
    {
        AnimatorStateInfo animationState = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] myAnimatorClip = animator.GetCurrentAnimatorClipInfo(0);
        float myTime = myAnimatorClip[0].clip.length * animationState.normalizedTime;
        //Debug.Log("Current animation time is " + myTime);
        return myTime;
    }
    public void HandleRotation(float delta, GameObject target)
    {
        Vector3 dir = target.transform.position - mTransform.position;
        dir.y = 0;
        dir.Normalize();

        if (dir == Vector3.zero)
        {
            dir = mTransform.forward;
        }

        float angle = Vector3.Angle(dir, mTransform.forward);
        if (angle > 5f)
        {
            animator.SetFloat("sideways", Vector3.Dot(dir, mTransform.right), 0.1f, delta);
        }
        else
        {
            animator.SetFloat("sideways", 0f, 0.1f, delta);
        }

        Quaternion targetRot = Quaternion.LookRotation(dir);
        mTransform.rotation = Quaternion.Slerp(mTransform.rotation, targetRot, delta / rotationSpeed);
    }

    void HandleDetection()
    {
        Collider[] cols = Physics.OverlapSphere(mTransform.position, fovRadius, detectionLayer);

        for (int i = 0; i < cols.Length; i++)
        {
            Controller controller = cols[i].transform.GetComponentInParent<Controller>();
            
            if (controller != null)
            {
                //TODO: LINECAST FOR WALLS
                IsAware(controller);
                return;
            }
        }
    }
    void IsAware(Controller target)
    {
        currentTarget = target;
        animatorHook.hasLookAtTarget = true;

        goalGeneratorID = 2;

        if (isAwareOnce) //patch --- was being called more than once sometimes
        {
            goapMemory.Init(); //Initiating the memory module.

            //NEXT LEVEL SHIT
            //agentID = GameObject.FindGameObjectWithTag("Manager").GetComponent<AI_Manager>().RegisterNewAgent(this.gameObject);

            isAwareOnce = false;
        }        
        stateMachine.popState();
        stateMachine.pushState(idleState);
    }

    #region ILockable
    public bool IsAlive()
    {
        return health > 0;
    }

    public int GetCurrentHealth()
    {
        return health;
    }

    public Transform GetLockOnTarget(Transform from)
    {
        return lockOnTarget;
    }
    #endregion ILockable 

    public void OnDamage(ActionContainer action) //OnDamageTaken***
    {
        if (action.owner == mTransform)  //Check if we hit ourselves :))
            return;

        if (!isHit)
        {
            isHit = true;  //Invincibility removal happens in Update method.
            hitTimer = 0.2f;

            //Sound
            SoundManager.PlaySound(SoundManager.Sound.EnemyHit, mTransform.position);

            //VFX
            GameObject blood = ObjectPool.GetObject("BloodFX");
            blood.transform.position = mTransform.position + Vector3.up * 1f;
            blood.transform.rotation = mTransform.rotation;
            blood.transform.SetParent(mTransform);
            blood.SetActive(true);

            // Defensive ->Pulling defensive stats here
            int totalDamageTaken = this.GetComponent<CombatStats>().CalculateFinalDamageTaken(action.damage, action.damageType);
            health -= totalDamageTaken;
            FloatingTextController.CreateFloatingText(totalDamageTaken.ToString(),this.transform.position); //Creating the floating combat text
            //Debug.Log("Agent " + agentID + " received " + totalDamageTaken + "damage. New health is " + health);

            //animatorHook.CloseDamageCollider(); //for safety


            if (health <= 0)
            {
                AgentDeath();
            }
            else if (health <= healThreshold)
            {
                Vector3 direction = action.owner.position - mTransform.position;
                float dot = Vector3.Dot(mTransform.forward, direction);

                if (action.overrideReactAnim)
                {
                    PlayTargetAnimation(action.reactAnim, true);
                }
                else
                {
                    if (dot > 0)
                    {
                        PlayTargetAnimation("Get Hit Front", true, 0f, true);
                    }
                    else
                    {
                        PlayTargetAnimation("Get Hit Back", true, 0f, true);
                    }
                }

                goapMemory.AgentLowHealth();
            }
            else
            {

                Vector3 direction = action.owner.position - mTransform.position;
                float dot = Vector3.Dot(mTransform.forward, direction);

                if (action.overrideReactAnim)
                {
                    PlayTargetAnimation(action.reactAnim, true);
                }
                else
                {
                    if (dot > 0)
                    {
                        PlayTargetAnimation("Get Hit Front", true, 0f, true);
                    }
                    else
                    {
                        PlayTargetAnimation("Get Hit Back", true, 0f, true);
                    }
                }
            }
        }
    }

    public ActionContainer GetActionContainer() //Gets current action to deal damage with it
    {
        return GetLastAction;
    }
    public ActionContainer GetLastAction
    {
        get
        {
            if (_lastAction == null)
            {
                _lastAction = new ActionContainer();
            }

            //Debug.Log("Trying: GAD-> " + currentActions.Peek().GetActionDamage() + " CurrentAct-> " +
            //    currentActions.Peek().GetType().Name + " final d-> " +
            //    this.GetComponent<CombatStats>().CalculateFinalDamageGiven(
            //    currentActions.Peek().GetActionDamage(),
            //    _lastAction.damageType));

            _lastAction.owner = mTransform; //For directional attacks

            /*
             * Queue.Peek() can bug out if we try to pull an empty action, although the system is designed to pull only when doing an action.
             */
            _lastAction.damageType = currentActions.Peek().GetActionDamageType();

            //Offensive -> Pulling offensive stats here
            _lastAction.damage = this.GetComponent<CombatStats>().CalculateFinalDamageGiven(
                currentActions.Peek().GetActionDamage(),
                _lastAction.damageType); 
            if(_lastAction.damage == 0)
            {
                _lastAction.damage = currentActions.Peek().GetActionDamage();
                if(_lastAction.damage == 0)
                {
                    _lastAction.damage = 20;
                }
            }

            //TODO: Evaluate react anim (if necessary implement in goap)
            //_lastAction.overrideReactAnim = currentSnapshot.overrideReactAnim;
            //_lastAction.reactAnim = currentSnapshot.reactAnim;

            return _lastAction;
        }
    }

    #region IParryable
    public void OnParried(Vector3 dir)
    {
        if (animatorHook.canBeParried && tag != "Dragon") //dragon doesn't have animations
        {
            if (!isInInterruption)
            {
                animatorHook.CloseDamageCollider(); //for safety

                dir.Normalize(); // to rotate agent to look at us
                dir.y = 0;
                mTransform.rotation = Quaternion.LookRotation(dir);

                HandleDetection(); //To make sure active target = attacker and re-plan through 
                //PLAY SOUND
                PlayTargetAnimation("Attack Interrupt", true, 0f, true);
            }
        }
    }

    public void GetParried(Vector3 origin, Vector3 direction)
    {
        animator.SetBool("interrupted",true);

        mTransform.position = origin + direction * parriedDistance;
        mTransform.rotation = Quaternion.LookRotation(-direction);
        PlayTargetAnimation("Getting Parried", true, 0f, true);

        //EXTRA DAMAGE DONE AI-SIDE BECAUSE REASONS
         OnSpecialStateFX(); //PATCH --- REMOVE IN FUTURE VERSIONS ---
    }
    public void GetBackstabbed(Vector3 origin, Vector3 direction)
    {
        animator.SetBool("interrupted", true);
        openToBackstab = false;

        mTransform.position = origin + direction * parriedDistance;
        mTransform.rotation = Quaternion.LookRotation(direction);
        PlayTargetAnimation("Getting Backstabbed", true, 0f, true);

        //EXTRA DAMAGE DONE AI-SIDE BECAUSE REASONS
        OnSpecialStateFX(); //PATCH --- REMOVE IN FUTURE VERSIONS ---
    }
    private void OnSpecialStateFX()
    {
        //TODO: Add direction FX & Sound

        //Sound
        SoundManager.PlaySound(SoundManager.Sound.EnemyHit, mTransform.position);

        //VFX
        GameObject blood = ObjectPool.GetObject("BloodFX");
        blood.transform.position = mTransform.position + Vector3.up * 1f;
        blood.transform.rotation = mTransform.rotation;
        blood.transform.SetParent(mTransform);
        blood.SetActive(true);

        // Defensive ->Pulling defensive stats here
        int totalDamageTaken = this.GetComponent<CombatStats>().CalculateFinalDamageTaken(15, "Physical"); //HARDCODE
        health -= totalDamageTaken;
        FloatingTextController.CreateFloatingText(totalDamageTaken.ToString(), this.transform.position); //Creating the floating combat text
        //Debug.Log("Agent " + agentID + " received SPECIAL " + totalDamageTaken + " damage. New health is " + health);

        animatorHook.CloseDamageCollider(); //for safety

        HandleDetection(); //To make sure active target = attacker and re-plan through 

        if (health <= 0)
        {
            AgentDeath();
        }
        else if(health <= healThreshold)
        {
            goapMemory.AgentLowHealth();
        }
    }
    private void AgentDeath()
    {
        SoundManager.PlaySound(SoundManager.Sound.EnemyDie, this.transform.position);

        PlayTargetAnimation("Death", true);
        animator.transform.parent = null; // in order for ragdoll to properly work

        goapMemory.AgentDeath();
        gameObject.SetActive(false); // could just destroy instead of disabling

        Text scoreT = GameObject.FindGameObjectWithTag("ScoreText").GetComponent<Text>();
        scoreT.text = (Int32.Parse(scoreT.text) + 800).ToString();

    }
    public HashSet<GoapAction> GetAvailableActions()
    {
        return availableActions;
    }
    public void IsInterruptedFromPlayer()
    {
        planInterrupted = true;
        dataProvider.planAborted(currentActions.Peek()); //informs goap memory that a plan was aborted
    }
    public void ResetPlan()
    {
        stateMachine.popState();
        stateMachine.pushState(idleState); //reset and re-plan
    }
    public void HealSelf(int amount)
    {
        //REMOVE BLOOD VFX
        health += amount;
        //canHeal?
        //Proper way would be to draw estus drink from
        //backpack script to check if its ok.
        if (health >= startingHealth)
        {
            health = startingHealth;
        }
    }
    public Transform getTransform() //Useful for dir calculations.
    {
        return mTransform;
    }
    public bool canBeParried()
    {
        return isInInterruption;
    }
    public bool canBeBackstabbed()
    {
        return openToBackstab; //Always true , this one depends on the type of AI we implement. Monster type of enemies might have this off.
    }
    #endregion
    //TODO: Generic AI methods
    public bool Reset()
    {
        return false;
    }
    public void Init()
    {

    }
    public void Despawn()
    {

    }
    public void Respawn()
    {

    }
    //HELPER METHODS
    public int GetAgentID()
    {
        return agentID;
    }
    public static string prettyPrint(HashSet<KeyValuePair<string, object>> state)
    {
        String s = "";
        foreach (KeyValuePair<string, object> kvp in state)
        {
            s += kvp.Key + ":" + kvp.Value.ToString();
            s += ", ";
        }
        return s;
    }
    public static string prettyPrint(GoapAction[] actions)
    {
        String s = "";
        foreach (GoapAction a in actions)
        {
            s += a.GetType().Name;
            s += ", ";
        }
        return s;
    }
    public static string prettyPrint(GoapAction action)
    {
        String s = "" + action.GetType().Name;
        return s;
    }
    public static string prettyPrint(Queue<GoapAction> actions)
    {
        String s = "";
        foreach (GoapAction a in actions)
        {
            s += a.GetType().Name;
            s += "-> ";
        }
        s += "GOAL";
        return s;
    }

    public FastStats GetStats()
    {
        return stats;
    }
}
