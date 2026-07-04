using UnityEngine;
using UnityEngine.Pool;

public class Bullet : MonoBehaviour
{
    private float speed;
    private float lifetime = 2f;
    private int damageAmount;

    private float currentLifetime;
    private IObjectPool<Bullet> originPool;
    private bool hasHit = false;

    public void Setup(float bulletSpeed, int damage)
{
    this.speed = bulletSpeed;
    this.damageAmount = damage; // Menggunakan damage asli dari Cannon
}
    public void SetPool(IObjectPool<Bullet> pool)
    {
        originPool = pool;
    }

    private void OnEnable()
    {
        currentLifetime = lifetime;
        hasHit = false;
    }

    private void Update()
    {
        transform.Translate(transform.forward * speed * Time.deltaTime, Space.World);

        currentLifetime -= Time.deltaTime;
        if (currentLifetime <= 0)
        {
            ReleaseToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damageAmount);
            Debug.Log($"Bullet hit {other.gameObject.name} and dealt {damageAmount} damage!");
        }
        else
        {
            Debug.Log($"Bullet hit {other.gameObject.name} but it is not damageable.");
        }

        hasHit = true;
        ReleaseToPool();
    }

    private void ReleaseToPool()
    {
        if (gameObject.activeSelf && originPool != null)
        {
            originPool.Release(this);
        }
    }
}