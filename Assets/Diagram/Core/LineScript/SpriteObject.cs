using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

namespace Diagram
{
    public class SpriteObject : LineBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        public SpriteRenderer MySpriteRenderer
        {
            get
            {
                if (spriteRenderer == null) spriteRenderer = gameObject.GetOrAddComponent<SpriteRenderer>();
                return spriteRenderer;
            }
        }
        public Sprite CurrentSprite
        {
            get => MySpriteRenderer.sprite;
            set => MySpriteRenderer.sprite = value;
        }

        #region Resource

        public void LoadOnResource(string path)
        {
            CurrentSprite = Resources.Load<Sprite>(path);
        }

        public void LoadOnUrl(string url)
        {
            StartCoroutine(LoadSprite(url));
        }

        public IEnumerator LoadSprite(string path)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(path);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var tex = DownloadHandlerTexture.GetContent(request);
                CurrentSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
            }
            else Debug.LogError(request.result);
        }

        #endregion
    }
}
