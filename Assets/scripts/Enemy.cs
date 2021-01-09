using UnityEngine;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
   private SortedList<int, PlayerPositionMessage> enemyPositionMessageQueue;

   private Rigidbody _enemy;
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
         }
      }
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
