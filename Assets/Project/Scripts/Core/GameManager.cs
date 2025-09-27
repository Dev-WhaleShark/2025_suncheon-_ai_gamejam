using UnityEngine;
using WhaleShark.Core;

namespace WhaleShark.Gameplay
{
    public class GameManager : Singleton<GameManager>
    {
        [Header("Game State")]
        public bool isPaused = false;

        /// <summary>게임 시작 후 경과 시간</summary>
        public float gameTime = 0f;

        public bool IsGameStarted = false;

        /// <summary>
        /// 이벤트 구독 등록
        /// 게임 시작 시 필요한 이벤트들을 구독합니다
        /// </summary>
        void Start()
        {
            EventBus.PauseToggled += OnPauseToggled;
        }

        void OnDestroy()
        {
            EventBus.PauseToggled -= OnPauseToggled;
        }

        /// <summary>
        /// 매 프레임 업데이트
        /// 게임 시간 카운트 및 입력 처리
        /// </summary>
        void Update()
        {
            if (!isPaused && IsGameStarted )
            {
                gameTime += Time.deltaTime;
            }
        }

        /// <summary>
        /// 게임 일시정지 상태를 토글합니다
        /// </summary>
        public void TogglePause()
        {
            isPaused = !isPaused;
            EventBus.PublishPause(isPaused);
        }

        /// <summary>
        /// 일시정지 상태 변경 이벤트 핸들러
        /// </summary>
        /// <param name="paused">일시정지 여부</param>
        void OnPauseToggled(bool paused)
        {
            isPaused = paused;
        }

        /// <summary>
        /// 플레이어 사망 이벤트 핸들러
        /// 게임 오버 처리 및 데이터 저장
        /// </summary>
        public void OnPlayerDied()
        {
            EventBus.PublishPlayerDied();
            Debug.Log("Player died!");
        }

        public void StartPrologue()
        {
            isPaused = false;
            gameTime = 0f;
            // 추가 초기화 로직

            LoadScene("Prologue");
        }

        public void GameClear()
        {
            WhaleShark.Core.EventBus.PublishGameCleared();
        }

        /// <summary>
        /// 지정한 씬을 로드 (기본: 페이드)
        /// </summary>
        public void LoadScene(string sceneName, bool useFade = true)
        {
            SceneTransitionManager.Instance.LoadScene(sceneName, useFade);
        }

        /// <summary>
        /// 현재 활성 씬 재로드
        /// </summary>
        public void ReloadScene(bool useFade = true)
        {
            SceneTransitionManager.Instance.ReloadCurrent(useFade);
        }

        /// <summary>
        /// Additive 로 씬 추가 로드
        /// </summary>
        public void LoadSceneAdditive(string sceneName, bool useFade = true)
        {
            SceneTransitionManager.Instance.LoadSceneAdditive(sceneName, useFade);
        }

        /// <summary>
        /// 전환 중 여부 반환
        /// </summary>
        public bool IsSceneTransitioning()
        {
            return SceneTransitionManager.Instance.IsTransitioning();
        }
    }
}
