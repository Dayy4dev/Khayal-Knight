using UnityEngine;

// Taruh script ini di prefab peluru (projectilePrefab pada EnemyAI)
public class ProjectilePlane : MonoBehaviour
{
    public float speed = 20f;
    public int damage = 10;
    public float lifeTime = 5f;

    private Transform target;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    void Update()
    {
        if (target == null)
        {
            // Tidak ada target: terbang lurus saja
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
            return;
        }

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(dir);

        if (Vector3.Distance(transform.position, target.position) < 1f)
        {
            var airplane = target.GetComponent<IAirplaneDamageable>();
            if (airplane != null) airplane.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}

// Interface supaya script airplane bebas bentuknya, asal implement TakeDamage
public interface IAirplaneDamageable
{
    void TakeDamage(int amount);
}
