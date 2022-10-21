using UnityEngine;

public class PlayerColorService : MonoBehaviour
{
   // set in Unity editor in PlayerColorService
   public Enemy enemy;
   public Rigidbody player;

   public void SetColors(string localPlayerNumber)
   {
      var playerRenderer = player.GetComponent<Renderer>();
      var enemyRenderer = enemy.GetComponent<Renderer>();

      Color playerColor = Color.blue;
      Color enemyColor = Color.red;

      if (localPlayerNumber == "2")
      {
         // player 1 always blue
         playerColor = Color.red;
         enemyColor = Color.blue;
      }
      playerRenderer.material.SetColor("_Color", playerColor);
      enemyRenderer.material.SetColor("_Color", enemyColor);
   }
}
