using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Util
{
    public class ScreenFade : MonoBehaviour
    {
        public static ScreenFade Instance = null;

        public RawImage _fadeImage = null;
        private const float _fadeDuration = 0.5f;
        private float _fadeTime = 0f;
        private TransitionState _fadeState = TransitionState.SHOW;

        //private Canvas _canvas = null;

        private UnityEvent _fadeOutEvent;
        private UnityEvent _fadeInEvent;
        private bool _invokeFadeOut;

        enum TransitionState
        {
            HIDDEN,
            FADEIN,
            SHOW,
            FADEOUT
        }

        void Awake()
        {
            Instance = this;

            //_canvas = GetComponent<Canvas>();
            //_canvas.enabled = false;

            _fadeOutEvent = new UnityEvent();
            _fadeInEvent = new UnityEvent();

            _invokeFadeOut = false;
        }

        private void Update()
        {
            if (_invokeFadeOut == true)
            {
                _fadeOutEvent.Invoke();

                _invokeFadeOut = false;
            }


            UpdateFade();
        }

        public void FadeOut()
        {
            _fadeState = TransitionState.FADEOUT;
            _fadeTime = 0f;

            //_canvas.enabled = true;
        }

        public void FadeIn()
        {
            if (_fadeState != TransitionState.HIDDEN)
            {
                return;
            }

            _fadeState = TransitionState.FADEIN;
            _fadeTime = 0f;

            //_canvas.enabled = true;
        }

        private void UpdateFade()
        {
            float percentage;

            switch (_fadeState)
            {
                case TransitionState.FADEOUT:
                    _fadeTime += Time.deltaTime;

                    bool fadeOutDone = false;

                    if (_fadeTime >= _fadeDuration)
                    {
                        _fadeTime = _fadeDuration;
                        _fadeState = TransitionState.HIDDEN;

                        fadeOutDone = true;
                    }

                    percentage = _fadeTime / _fadeDuration;
                    _fadeImage.color = new Color(0f, 0f, 0f, percentage);

                    if (fadeOutDone == true)
                    {
                        _invokeFadeOut = true;
                    }
                    break;
                case TransitionState.FADEIN:
                    _fadeTime += Time.deltaTime;

                    bool fadeInDone = false;

                    if (_fadeTime >= _fadeDuration)
                    {
                        _fadeTime = _fadeDuration;
                        _fadeState = TransitionState.SHOW;

                        //_canvas.enabled = false;    // fully faded in, fadeimage not shown => disable canvas

                        fadeInDone = true;
                    }

                    percentage = 1f - (_fadeTime / _fadeDuration);
                    _fadeImage.color = new Color(0f, 0f, 0f, percentage);

                    if (fadeInDone == true)
                    {
                        _fadeInEvent.Invoke();
                    }

                    break;
                case TransitionState.SHOW:
                    break;
                case TransitionState.HIDDEN:
                    break;
            }
        }

        public void SubscribeOnFadeOut(UnityAction action)
        {
            _fadeOutEvent.AddListener(action);
        }

        public void UnsubscribeOnFadeOut(UnityAction action)
        {
            _fadeOutEvent.RemoveListener(action);
        }

        public void SubscribeOnFadeIn(UnityAction action)
        {
            _fadeInEvent.AddListener(action);
        }

        public void UnsubscribeOnFadeIn(UnityAction action)
        {
            _fadeInEvent.RemoveListener(action);
        }
    }
}