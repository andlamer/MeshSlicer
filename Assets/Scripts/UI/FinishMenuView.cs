using MeshSlicer.Main;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MeshSlicer.UI
{
    public class FinishMenuView : MonoBehaviour
    {
        [SerializeField] private Button restartButton;
        [SerializeField] private Button finishButton;
        [SerializeField] private GameFlowManager gameFlowManager;   

        private void Awake()
        {
            gameFlowManager.OnGameFinish += Show;
            gameObject.SetActive(false);
        }

        private void Start()
        {
            restartButton.onClick.AddListener(OnRestartClick);
            finishButton.onClick.AddListener(OnQuitClick);
        }

        private void OnDestroy()
        {
            restartButton.onClick.RemoveListener(OnRestartClick);
            finishButton.onClick.RemoveListener(OnQuitClick);
            gameFlowManager.OnGameFinish -= Show;
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        private void OnRestartClick() => SceneManager.LoadScene(sceneBuildIndex: 0);
        private void OnQuitClick() => Application.Quit();
    }
}