using UnityEngine;

public class GatherInteractor : MonoBehaviour
{
    public Camera cam;
    public float interactDistance = 3.0f;
    public LayerMask interactMask = ~0;
    public bool hitTriggers = true;
    public Transform selfRoot;
    public bool ignoreSelf = true;
    public bool offsetFromCamera = true;

    OutlineTarget _currentOutline;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!selfRoot) selfRoot = transform.root;
    }

    void Update()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (offsetFromCamera) ray.origin += ray.direction * 0.1f;

        // 3A) 하이라이트 갱신(매 프레임)
        UpdateAimHighlight(ray);

        // 3B) 수집 입력
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            var hits = Physics.RaycastAll(ray, interactDistance, interactMask,
                hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore);
            if (hits.Length == 0) return;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (ignoreSelf && h.collider.transform.root == selfRoot) continue;
                var g = h.collider.GetComponentInParent<Gatherable>();
                if (g) { g.TryCollect(transform); break; }
            }
            if (InputGate.PortalInputCaptured) return; // 포탈 존에서는 채집 입력 무시
        }
    }

    void UpdateAimHighlight(Ray ray)
    {
        OutlineTarget next = null;

        var hits = Physics.RaycastAll(ray, interactDistance, interactMask,
            hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore);
        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (ignoreSelf && h.collider.transform.root == selfRoot) continue;

                var g = h.collider.GetComponentInParent<Gatherable>();
                if (!g) continue;

                next = g.GetComponent<OutlineTarget>();
                if (!next) next = g.gameObject.AddComponent<OutlineTarget>(); // 없으면 자동 추가
                break;
            }
        }

        if (_currentOutline == next) return;
        if (_currentOutline) _currentOutline.SetHighlighted(false);
        _currentOutline = next;
        if (_currentOutline) _currentOutline.SetHighlighted(true);
    }
}
