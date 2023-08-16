using MeshSlicer.Slicer;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MeshSlicer.UI
{
    public class PauseMenuView : MonoBehaviour
    {
        private const string CountFormat = "<color=#3A66AE>Slices count</color> \n <color=#FD8D14> {0} </color>";
        
        [SerializeField] private Button restartButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button quitButton;

        [SerializeField] private TMP_Text countText;

        private void Start()
        {
            restartButton.onClick.AddListener(OnRestartButtonClick);
            continueButton.onClick.AddListener(OnContinueButtonClick);
            quitButton.onClick.AddListener(OnQuitButtonClick);
        }

        private void OnDestroy()
        {
            restartButton.onClick.RemoveListener(OnRestartButtonClick);
            continueButton.onClick.RemoveListener(OnContinueButtonClick);
            quitButton.onClick.RemoveListener(OnQuitButtonClick);
        }

        private void OnEnable()
        {
            Time.timeScale = 0;
            countText.text = string.Format(CountFormat, SliceManager.Instance.TotalSlicesCount);
        }

        private void OnDisable()
        {
            Time.timeScale = 1;
        }

        private void OnRestartButtonClick() => SceneManager.LoadScene(sceneBuildIndex: 0);
        private void OnContinueButtonClick() => gameObject.SetActive(false);
        private void OnQuitButtonClick() => Application.Quit();
    }   
}