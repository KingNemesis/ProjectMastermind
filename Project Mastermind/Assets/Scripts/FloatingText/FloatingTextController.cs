using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingTextController : MonoBehaviour
{
    private static FloatingText popupText;
    private static GameObject canvas;

    public static void Initialize()
    {
        //Debug.Log("FloatingText init");
        canvas = GameObject.FindGameObjectWithTag("MainCanvas");
        if(!popupText)
        popupText = Resources.Load<FloatingText>("Prefabs/PopupTextParent");
    }
    public static void CreateFloatingText(string text, Vector3 position)
    {
        FloatingText instance = Instantiate(popupText);
        //Vector2 screenPosition = Camera.main.WorldToScreenPoint(
         //                      new Vector2( position.x, position.y
           //                    ));
        //position.x + Random.Range(-0.5f, 0.5f), position.y +Random.Range(1.5f, 2f)
        instance.transform.SetParent(canvas.transform, false);
        //instance.transform.position = screenPosition;
        instance.SetText(text);
    }
}
