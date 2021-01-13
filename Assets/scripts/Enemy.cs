using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class Enemy : MonoBehaviour
{
   private SortedList<int, PlayerPositionMessage> enemyPositionMessageQueue;
   private PlayerPositionMessage playerPositionDriftCheckMessage;
   private Rigidbody _enemy;
   private long lagTime = -1;
   private const float DriftThreshold = 0.5f;

   public int enemyPositionSequence = 0;

   void FixedUpdate()
   {
      if (WebSocketService.Instance.matchInitialized && enemyPositionMessageQueue != null)
      {
         // this FixedUpdate loop continuously applies whatever movement vectors are in the queue.
         // The list stores positions by sequence number, not index.
         PlayerPositionMessage enemyPositionToRender;
         Vector3 movementPlane = new Vector3(_enemy.velocity.x, 0, _enemy.velocity.z);

         // Check if we have the next sequence to render & 
         // Capping the speed/magnitude across network is critical to maintain smooth movement
         if (enemyPositionMessageQueue.TryGetValue(enemyPositionSequence, out enemyPositionToRender) && movementPlane.magnitude <= 10)
         {
            _enemy.AddForce(enemyPositionToRender.velocity, ForceMode.VelocityChange);

            // Debug.Log("Rendered queue sequence number: " + enemyPositionSequence);
            enemyPositionSequence++;
            enemyPositionMessageQueue.Remove(enemyPositionToRender.seq);

            if (lagTime < 0)
            {
               // check this position after lag time below
               playerPositionDriftCheckMessage = enemyPositionToRender;
               lagTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - (long)enemyPositionToRender.timestamp;
               StartCoroutine(CheckForDrift());
            }
         }
      }
   }

   private IEnumerator CheckForDrift()
   {
      float i = 0.0f;

      // wait for the lag time to pass
      while (i < lagTime)
      {
         i += Time.deltaTime;
      }

      // if our drift threshold is exceeded, perform correction
      float drift = Vector3.Distance(_enemy.position, playerPositionDriftCheckMessage.currentPos);
      if (drift >= DriftThreshold)
      {
         // Debug.Log("Drift detected ******************************");
         StartCoroutine(CorrectDrift(_enemy.transform, _enemy.position, playerPositionDriftCheckMessage.currentPos, .2f));
      }

      // reset 
      lagTime = -1;
      yield return null;
   }

   private IEnumerator CorrectDrift(Transform thisTransform, Vector3 startPos, Vector3 endPos, float correctionDuration)
   {
      float i = 0.0f;
      while (i < correctionDuration)
      {
         i += Time.deltaTime;
         thisTransform.position = Vector3.Lerp(startPos, endPos, i);

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
