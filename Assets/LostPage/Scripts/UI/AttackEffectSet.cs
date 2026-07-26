using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostPage
{
    public sealed class AttackEffectSet : MonoBehaviour
    {
        [SerializeField] private Texture2D spriteSheet;
        [SerializeField] private int frameCount = 5;

        private static AttackEffectSet _instance;
        private static IReadOnlyList<Sprite> _frames;

        private void Awake()
        {
            _instance = this;
            _frames = null;
        }

        public static IReadOnlyList<Sprite> GetFrames()
        {
            EnsureInstance();
            if (_frames != null)
            {
                return _frames;
            }

            if (_instance == null ||
                _instance.spriteSheet == null ||
                _instance.frameCount <= 0 ||
                _instance.spriteSheet.width % _instance.frameCount != 0)
            {
                return Array.Empty<Sprite>();
            }

            var frameWidth =
                _instance.spriteSheet.width / _instance.frameCount;
            var frames = new List<Sprite>(_instance.frameCount);
            for (var index = 0; index < _instance.frameCount; index++)
            {
                frames.Add(
                    Sprite.Create(
                        _instance.spriteSheet,
                        new Rect(
                            index * frameWidth,
                            0,
                            frameWidth,
                            _instance.spriteSheet.height),
                        new Vector2(0.5f, 0.5f),
                        100f));
            }

            _frames = frames;
            return _frames;
        }

        private static void EnsureInstance()
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<AttackEffectSet>();
            }
        }

#if UNITY_EDITOR
        public void Configure(Texture2D texture, int count)
        {
            spriteSheet = texture;
            frameCount = count;
            _frames = null;
        }
#endif
    }
}
