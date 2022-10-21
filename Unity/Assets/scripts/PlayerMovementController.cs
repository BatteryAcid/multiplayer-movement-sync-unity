using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovementController : MonoBehaviour
{
   public Rigidbody player;
   public Rigidbody ball;
   public Transform playerCamera;
   public float baseBallThrust = 20.0f;

   private float _throwKeyPressedStartTime;
   private BallActionHandler _ballActionHandler;
   private Vector3 lastX, lastZ;
   private float inputHorX, inputVertY;
   private bool firstZeroReceivedInARow = false;
   private bool playerIdle = false;
   private float maxSpeed = 10;

   private void PlayerMovement(float x, float y)
   {
      PlayerIdleCheck(x, y);

      if (!playerIdle) // skip sending extra zero vertors when player isn't moving
      {
         Vector3 playerMovementRotation = new Vector3(x, 0f, y) * maxSpeed;

         Vector3 camRotation = playerCamera.transform.forward;
         camRotation.y = 0f; // zero out camera's vertical axis so it doesn't make them fly

         // need to clamp camera rotation to x/z only and not y vertical 
         Vector3 playerMovementWithCameraRotation = Quaternion.LookRotation(camRotation) * playerMovementRotation;

         // rounded to two decimal places
         Vector3 roundedVelocity
            = new Vector3(Mathf.Round(playerMovementWithCameraRotation.x * 100f) / 100f, 0f, Mathf.Round(playerMovementWithCameraRotation.z * 100f) / 100f);

         // Debug.Log("velocity to send: " + roundedVelocity.ToString("f6"));

         player.AddForce(roundedVelocity, ForceMode.VelocityChange);

         if (WebSocketService.Instance.matchInitialized)
         {
            WebSocketService.Instance.SendVelocity(roundedVelocity);
         }
      }
   }

   // A check to see if the user stopped moving
   private void PlayerIdleCheck(float x, float y)
   {
      if (x == 0 && y == 0)
      {
         if (firstZeroReceivedInARow)
         {
            // we have two zero messages, player not moving, stop sending messages
            playerIdle = true;
         }
         else
         {
            firstZeroReceivedInARow = true;
         }
      }
      else
      {
         // player moved, set both to false
         firstZeroReceivedInARow = false;
         playerIdle = false;
      }
   }

   void Update()
   {
      // limit player speed
      if (player.velocity.magnitude > maxSpeed)
      {
         player.velocity = Vector3.ClampMagnitude(player.velocity, maxSpeed);
      }

      inputHorX = Input.GetAxis("Horizontal");
      inputVertY = Input.GetAxis("Vertical");
      // actual player update is performed in FixedUpdate

      if (Input.GetMouseButtonDown(0))
      {
         _throwKeyPressedStartTime = Time.time;
      }

      if (Input.GetMouseButtonUp(0))
      {

         // allows us to click the button while over it with the mouse
         if (EventSystem.current.IsPointerOverGameObject())
            return;

         _ballActionHandler.ThrowBall(player.transform.position, player.transform.forward, _throwKeyPressedStartTime);
      }
   }

   void FixedUpdate()
   {
      // This is linked to the project settings under Time > Fixed Timestamp
      // Currently set to .02 seconds, which is 20ms
      PlayerMovement(inputHorX, inputVertY);
   }

   void Start()
   {
      // For now just hit this variable to create the singleton
      WebSocketService.Instance.init();

      player = GetComponent<Rigidbody>();
      _ballActionHandler = new BallActionHandler(playerCamera, ball, baseBallThrust);

      // Give the websocket a reference to the object so it can know where its position is
      WebSocketService.Instance.SetLocalPlayerRef(player);
   }
}
