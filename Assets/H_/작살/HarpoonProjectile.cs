using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class HarpoonProjectile : MonoBehaviour
{
    Rigidbody _rb;
    HarpoonGun _owner;
    Transform _collector;
    float _maxDist;
    Vector3 _startPos;
    bool _finished;
    LayerMask _hitMask;

    public float lifeTime = 5f;   // 안전상 한 번 더 끊기
    float _life;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void Init(HarpoonGun owner, float speed, float maxDistance, LayerMask hitMask, Transform collector)
    {
        _owner = owner;
        _collector = collector ? collector : owner.transform;
        _maxDist = maxDistance;
        _hitMask = hitMask;
        _startPos = transform.position;
        _life = lifeTime;

        _rb.velocity = transform.forward * speed;
    }

    void Update()
    {
        if (_finished) return;

        _life -= Time.deltaTime;
        if (_life <= 0f || Vector3.Distance(_startPos, transform.position) > _maxDist)
            Finish();
    }

    void OnCollisionEnter(Collision c)
    {
        if (_finished) return;

        // 마스크 체크
        if ((_hitMask.value & (1 << c.collider.gameObject.layer)) == 0)
        {
            Finish();
            return;
        }

        // 맞은 게 채집물?
        var g = c.collider.GetComponentInParent<Gatherable>();
        if (g)
        {
            g.TryCollect(_collector);   // ✅ 히트 시 채집
            Finish();
            return;
        }

        // 채집물이 아니면 벽에 '꽂혔다'고 가정하고 잠깐 멈췄다가 회수
        _rb.isKinematic = true;
        _rb.velocity = Vector3.zero;
        Invoke(nameof(Finish), 0.3f);
    }

    void Finish()
    {
        if (_finished) return;
        _finished = true;
        _owner?.OnProjectileDone();
        Destroy(gameObject);
    }
}
