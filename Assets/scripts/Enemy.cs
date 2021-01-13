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
               StartCoroutine(CorrectDrift());
            }
         }
      }
   }

   private IEnumerator CorrectDrift()
   {
      float i = 0.0f;
      while (i < lagTime)
      {
         i += Time.deltaTime;
      }

      float drift = Vector3.Distance(_enemy.position, playerPositionDriftCheckMessage.currentPos);
      if (drift >= 0.5f)
      {
         Debug.Log("Drift detected ******************************");
         // TODO: investigate how the time parameter effects the lerp - moving it to a full second doesn't seem to change much
         StartCoroutine(MoveObject(_enemy.transform, _enemy.position, playerPositionDriftCheckMessage.currentPos, .2f));
      }
      lagTime = -1;
      yield return null;
   }

   //TODO: look into cleaning this up
   private IEnumerator MoveObject(Transform thisTransform, Vector3 startPos, Vector3 endPos, float time)
   {
      float i = 0.0f;
      float rate = 1.0f / time;
      while (i < 1.0)
      {
         i += Time.deltaTime * rate;
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
