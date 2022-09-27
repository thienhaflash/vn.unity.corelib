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
            KImageLoader.Load(currentURL, (sprt) =>
            {
                if (url != currentURL) return; // change to a different URL
                target.sprite = defaultImage;
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