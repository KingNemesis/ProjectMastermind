using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLogManager : MonoBehaviour
{
    List<GoapMemory> observers = new List<GoapMemory>(); //Currently active agents who want information.
    private string[] playerActions = new string[200]; //Current player actions log.
    private int playerActionsCount = 0;

    public void AddObserver(GoapMemory goapMemory)
    {
        observers.Add(goapMemory);
    }
    public void RemoveObserver(GoapMemory goapMemory)
    {
        observers.Remove(goapMemory);
    }
    public void NotifyObservers(string action)
    {
        foreach (GoapMemory observer in observers)
        {
            observer.AddPlayerAction(action);
        }
    }
    public void LogPlayerAction(string action)
    {
        if(action != null)
        {
            if(playerActionsCount <= 200)
            {
                playerActions[playerActionsCount] = action;
                playerActionsCount++;
                NotifyObservers(action);
            }
            else
            {
                Debug.Log("PLM -> Player actions array is full.");
            }
        }
        else
        {
            Debug.Log("PLM -> Player action log attempt failed.");
        }
    }
}
