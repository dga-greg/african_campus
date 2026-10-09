using CampusLife.Runtime.World;
using UnityEngine;

namespace CampusLife.Runtime.Npc
{
    /// <summary>Short floating line of text above a character ("Hey, careful!").</summary>
    public sealed class SpeechBubble : MonoBehaviour
    {
        TextMesh _text;
        float _hideAt;

        public static SpeechBubble Create(Transform character)
        {
            var go = new GameObject("SpeechBubble");
            go.transform.SetParent(character, false);
            go.transform.localPosition = new Vector3(0f, 2.25f, 0f);
            var bubble = go.AddComponent<SpeechBubble>();
            bubble._text = WorldKit.Text(go.transform, "", Vector3.zero, 0.16f, Color.white);
            bubble._text.gameObject.SetActive(false);
            return bubble;
        }

        public void Say(string line, float seconds = 2.5f)
        {
            _text.text = line;
            _text.gameObject.SetActive(true);
            _hideAt = Time.time + seconds;
        }

        void LateUpdate()
        {
            if (!_text.gameObject.activeSelf) return;
            if (Time.time > _hideAt)
            {
                _text.gameObject.SetActive(false);
                return;
            }
            var cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
