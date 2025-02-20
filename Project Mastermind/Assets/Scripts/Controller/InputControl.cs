﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InputControl : MonoBehaviour
{
    //Input Manager but named to InputControl to avoid conflicts
    public CameraManager cameraManager;
    public Controller controller;
    public Transform camTransform;

    //Triggers & Bumpers
    bool Rb;
    bool Rt;
    bool Lb;
    bool Lt;

    bool LMDown;
    bool RMDown;

    //Inventory
    bool inventoryInput;

    //Prompts
    bool b_Input; //roll
    bool y_Input; //pick up
    bool x_Input; //lock on
    bool a_Input; //use item
    bool FKey_Input;
    bool GKey_Input;
    bool VKey_Input;
    bool XKey_Input;

    //DPad
    //float DPadHor;
    //float DPadVer;

    //public static bool IsLeft, IsRight, IsUp, IsDown;
    //private float _LastX, _LastY;

    //bool leftArrow;
    //bool rightArrow;
    //bool upArrow;
    //bool downArrow;

    //Logic
    bool isAttacking;

    float vertical;
    float horizontal;
    float moveAmount;
    float mouseX;
    float mouseY;
    bool rollFlag;
    float rollTimer;

    public PlayerProfile playerProfile;

    ILockable currentLockable;

    public ExecutionOrder cameraMovement;

    public enum ExecutionOrder
    {
        fixedUpdate, update, lateUpdate
    }

    private void Start()
    {
        camTransform = Camera.main.transform;

        controller.Init();
        controller.InitInventory(playerProfile);

        //controller.SetWeapons(rm.GetItem(playerProfile.rightHandWeapon), rm.GetItem(playerProfile.leftHandWeapon));

        cameraManager.targetTransform = controller.transform;

        Settings.interactionsLayer = (1 << 13);
    }

    private void FixedUpdate()
    {
        if (controller == null)
            return;

        float delta = Time.fixedDeltaTime;

        HandleMovement(delta);
        cameraManager.FollowTarget(delta);

        if (cameraMovement == ExecutionOrder.fixedUpdate)
        {
            cameraManager.HandleRotation(delta, mouseX, mouseY);
        }
    }

    private void Update()
    {
        if (controller == null)
            return;

        float delta = Time.deltaTime;

        HandleInput();

        if (b_Input && !controller.isRolling)
        {
            rollFlag = true;
            rollTimer += delta;
        }

        if (XKey_Input || a_Input)
        {
            if (!controller.isInteracting)
            {
                string targetAnim = "";
                if (controller.inventoryManager.TryToConsumeItem(ref targetAnim))
                {
                    controller.PlayTargetAnimation(targetAnim, true);
                }
            }
        }

        if (cameraMovement == ExecutionOrder.update)
        {
            cameraManager.HandleRotation(delta, mouseX, mouseY);
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            SceneManager.LoadScene(0); //reload first scene
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            SceneManager.LoadScene(1); //reload first scene
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            SceneManager.LoadScene(2); //reload first scene
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TempUI.singleton.HandlePauseMenu();
            //if(Cursor.visible == true)
            //{
            //    Cursor.lockState = CursorLockMode.Locked;
            //    Cursor.visible = false;
            //}
            //else
            //{
            //    Cursor.lockState = CursorLockMode.None;
            //    Cursor.visible = true;
            //}
          
        }

        //Debug.Log(Input.anyKeyDown);

        if (Input.GetKeyDown(KeyCode.Alpha1) || DPadButtons.IsLeft)
        {
            HandleSwitchWeapons(true);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) || DPadButtons.IsRight)
        {
            HandleSwitchWeapons(false);
        }

    }

    private void LateUpdate()
    {
        if (cameraMovement == ExecutionOrder.lateUpdate)
        {
            //cameraManager.FollowTarget(Time.deltaTime);
        }

        if (VKey_Input || y_Input)
        {
            HandleInteractions();
        }

        HandleInteractionDetection();
    }

    void HandleSwitchWeapons(bool isLeft)
    {
        if (controller.isInteracting)
            return;

        controller.inventoryManager.SwitchWeapon(isLeft);
    }

    void HandleMovement(float delta)
    {
        Vector3 movementDirection = camTransform.right * horizontal;
        movementDirection += camTransform.forward * vertical;
        movementDirection.Normalize();

        controller.MoveCharacter(vertical, horizontal, movementDirection, delta);
    }

    void HandleInput()
    {
        bool retVal = false;
        isAttacking = false;

        vertical = Input.GetAxis("Vertical");
        horizontal = Input.GetAxis("Horizontal");
        moveAmount = Mathf.Clamp01(Mathf.Abs(vertical) + Mathf.Abs(horizontal));

        Rb = Input.GetButton("RB");
        Rt = Input.GetButton("RT");
        Lb = Input.GetButton("LB");
        Lt = Input.GetButton("LT");

        inventoryInput = Input.GetButton("Inventory");

        b_Input = Input.GetButton("B");
        y_Input = Input.GetButtonDown("Y");
        x_Input = Input.GetButtonDown("X");
        a_Input = Input.GetButtonDown("A");
        FKey_Input = Input.GetKeyDown(KeyCode.F); //need to add to input profile
        GKey_Input = Input.GetKeyDown(KeyCode.G); //need to add to input profile
        VKey_Input = Input.GetKeyDown(KeyCode.V); //need to add to input profile
        XKey_Input = Input.GetKeyDown(KeyCode.X); //need to add to input profile

        LMDown = Input.GetMouseButton(0);
        RMDown = Input.GetMouseButton(1);

        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        if (!controller.isInteracting)
        {
            if (retVal == false)
            {
                retVal = HandleRolls();
            }
        }

        if (retVal == false)
        {
            retVal = HandleAttacking();
        }

        if (FKey_Input || x_Input)
        {
            if (controller.lockOn)
            {
                DisableLockOn();
            }
            else
            {
                Transform lockTarget = null;

                currentLockable = controller.FindLockableTarget();
                if (currentLockable != null)
                {
                    lockTarget = currentLockable.GetLockOnTarget(controller.mTransform);
                }

                if (lockTarget != null)
                {
                    cameraManager.lockTarget = lockTarget;
                    controller.lockOn = true;
                    controller.currentLockTarget = lockTarget;
                    controller.canvasOnLockTarget = lockTarget;
                }
                else
                {
                    cameraManager.lockTarget = null;
                    controller.lockOn = false;
                }
            }
        }

        if (controller.lockOn)
        {
            controller.canvasOnLockTarget.parent.GetComponent<ObjectCanvasController>().isAware(); // dirty

            if (!currentLockable.IsAlive())
            {
                DisableLockOn();
            }
        }
    }

    void DisableLockOn()
    {
        cameraManager.lockTarget = null;
        controller.lockOn = false;
        controller.currentLockTarget = null;
        currentLockable = null;
        controller.canvasOnLockTarget.parent.GetComponent<ObjectCanvasController>().isNotAware(); // more dirt

    }

    bool HandleAttacking()
    {
        AttackInputs attackInput = AttackInputs.none;

        if (Rb || Rt || Lb || Lt || LMDown || RMDown)
        {
            isAttacking = true;
            if (Lb || LMDown)
            {
                attackInput = AttackInputs.rb;
            }
            if (Rt)
            {
                attackInput = AttackInputs.rt;
            }
            if (Rb || RMDown)
            {
                attackInput = AttackInputs.lb;
            }
            if (Lt)
            {
                attackInput = AttackInputs.lt;
            }
        }

        if (y_Input || GKey_Input)
        {
        }

        if (attackInput != AttackInputs.none)
        {
            if (!controller.isInteracting)
            {
                controller.PlayTargetItemAction(attackInput);
            }
            else
            {
                if (controller.animatorHook.canDoCombo)
                {
                    controller.DoCombo(attackInput);
                }
            }
        }

        return isAttacking;
    }

    bool HandleRolls()
    {
        controller.isSprinting = false;
        controller.isRolling = true;

        if (b_Input == false && rollFlag)
        {
            rollFlag = false;

            if (rollTimer < 0.5f)
            {
                if (moveAmount > 0)
                {
                    Vector3 movementDirection = camTransform.right * horizontal;
                    movementDirection += camTransform.forward * vertical;
                    movementDirection.Normalize();
                    movementDirection.y = 0;

                    Quaternion dir = Quaternion.LookRotation(movementDirection);
                    controller.transform.rotation = dir;
                    controller.PlayTargetAnimation("Roll", true, false, 1.5f);
                    controller.stats.AssignRollCost(false);
                    return true;

                }
                else
                {
                    controller.PlayTargetAnimation("Step", true, false);
                    controller.stats.AssignRollCost(true);
                }
            }
        }
        else if (rollFlag)
        {
            if (moveAmount > 0.5f)
            {
                if(controller.stats.stamina > 0)
                {
                    controller.isSprinting = true;
                }
            }
        }

        if (b_Input == false)
        {
            rollTimer = 0;
        }

        controller.isRolling = false;
        return false;
    }

    IInteractable currentInteractable;

    void HandleInteractionDetection()
    {
        TempUI tempUI = TempUI.singleton;
        currentInteractable = null;
        tempUI.ResetInteraction();

        Collider[] colliders = Physics.OverlapSphere(controller.mTransform.position, 1.5f, Settings.interactionsLayer);

        for (int i = 0; i < colliders.Length; i++)
        {
            IInteractable interactable = colliders[i].transform.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                currentInteractable = interactable;
                tempUI.LoadInteraction(interactable.GetInteractionType());
                break;
            }
        }
    }

    void HandleInteractions()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnInteract(this);
        }
    }

}
