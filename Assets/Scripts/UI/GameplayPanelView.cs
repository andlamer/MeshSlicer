using DG.Tweening;
using MeshSlicer.Main;
using UnityEngine;
using UnityEngine.UI;

namespace MeshSlicer.UI
{
    public class GameplayPanelView : MonoBehaviour
    {
        [SerializeField] private RectTransform textTransform;
        [SerializeField] private Button pauseMenuButton;
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private Button startGameButton;
        [SerializeField] private GameFlowManager gameFlowManager;
        [SerializeField] private float animationTime;
        [SerializeField] private RectTransform blockerRectTransform;

        private Tweener _cachedTweener;

        private void Start()
        {
            pauseMenuButton.onClick.AddListener(OnPauseMenuClick);
            startGameButton.onClick.AddListener(OnStartGameButtonClick);

            _cachedTweener = textTransform
                .DOScale(new Vector3(0.9f, 0.9f, 0.9f), animationTime)
                .SetLoops(-1, LoopType.Yoyo);
        }


        private void OnDestroy()
        {
            pauseMenuButton.onClick.RemoveAllListeners();
            startGameButton.onClick.RemoveAllListeners();
        }
        
        private void OnStartGameButtonClick()
        {
            gameFlowManager.StartGame();
            _cachedTweener.Kill();
            blockerRectTransform.gameObject.SetActive(false);
        }

        private void OnPauseMenuClick() => pauseMenu.SetActive(true);
    }
}