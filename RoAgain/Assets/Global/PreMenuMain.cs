using UnityEngine;
using UnityEngine.SceneManagement;

public class PreMenuMain : MonoBehaviour
{
    [SerializeField]
    private Rect _titlePlacement;

    [SerializeField]
    private Rect _buttonsPlacement;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnGUI()
    {
        Rect currentRect = _titlePlacement;
        GUI.Label(currentRect, "Ragnarok Online Again");
        currentRect.y += currentRect.height +5; // Next Line
        GUI.Label(currentRect, "pre-alpha 3");

        currentRect = _buttonsPlacement;
        currentRect.width /= 2;
        if(GUI.Button(currentRect, "Open as Client"))
        {
            SceneManager.LoadScene("ClientMenu");
        }

        currentRect.x += currentRect.width;
        if(GUI.Button(currentRect, "Open as Server"))
        {
            SceneManager.LoadScene("ServerScene");
        }

        currentRect = _buttonsPlacement;
        currentRect.y += currentRect.height + 5;
        if(GUI.Button(currentRect, "Open as Both"))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
