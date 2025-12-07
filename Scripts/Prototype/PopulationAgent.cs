using UnityEngine;
using HexGrid;

public class PopulationAgent : MonoBehaviour
{
    public float moveSpeed = .25f;
    public HexTile currentTile;
    HexTile targetTile;
    bool stayOnTile = false;

    // local wandering
    Vector3 localTarget;
    public float idleRadius = 0.65f;
    public float idleTargetThreshold = 0.001f;
    float idleTimer = 0f;
    public float idlePickInterval = 2f;

    public void Initialize(HexTile start, bool _stayOnTile = false)
    {
        currentTile = start;
        targetTile = null;
        transform.position = currentTile != null ? currentTile.transform.position : Vector3.zero;
        stayOnTile = _stayOnTile;
        if (stayOnTile)
        {
            // place with a small jitter inside the tile so agents are visible
            transform.position = GetRandomPointInTile();
            PickNewLocalTarget();
        }
        // Set sprite renderer order in layer to be above tiles
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 20;
        }
        // Set transform scale
        transform.localScale = Vector3.one * 4f;
    }

    void Update()
    {
        if (stayOnTile && targetTile == null)
        {
            // idle wandering inside current tile
            idleTimer += Time.deltaTime;
            Vector3 targetPos = localTarget;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            if (Vector3.SqrMagnitude(transform.position - targetPos) < idleTargetThreshold || idleTimer >= idlePickInterval)
            {
                PickNewLocalTarget();
            }
            return;
        }

        if (targetTile == null) return;

        Vector3 worldTargetPos = targetTile.transform.position;
        transform.position = Vector3.MoveTowards(transform.position, worldTargetPos, moveSpeed * Time.deltaTime);
        if (Vector3.SqrMagnitude(transform.position - worldTargetPos) < 0.0001f)
        {
            ArriveAtTarget();
        }
    }

    void ArriveAtTarget()
    {
        if (currentTile != null) currentTile.OnPopulationLeave(this);
        currentTile = targetTile;
        if (currentTile != null) currentTile.OnPopulationEnter(this);
        targetTile = null;
        PopulationManager.Instance.RequestNextMove(this, currentTile);
    }

    public void SetTarget(HexTile t)
    {
        if (t == null) return;
        targetTile = t;
    }

    void PickNewLocalTarget()
    {
        localTarget = GetRandomPointInTile();
        idleTimer = 0f;
    }

    Vector3 GetRandomPointInTile()
    {
        if (currentTile == null) return transform.position;
        var center = currentTile.transform.position;
        var off = Random.insideUnitCircle * idleRadius;
        return new Vector3(center.x + off.x, center.y + off.y, center.z);
    }

    // Called to begin inter-tile movement
    public void StartMovement()
    {
        if (!stayOnTile) return;
        stayOnTile = false;
        // ask manager to pick a neighbor
        PopulationManager.Instance.RequestNextMove(this, currentTile);
    }
}
