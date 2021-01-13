using UnityEngine;
using NativeWebSocket;

public class WebSocketService : Singleton<WebSocketService>
{
   private StatusController _statusController = null;
   private Menu _menu = null;
   private EnemyPositionHandler _enemyPositionHandler = null;
   private PlayerColorService _playerColorService;
   private Rigidbody localPlayerReference;
   private bool intentionalClose = false;
   private string matchId;
   private int playerMovementMessageSequence = 0;
   private WebSocket _websocket;
   private string _webSocketDns = "wss://YOUR_UNIQUE_API_PREFIX.execute-api.YOUR_REGION.amazonaws.com/YOUR_STAGE_NAME";

   public const string FirstToJoinOp = "0";
   public const string RequestStartOp = "1";
   public const string PlayingOp = "11";
   public const string ThrowOp = "5";
   public const string BlockHitOp = "9";
   public const string YouWonOp = "91";
   public const string YouLostOp = "92";
   public const string OpponentVelocity = "21";

   public bool matchInitialized = false;
   public string playerNum;
   public string enemyNum;

   // All messages received through the websocket connection are processed here
   private void ProcessReceivedMessage(string message)
   {
      GameMessage gameMessage = JsonUtility.FromJson<GameMessage>(message);

      if (gameMessage.opcode == PlayingOp)
      {
         Debug.Log("Playing op code received: player 2 joined, game started");
         matchId = gameMessage.uuid;

         _statusController.SetText(StatusController.Playing);

         // the server assigns player starting position, set here
         PlayerPositionMessage posMessage = JsonUtility.FromJson<PlayerPositionMessage>(message);
         localPlayerReference.position = posMessage.velocity;

         // establish p1 and p2
         playerNum = posMessage.player;
         if (playerNum == "1")
         {
            enemyNum = "2";
         }
         else
         {
            enemyNum = "1";
         }

         _playerColorService.SetColors(playerNum);

         // we also get the enemy's start position, set here
         _enemyPositionHandler.init(posMessage.enemyVelocity);

         matchInitialized = true;

         playerMovementMessageSequence = 0;

         // we don't need to send out the starting positions as the server already sends both player and enemy positions for each.
      }
      else if (gameMessage.opcode == OpponentVelocity)
      {
         PlayerPositionMessage posMessage = JsonUtility.FromJson<PlayerPositionMessage>(message);
         _enemyPositionHandler.UpdateVelocity(posMessage);
      }
      else if (gameMessage.opcode == ThrowOp)
      {
         Debug.Log(gameMessage.message);
      }
      else if (gameMessage.opcode == YouWonOp)
      {
         _statusController.SetText(StatusController.YouWon);
         QuitGame();
      }
      else if (gameMessage.opcode == YouLostOp)
      {
         _statusController.SetText(StatusController.YouLost);
         QuitGame();
      }
      else if (gameMessage.opcode == FirstToJoinOp)
      {
         matchId = gameMessage.uuid;
      }
   }

   private void SetupWebsocketCallbacks()
   {
      _websocket.OnOpen += () =>
      {
         Debug.Log("Connection open!");
         intentionalClose = false;
         GameMessage startRequest = new GameMessage("OnMessage", RequestStartOp);
         SendWebSocketMessage(JsonUtility.ToJson(startRequest));
      };

      _websocket.OnClose += (e) =>
      {
         Debug.Log("Connection closed!");

         // only do this if someone quit the game session, and not for a game ending event
         if (!intentionalClose)
         {
            UnityMainThreadHelper.wkr.AddJob(() =>
            {
               _menu.Disconnected();
            });
         }
      };

      _websocket.OnMessage += (bytes) =>
      {
         // Debug.Log("OnMessage!");
         string message = System.Text.Encoding.UTF8.GetString(bytes);
         // Debug.Log(message.ToString());

         ProcessReceivedMessage(message);
      };

      _websocket.OnError += (e) =>
      {
         Debug.Log("Error! " + e);
      };
   }

   // Creates a websocket connection to the server and establishes the connection's lifecycle callbacks.
   // Once the connection is established, OnOpen, it automatically attempts to create or join a game through the RequestStartOp code.
   async public void FindMatch()
   {
      // waiting for messages
      await _websocket.Connect();
   }

   private void SendVectorAsMessage(Vector3 vector, string opCode, int seq)
   {
      SerializableVector3 posToSend = vector;
      GameMessage posMessage = new PlayerPositionMessage("OnMessage", opCode, posToSend, new SerializableVector3(), 0, seq, "", localPlayerReference.position);
      posMessage.uuid = matchId;
      SendWebSocketMessage(JsonUtility.ToJson(posMessage));
   }

   public void BlockHit()
   {
      GameMessage blockHitMessage = new GameMessage("OnMessage", BlockHitOp);
      SendWebSocketMessage(JsonUtility.ToJson(blockHitMessage));
   }

   public async void SendWebSocketMessage(string message)
   {
      if (_websocket != null && _websocket.State == WebSocketState.Open)
      {
         // Sending plain text
         await _websocket.SendText(message);
      }
   }

   public void SendVelocity(Vector3 velocityIn)
   {
      SendVectorAsMessage(velocityIn, OpponentVelocity, playerMovementMessageSequence++);
   }

   public void SetLocalPlayerRef(Rigidbody localPlayerReferenceIn)
   {
      localPlayerReference = localPlayerReferenceIn;
   }

   public async void QuitGame()
   {
      intentionalClose = true;
      matchInitialized = false;
      _menu.ShowFindMatch();
      await _websocket.Close();
   }

   private async void OnApplicationQuit()
   {
      await _websocket.Close();
   }

   void Start()
   {
      Debug.Log("Websocket start");

      intentionalClose = false;
      _statusController = FindObjectOfType<StatusController>();
      _menu = FindObjectOfType<Menu>();
      _enemyPositionHandler = FindObjectOfType<EnemyPositionHandler>();
      _websocket = new WebSocket(_webSocketDns);
      _playerColorService = FindObjectOfType<PlayerColorService>();

      SetupWebsocketCallbacks();
      FindMatch();
   }

   void Update()
   {
      // Debug canvas text to track player coordinates 
      if (_statusController != null && _enemyPositionHandler != null && localPlayerReference != null
         && _statusController._p1 != null && _statusController._p2 != null)
      {
         _statusController._p1.text = "P" + playerNum + " (You): " + localPlayerReference.position.ToString();
         _statusController._p2.text = "P" + enemyNum + ": " + _enemyPositionHandler.enemy.transform.position.ToString();
      }

#if !UNITY_WEBGL || UNITY_EDITOR
      _websocket.DispatchMessageQueue();
#endif
   }

   public void init() { }

   protected WebSocketService() { }
}
