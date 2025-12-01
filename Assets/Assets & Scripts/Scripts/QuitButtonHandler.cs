using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuitButtonHandler : MonoBehaviour
{
    public void QuitApplication()
    {
        // For development/debugging purposes only. Can be removed in final build.
        Debug.Log("Application Quit Requested.");

#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}