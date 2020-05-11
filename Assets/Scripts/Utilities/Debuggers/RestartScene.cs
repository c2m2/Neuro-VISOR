using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
public class RestartScene : MonoBehaviour
{
    public bool allowKeyboardRestart = true;
    public KeyCode keyboardRestartButton = KeyCode.Space;
    public UnityEvent onRestart;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (allowKeyboardRestart)
        {
            if (Input.GetKeyDown(keyboardRestartButton))
            {
               // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                onRestart.Invoke();
            }
        }
    }
}
