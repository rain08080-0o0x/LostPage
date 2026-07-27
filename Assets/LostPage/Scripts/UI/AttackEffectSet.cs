using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostPage
{
    public sealed class AttackEffectSet : MonoBehaviour
    {
        private const string SceneTextureObjectName = "AttackEffectTexture";
        private const int DefaultFrameCount = 5;

        [SerializeField] private Texture2D spriteSheet;
        [SerializeField] private int frameCount = DefaultFrameCount;

        private static AttackEffectSet _instance;
        private static IReadOnlyList<Sprite> _frames;

        private void Awake()
        {
            _instance = this;
            _frames = null;
        }

        public static IReadOnlyList<Sprite> GetFrames()
        {
            if (_frames != null)
            {
                return _frames;
            }

            ResolveTexture(out var texture, out var count);
            if (texture == null ||
                count <= 0 ||
                texture.width % count != 0)
            {
                return Array.Empty<Sprite>();
            }

            var frameWidth = texture.width / count;
            var frames = new List<Sprite>(count);
            for (var index = 0; index < count; index++)
            {
                frames.Add(
                    Sprite.Create(
                        texture,
                        new Rect(
                            index * frameWidth,
                            0,
                            frameWidth,
                            texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f));
            }

            _frames = frames;
            return _frames;
        }

        private static void ResolveTexture(
            out Texture2D texture,
            out int count)
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<AttackEffectSet>();
            }

            if (_instance != null && _instance.spriteSheet != null)
            {
                texture = _instance.spriteSheet;
                count = _instance.frameCount;
                return;
            }

            var textureObject = GameObject.Find(SceneTextureObjectName);
            var spriteRenderer = textureObject == null
                ? null
                : textureObject.GetComponent<SpriteRenderer>();
            texture = spriteRenderer == null ||
                      spriteRenderer.sprite == null
                ? null
                : spriteRenderer.sprite.texture;
            count = DefaultFrameCount;
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
