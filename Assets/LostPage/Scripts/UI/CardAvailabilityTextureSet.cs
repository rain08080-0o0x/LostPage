using UnityEngine;

namespace LostPage
{
    public static class CardAvailabilityTextureSet
    {
        public static Sprite GetCooldownSprite(int cooldownRemaining)
        {
            return cooldownRemaining >= 1 && cooldownRemaining <= 8
                ? FindSceneSprite($"CardAvailability_CT_{cooldownRemaining}")
                : null;
        }

        public static Sprite GetNotUseSprite()
        {
            return FindSceneSprite("CardAvailability_NotUse");
        }

        private static Sprite FindSceneSprite(string objectName)
        {
            var spriteObject = GameObject.Find(objectName);
            var spriteRenderer = spriteObject == null
                ? null
                : spriteObject.GetComponent<SpriteRenderer>();
            return spriteRenderer == null ? null : spriteRenderer.sprite;
        }
    }
}
