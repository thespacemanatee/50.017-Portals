using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    public class MenuController : MonoBehaviour
    {
        // Events
        public UnityEvent onGameStart;

        private Image m_Background;
        private bool m_GameStarted;

        // Start is called before the first frame update
        private void Start()
        {
            m_Background = GetComponent<Image>();
            ShowMenu();
        }

        // Update is called once per frame
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                HandleResumeGame();
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                HandlePauseGame();
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }
        }

        private void HandleResumeGame()
        {
            HideMenu();
        }

        private void HandlePauseGame()
        {
            ShowMenu();
        }

        private void ShowMenu()
        {
            Time.timeScale = 0f;
            m_Background.enabled = true;
            foreach (Transform eachChild in transform)
            {
                eachChild.gameObject.SetActive(true);
            }
        }

        private void HideMenu()
        {
            Time.timeScale = 1.0f;
            m_Background.enabled = false;
            foreach (Transform eachChild in transform)
            {
                eachChild.gameObject.SetActive(false);
            }
        }
    }
}