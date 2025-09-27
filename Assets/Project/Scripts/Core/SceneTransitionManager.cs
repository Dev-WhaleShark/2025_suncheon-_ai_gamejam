using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace WhaleShark.Core
{
    [DisallowMultipleComponent]
    public class SceneTransitionManager : Singleton<SceneTransitionManager>
    {
        [Header("Fade Settings")]
        [SerializeField] private float fadeOutTime = 0.5f;
        [SerializeField] private float fadeInTime = 0.5f;
        [SerializeField] private Color fadeColor = Color.black;

        private Canvas overlayCanvas;
        private CanvasGroup canvasGroup;
        private bool isTransitioning;

        private Tween fadeTween;
        private bool nextFadeInOnSceneStart;

        protected override void Awake()
        {
            base.Awake();
            EnsureOverlay();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            KillFadeTween();
        }

        public void LoadScene(string sceneName, bool fadeInOnSceneStart = false)
        {
            if (isTransitioning)
            {
                return;
            }
            nextFadeInOnSceneStart = fadeInOnSceneStart;
            BeginSceneChange(() => SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single));
        }

        public void LoadScene(int buildIndex, bool useFade = true, bool fadeInOnSceneStart = false)
        {
            if (isTransitioning)
            {
                return;
            }
            nextFadeInOnSceneStart = fadeInOnSceneStart;
            BeginSceneChange(() => SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single));
        }

        public void ReloadCurrent(bool fadeInOnSceneStart = false)
        {
            var active = SceneManager.GetActiveScene();
            LoadScene(active.name, fadeInOnSceneStart);
        }

        public void LoadSceneAdditive(string sceneName, bool fadeInOnSceneStart = false)
        {
            if (isTransitioning)
            {
                return;
            }
            nextFadeInOnSceneStart = fadeInOnSceneStart;
            BeginSceneChange(() => SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive));
        }

        public bool IsTransitioning()
        {
            return isTransitioning;
        }

        /// <summary>
        /// 현재 즉시 화면을 검은색으로 채움(초기 부팅시 바로 FadeIn 할 때 사용)
        /// </summary>
        public void ForceBlack()
        {
            if (canvasGroup == null)
            {
                return;
            }
            KillFadeTween();
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// 외부에서 수동 페이드 인 (Black -> Clear)
        /// </summary>
        public void FadeInFromBlack(float? duration = null)
        {
            if (canvasGroup == null)
            {
                return;
            }
            KillFadeTween();
            float d = duration.HasValue ? Mathf.Max(0f, duration.Value) : fadeInTime;
            if (d <= 0f)
            {
                canvasGroup.alpha = 0f;
                return;
            }
            fadeTween = canvasGroup
                .DOFade(0f, d)
                .SetUpdate(true)
                .OnComplete(() => fadeTween = null);
        }

        /// <summary>
        /// 외부에서 수동 페이드 아웃 (Clear -> Black)
        /// </summary>
        public void FadeOutToBlack(float? duration = null, UnityAction onComplete = null)
        {
            if (canvasGroup == null)
            {
                return;
            }
            KillFadeTween();
            float d = duration.HasValue ? Mathf.Max(0f, duration.Value) : fadeOutTime;
            if (d <= 0f)
            {
                canvasGroup.alpha = 1f;
                onComplete?.Invoke();
                return;
            }
            fadeTween = canvasGroup
                .DOFade(1f, d)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    fadeTween = null;
                    onComplete?.Invoke();
                });
        }

        private void BeginSceneChange(Func<AsyncOperation> loadOpFactory)
        {
            isTransitioning = true;

            FadeOutToBlack(fadeOutTime, () => StartAsyncLoad(loadOpFactory));
        }

        private void StartAsyncLoad(Func<AsyncOperation> loadOpFactory)
        {
            AsyncOperation op = null;
            try
            {
                op = loadOpFactory();
            }
            catch (Exception e)
            {
                Debug.LogError("[SceneTransitionManager] 씬 로드 중 예외: " + e.Message);
                isTransitioning = false;
                nextFadeInOnSceneStart = false;
                return;
            }

            if (op == null)
            {
                Debug.LogError("[SceneTransitionManager] AsyncOperation null");
                isTransitioning = false;
                nextFadeInOnSceneStart = false;
                return;
            }

            op.completed += _ =>
            {

                if ( nextFadeInOnSceneStart )
                {
                    if ( canvasGroup != null)
                    {
                        canvasGroup.alpha = 1f;
                    }

                    DOVirtual.DelayedCall(Time.unscaledDeltaTime, StartFadeIn).SetUpdate(true);
                    nextFadeInOnSceneStart = false;
                    return;
                }

                if ( canvasGroup != null && canvasGroup.alpha < 1f)
                {
                    canvasGroup.alpha = 1f;
                }

                StartFadeIn();
            };
        }

        private void StartFadeIn()
        {
            KillFadeTween();
            if (canvasGroup == null)
            {
                FinishTransition();
                return;
            }
            float d = fadeInTime;
            if (d <= 0f)
            {
                canvasGroup.alpha = 0f;
                FinishTransition();
                return;
            }
            fadeTween = canvasGroup
                .DOFade(0f, d)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    fadeTween = null;
                    FinishTransition();
                });
        }

        private void FinishTransition()
        {
            isTransitioning = false;
        }

        private void EnsureOverlay()
        {
            if (overlayCanvas != null)
            {
                return;
            }

            var canvasGo = new GameObject("SceneFadeCanvas");
            canvasGo.transform.SetParent(transform);
            overlayCanvas = canvasGo.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 10000;

            canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var imageGo = new GameObject("FadeImage");
            imageGo.transform.SetParent(canvasGo.transform, false);
            var img = imageGo.AddComponent<UnityEngine.UI.Image>();
            img.color = fadeColor;
            var rect = img.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void KillFadeTween()
        {
            if (fadeTween != null && fadeTween.IsActive())
            {
                fadeTween.Kill();
            }
            fadeTween = null;
        }

        [ContextMenu("Test / FadeOut")]
        private void TestFadeOut()
        {
            FadeOutToBlack();
        }

        [ContextMenu("Test / FadeIn")]
        private void TestFadeIn()
        {
            FadeInFromBlack();
        }
    }
}
