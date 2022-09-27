using UnityEngine;
using UnityEngine.UI;

namespace vn.corelib
{
    public class UIImageURL : MonoBehaviour
    {
        public Image target;
        public Sprite defaultImage;

        [SerializeField] private string url;

        void Start()
        {
            if (!string.IsNullOrEmpty(url)) Refresh();
        }

        [Button(ButtonMode.EnabledInPlayMode)]
        public void Refresh()
        {
            target.sprite = defaultImage;
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            var currentURL = url;
            KImageLoader.Load(currentURL, (text) =>
            {
                if (url != currentURL) return; // change to a different URL
                if (text == null) return;
                
                var spr = Sprite.Create(text, new Rect(0.0f, 0.0f, text.width, text.height), new Vector2(0.5f, 0.5f), 100.0f);
                target.sprite = spr;
            });
        }

        public void SetURL(string value)
        {
            if (value == url)
            {
                // Debug.LogWarning($"Same URL : {url}");
                return;
            }

            url = value;
            Refresh();
        }
    }
}