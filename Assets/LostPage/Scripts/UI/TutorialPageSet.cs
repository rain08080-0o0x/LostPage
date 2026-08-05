using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LostPage
{
    public sealed class TutorialPageSet : MonoBehaviour
    {
        private static readonly string[] ScenePageNames =
        {
            "TutorialPage_01",
            "TutorialPage_02",
            "TutorialPage_03",
            "TutorialPage_04",
            "TutorialPage_05"
        };
        private const string ArrowObjectName = "TutorialArrowTexture";
        private const string UsedEtherObjectName = "TutorialUsedEtherTexture";

        [SerializeField] private Sprite[] pages;

        private static TutorialPageSet _instance;

        private void Awake()
        {
            _instance = this;
        }

        public static int PageCount
        {
            get
            {
                EnsureInstance();
                return _instance == null || _instance.pages == null
                    ? ScenePageNames.Length
                    : _instance.pages.Length;
            }
        }

        public static Sprite GetPage(int index)
        {
            EnsureInstance();
            if (_instance != null && _instance.pages != null)
            {
                return index < 0 || index >= _instance.pages.Length
                    ? null
                    : _instance.pages[index];
            }

            if (index < 0 || index >= ScenePageNames.Length)
            {
                return null;
            }

            var pageObject = GameObject.Find(ScenePageNames[index]);
            var spriteRenderer = pageObject == null
                ? null
                : pageObject.GetComponent<SpriteRenderer>();
            return spriteRenderer == null ? null : spriteRenderer.sprite;
        }

        public static Sprite GetArrow()
        {
            return GetSceneSprite(ArrowObjectName);
        }

        public static Sprite GetUsedEther()
        {
            return GetSceneSprite(UsedEtherObjectName);
        }

        private static Sprite GetSceneSprite(string objectName)
        {
            var spriteObject = GameObject.Find(objectName);
            var spriteRenderer = spriteObject == null
                ? null
                : spriteObject.GetComponent<SpriteRenderer>();
            return spriteRenderer == null ? null : spriteRenderer.sprite;
        }

        private static void EnsureInstance()
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<TutorialPageSet>();
            }
        }

#if UNITY_EDITOR
        public void Configure(Sprite[] pageSprites)
        {
            pages = pageSprites;
        }
#endif
    }

    public sealed class TutorialSwipeHandler :
        MonoBehaviour,
        IBeginDragHandler,
        IEndDragHandler
    {
        private Action<int> _onPageDelta;
        private float _startX;

        public void Configure(Action<int> onPageDelta)
        {
            _onPageDelta = onPageDelta;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _startX = eventData.position.x;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            var delta = eventData.position.x - _startX;
            if (Mathf.Abs(delta) < 80f)
            {
                return;
            }

            _onPageDelta?.Invoke(delta < 0f ? 1 : -1);
        }
    }
}
