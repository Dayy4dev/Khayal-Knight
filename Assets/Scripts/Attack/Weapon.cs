using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Pool;

public abstract class Weapon : MonoBehaviour
{
    [Header("Animation Rigging")]
    public Rig weaponRig;

    [Header("Shoot VFX")]
    [SerializeField] protected ShootVFXInstance shootVFXPrefab;
    [SerializeField] protected Transform shootVFXSpawnPoint;
    [SerializeField] protected Vector3 shootVFXLocalPosition = Vector3.zero;
    [SerializeField] protected Vector3 shootVFXLocalRotation = Vector3.zero;
    [SerializeField] protected Vector3 shootVFXLocalScale = Vector3.one;
    [SerializeField] protected bool shootVFXFollowWeapon = true;
    [SerializeField] protected bool shootVFXFlipWithParent = true;
    [SerializeField] protected bool shootVFXFollowRotation = true;
    [SerializeField] protected string shootVFXSortingLayer = "Default";
    [SerializeField] protected int shootVFXSortingOrder = 0;
    [SerializeField] protected float shootVFXAutoDestroyTime = 0.5f;
    [SerializeField] protected bool shootVFXFaceCamera = false;

    protected IObjectPool<ShootVFXInstance> shootVFXPool;

    public abstract void Attack();

    public virtual bool CanAttack() { return true; }

    public virtual void OnWeaponActivate() { }
    public virtual void OnWeaponDeactivate() { }

    public virtual float GetCooldownPercentage()
    {
        return 0f;
    }

    protected void SpawnShootVFX()
    {
        if (shootVFXPrefab == null || shootVFXSpawnPoint == null) return;

        if (shootVFXPool == null)
        {
            shootVFXPool = new ObjectPool<ShootVFXInstance>(
                CreateShootVFX,
                OnTakeShootVFXFromPool,
                OnReturnShootVFXToPool,
                OnDestroyShootVFX,
                true,
                10,
                25
            );
        }

        ShootVFXInstance instance = shootVFXPool.Get();
        if (instance == null) return;

        instance.Play(
            shootVFXSpawnPoint,
            shootVFXLocalPosition,
            shootVFXLocalRotation,
            shootVFXLocalScale,
            shootVFXSortingLayer,
            shootVFXSortingOrder,
            shootVFXFlipWithParent,
            shootVFXFollowWeapon,
            shootVFXAutoDestroyTime,
            shootVFXFaceCamera,
            shootVFXFollowRotation ? shootVFXSpawnPoint : null
        );
    }

    private ShootVFXInstance CreateShootVFX()
    {
        ShootVFXInstance instance = Instantiate(shootVFXPrefab);
        instance.SetPool(shootVFXPool);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private void OnTakeShootVFXFromPool(ShootVFXInstance instance)
    {
        if (instance != null)
            instance.gameObject.SetActive(true);
    }

    private void OnReturnShootVFXToPool(ShootVFXInstance instance)
    {
        if (instance != null)
            instance.gameObject.SetActive(false);
    }

    private void OnDestroyShootVFX(ShootVFXInstance instance)
    {
        if (instance != null)
            Destroy(instance.gameObject);
    }
}
