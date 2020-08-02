using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathPlane : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("hit" + other.name);
        if (other.CompareTag("Player"))
        {

            Debug.Log(other.name + " LOL ");
            other.GetComponent<Controller>().PlayerDeath();
        }
    }
    private void OnCollisionEnter(Collision other)
    {
        Debug.Log("hit collision" + other.collider.name);
        if (other.collider.CompareTag("Player"))
        {

            Debug.Log(other.collider.name + " LOL ");
            other.collider.GetComponent<Controller>().PlayerDeath();
        }
    }
}
