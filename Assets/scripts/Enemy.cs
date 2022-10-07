using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Enemy : MonoBehaviour
{
    private SortedList<int, PlayerPositionMessage> enemyPositionMessageQueue;
    private PlayerPositionMessage playerPositionDriftCheckMessage;
    private Rigidbody _enemy;
    private const float DriftThreshold = 0.02f;
    private float maxSpeed = 10;

    public int enemyPositionSequence = 0;

    PlayerPositionMessage previousEnemyPositionMessage = null;

    void FixedUpdate()
    {
        if (WebSocketService.Instance.matchInitialized && enemyPositionMessageQueue != null)
        {
            // this FixedUpdate loop continuously applies whatever movement vectors are in the queue.
            // The list stores positions by sequence number, not index.
            PlayerPositionMessage enemyPositionToRender;
            Vector3 movementPlane = new Vector3(_enemy.velocity.x, 0, _enemy.velocity.z);

            Debug.Log(enemyPositionMessageQueue.IndexOfKey(50));

            if (enemyPositionMessageQueue.Keys.Count > 0)
            {
                enemyPositionSequence = enemyPositionMessageQueue.Keys[enemyPositionMessageQueue.Keys.Count - 1];
            }

            // Check if we have the next sequence to render
            if (enemyPositionMessageQueue.TryGetValue(enemyPositionSequence, out enemyPositionToRender))
            {

                if (previousEnemyPositionMessage != null)
                {
                    float drift = Vector3.Distance(_enemy.position, previousEnemyPositionMessage.currentPos);
                    if (drift >= DriftThreshold)
                    {
                        Debug.Log("Drift detected ******************************");
                        StartCoroutine(CorrectDrift(_enemy.transform, _enemy.position, previousEnemyPositionMessage.currentPos, .2f));
                    }
                    enemyPositionMessageQueue.Remove(enemyPositionToRender.seq - 1);
                }

                previousEnemyPositionMessage = (PlayerPositionMessage)enemyPositionToRender.Clone();

                _enemy.AddForce(enemyPositionToRender.velocity, ForceMode.VelocityChange);


            }
        }
    }

    void Update()
    {
        // Capping the speed/magnitude across network is critical to maintain smooth movement
        if (_enemy.velocity.magnitude > maxSpeed)
        {
            _enemy.velocity = Vector3.ClampMagnitude(_enemy.velocity, maxSpeed);
        }
    }

    private IEnumerator CorrectDrift(Transform thisTransform, Vector3 startPos, Vector3 endPos, float correctionDuration)
    {
        float i = 0.0f;
        while (i < correctionDuration)
        {
            i += Time.deltaTime;
            thisTransform.position = Vector3.Lerp(startPos, endPos, i);
            //Debug.Log(startPos);
            //Debug.Log(endPos);
            Debug.Log(Vector3.Lerp(startPos, endPos, i));

        }
        yield return null;
    }

    public void BufferState(PlayerPositionMessage state)
    {
        // only add enemy position messages, for now
        if (state.opcode == WebSocketService.OpponentVelocity)
        {
            enemyPositionMessageQueue.Add(state.seq, state);
        }
    }

    public void Reset(Vector3 enemyPosMessage)
    {
        _enemy.transform.position = enemyPosMessage;
        enemyPositionSequence = 0;
        enemyPositionMessageQueue = new SortedList<int, PlayerPositionMessage>();
    }

    public void SetActive(bool activeFlag)
    {
        gameObject.SetActive(activeFlag);
    }

    void Awake()
    {
        Debug.Log("Enemy Awake");
        _enemy = gameObject.GetComponent<Rigidbody>();
        SetActive(false);
    }

    void Start()
    {
        Debug.Log("Enemy start");
    }
}
