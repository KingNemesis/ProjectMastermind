using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitch : MonoBehaviour
{
    private void Start()
    {
        //Scene scene = SceneManager.GetActiveScene();

        //if (scene.buildIndex == 0)
        //{
        //    Cursor.lockState = CursorLockMode.None;
        //    Cursor.visible = true;
        //}
        //if(scene.buildIndex == 2)
        //{
        //    Cursor.lockState = CursorLockMode.Locked;
        //    Cursor.visible = false;
        //}

    }

    public void LoadScene(int index)
    {
        if(GameObject.FindGameObjectWithTag("Manager") == null)
        {
            SceneManager.LoadScene(index);
        }
        else
        {
            GameObject.FindGameObjectWithTag("Manager").GetComponent<StatsManager>().InitSave();

            SceneManager.LoadScene(index);
        }       
    }

    public void SaveAndExitGame()
    {
        GameObject.FindGameObjectWithTag("Manager").GetComponent<StatsManager>().InitSave();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public void ExitGame()
    {

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif

    }
}