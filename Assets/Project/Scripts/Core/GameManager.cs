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
        

        /// <summary>
        /// 이벤트 구독 등록
        /// 게임 시작 시 필요한 이벤트들을 구독합니다
        /// </summary>
        void Start()
        {
            EventBus.PauseToggled += OnPauseToggled;
            EventBus.PlayerDied += OnPlayerDied;
        }

        void OnDestroy()
        {
            EventBus.PauseToggled -= OnPauseToggled;
            EventBus.PlayerDied -= OnPlayerDied;
        }

        /// <summary>
        /// 매 프레임 업데이트
        /// 게임 시간 카운트 및 입력 처리
        /// </summary>
        void Update()
        {
            if (!isPaused)
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
        void OnPlayerDied()
        {
            Debug.Log("Player died!");
        }

        private void TestDied()
        {
            EventBus.PublishPlayerDied();
        }
    }
}