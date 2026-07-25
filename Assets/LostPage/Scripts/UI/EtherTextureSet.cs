using System;
using UnityEngine;

namespace LostPage
{
    public sealed class EtherTextureSet : MonoBehaviour
    {
        [SerializeField] private Sprite red;
        [SerializeField] private Sprite blue;
        [SerializeField] private Sprite yellow;
        [SerializeField] private Sprite purple;

        private static EtherTextureSet _instance;

        private void Awake()
        {
            _instance = this;
        }

        public static Sprite GetSprite(EtherType type)
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<EtherTextureSet>();
            }

            if (_instance != null)
            {
                switch (type)
                {
                    case EtherType.Red:
                        return _instance.red;
                    case EtherType.Blue:
                        return _instance.blue;
                    case EtherType.Yellow:
                        return _instance.yellow;
                    case EtherType.Purple:
                        return _instance.purple;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }

            switch (type)
            {
                case EtherType.Red:
                    return FindSceneSprite("EtherTexture_Red");
                case EtherType.Blue:
                    return FindSceneSprite("EtherTexture_Blue");
                case EtherType.Yellow:
                    return FindSceneSprite("EtherTexture_Yellow");
                case EtherType.Purple:
                    return FindSceneSprite("EtherTexture_Purple");
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static Sprite FindSceneSprite(string objectName)
        {
            var spriteObject = GameObject.Find(objectName);
            var spriteRenderer = spriteObject == null
                ? null
                : spriteObject.GetComponent<SpriteRenderer>();
            return spriteRenderer == null ? null : spriteRenderer.sprite;
        }

#if UNITY_EDITOR
        public void Configure(
            Sprite redSprite,
            Sprite blueSprite,
            Sprite yellowSprite,
            Sprite purpleSprite)
        {
            red = redSprite;
            blue = blueSprite;
            yellow = yellowSprite;
            purple = purpleSprite;
        }
#endif
    }
}
