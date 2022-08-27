using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMgr : SingletonMono<PlayerMgr>
{
    public int hp;
    public PlayerState playerState;
    public float jumpPower;
    public float ropeReachSpeed;
    public float ropeReachLimit;
    public float goToBulbSpeed;
    public float detectStartDegree;
    public float detectEndDegree;
    public Transform bulbTr;
    public List<Transform> bulbTrList;
    public int bulbWaitDistance;

    private Rigidbody2D rigid;
    private Coroutine detectBulbRoutine;
    private Coroutine moveToBulbRoutine;
    private float originGravityScale;
    private float currRopeReach;

    public enum PlayerState
    {
        None,
        Idle,
        Jump,
        Rope,
        RopeJump,
        Dead
    }

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        originGravityScale = rigid.gravityScale;
        rigid.gravityScale = 0;
        this.gameObject.SetActive(false);
    }

    void Update()
    {
        switch (playerState)
        {
            case PlayerState.None:
                break;
            case PlayerState.Idle:
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Jump();
                }
                break;
            case PlayerState.Jump:
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    playerState = PlayerState.Rope;
                }
                break;
            case PlayerState.Rope:
                if (Input.GetKey(KeyCode.Space))
                {
                    DetectBulb();
                }
                if (Input.GetKeyUp(KeyCode.Space))
                {
                    CancelDetectBulb();
                    playerState = PlayerState.None;
                }
                break;
            case PlayerState.RopeJump:
                if (Input.GetKey(KeyCode.Space))
                {
                    MoveToBulb();
                }
                if (Input.GetKeyUp(KeyCode.Space))
                {
                    CancelMoveToBulb();
                }
                break;
            case PlayerState.Dead:
                break;
        }
    }

    public void AddDamage(int damage)
    {
        hp = hp - damage;
    }

    public void InitPlayer()
    {
        rigid.gravityScale = originGravityScale;
        this.gameObject.SetActive(true);
    }

    private void Jump()
    {
        rigid.velocity = Vector2.zero;
        rigid.AddForce(Vector3.up * jumpPower, ForceMode2D.Impulse);
        playerState = PlayerState.Jump;
    }

    private void DetectBulb()
    {
        // TODO: 아래 여섯줄 테스트용
        var startv2 = DegreeToVector2(detectStartDegree);
        var endv2 = DegreeToVector2(detectEndDegree);
        var startv3 = new Vector3(startv2.x, startv2.y, 0);
        var endv3 = new Vector3(endv2.x, endv2.y, 0);
        Debug.DrawRay(transform.position, startv3.normalized * ropeReachLimit, Color.blue);
        Debug.DrawRay(transform.position, endv3.normalized * ropeReachLimit, Color.blue);

        if (detectBulbRoutine != null) return;

        detectBulbRoutine = StartCoroutine(DetectBulbRoutine());
    }

    private void CancelDetectBulb()
    {
        if (detectBulbRoutine != null)
        {
            StopCoroutine(detectBulbRoutine);
            detectBulbRoutine = null;
        }

        bulbTr = null;
        currRopeReach = 0;
    }

    private void MoveToBulb()
    {
        if (moveToBulbRoutine != null) return;

        moveToBulbRoutine = StartCoroutine(MoveToBulbRoutine());
    }

    private void CancelMoveToBulb()
    {
        if (moveToBulbRoutine != null)
        {
            StopCoroutine(moveToBulbRoutine);
            moveToBulbRoutine = null;
        }

        bulbTr = null;
        rigid.gravityScale = originGravityScale;
        playerState = PlayerState.None;
    }

    private Transform GetTargetBulbTr(float reach)
    {
        foreach (var tr in bulbTrList)
        {
            if (IsTargetted(tr, reach))
            {
                currRopeReach = 0;
                return tr;
            }
        }
        return null;
    }

    private bool IsTargetted(Transform targetTr, float reach)
    {
        Vector2 targetPos = new Vector2(targetTr.position.x, targetTr.position.y);
        Vector2 playerPos = new Vector2(transform.position.x, transform.position.y);

        Vector2 v2 = targetPos - playerPos;
        float degree = Mathf.Atan2(v2.y, v2.x) * Mathf.Rad2Deg;

        // 아래 두줄은 테스트용 코드
        var tempDist = new Vector3(v2.x, v2.y, 0);
        Debug.DrawRay(playerPos, tempDist.normalized * reach, Color.red);

        if (degree < detectStartDegree || detectEndDegree < degree)
        {
            return false;
        }

        var dist = Vector2.Distance(targetPos, playerPos);

        return reach >= dist;
    }

    private IEnumerator DetectBulbRoutine()
    {
        while (bulbTr == null)
        {
            currRopeReach += Time.deltaTime * ropeReachSpeed;
            if (currRopeReach >= ropeReachLimit)
            {
                CancelDetectBulb();
                playerState = PlayerState.None;
                yield break;
            }
            bulbTr = GetTargetBulbTr(currRopeReach);
            yield return null;
        }

        currRopeReach = 0;
        playerState = PlayerState.RopeJump;
        detectBulbRoutine = null;
    }

    private IEnumerator MoveToBulbRoutine()
    {
        rigid.gravityScale = 0;
        rigid.velocity = Vector2.zero;
        Vector3 targetPos = new Vector3(transform.position.x, bulbTr.position.y, 0);
        float dist = Vector2.Distance(new Vector2(bulbTr.position.x, 0)
                                    , new Vector2(transform.position.x, 0));

        while (targetPos.y * 0.925f > transform.position.y || dist > bulbWaitDistance)
        {
            dist = Vector2.Distance(new Vector2(bulbTr.position.x, 0)
                                , new Vector2(transform.position.x, 0));
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * goToBulbSpeed / dist);
            yield return null;
        }

        rigid.gravityScale = originGravityScale;
        rigid.velocity = Vector2.zero;
        rigid.AddForce(Vector3.up * jumpPower / 5, ForceMode2D.Impulse);
        bulbTr = null;
        playerState = PlayerState.Idle;
        moveToBulbRoutine = null;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag.Equals("Ground"))
        {
            CancelDetectBulb();
            playerState = PlayerState.Idle;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag.Equals("DamageObj"))
        {
            var spawnObj = other.GetComponent<SpawnObject>();
            AddDamage(spawnObj.damage);
            spawnObj.DestroyObject();
        }
    }

    public static Vector2 RadianToVector2(float radian)
    {
        return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
    }

    public static Vector2 DegreeToVector2(float degree)
    {
        return RadianToVector2(degree * Mathf.Deg2Rad);
    }


}
