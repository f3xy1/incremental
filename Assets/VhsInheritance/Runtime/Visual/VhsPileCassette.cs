using UnityEngine;

namespace VhsInheritance
{
    // One persistent scene object per cassette, identified by the saved cassette id.
    public sealed class VhsPileCassette : MonoBehaviour
    {
        [SerializeField] private CassetteData cassette;
        [SerializeField] private int stackIndex;
        private SpriteRenderer cover;
        private Vector3 from;
        private Vector3 target;
        private float elapsed;
        private float duration;
        private bool dropping;

        public string CassetteId => cassette.id;

        public void Initialize(CassetteData data, Vector3 source, Vector3 destination, int index, bool animate)
        {
            cassette = data;
            name = "Cassette " + data.genre + " " + data.id.Substring(0, Mathf.Min(6, data.id.Length));
            cover = gameObject.AddComponent<SpriteRenderer>();
            cover.sprite = VhsSpriteFactory.Cassette(data.genre, data.isForeign);
            var collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.92f, 1.05f);
            collider.isTrigger = true;
            var shadow = new GameObject("Contact Shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(transform, false);
            shadow.transform.localPosition = new Vector3(0.08f, -0.23f, 0);
            shadow.sprite = VhsSpriteFactory.Shadow();
            transform.position = animate ? source : destination;
            dropping = animate;
            SetTarget(destination, index);
            duration = animate ? 0.6f : 0;
        }

        public void SetTarget(Vector3 destination, int index)
        {
            stackIndex = index;
            from = transform.position;
            target = destination;
            elapsed = 0;
            if (!dropping) duration = 0.24f;
            cover.sortingOrder = index * 2 + 1;
            transform.GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = index * 2;
        }

        private void Update()
        {
            if (duration <= 0) { transform.position = target; return; }
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(from, target, Mathf.SmoothStep(0, 1, t));
            if (dropping) transform.position += Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.7f);
            if (t >= 1) { dropping = false; duration = 0; }
        }
    }
}
