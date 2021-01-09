using System;
using UnityEngine;

// Based on http://answers.unity.com/answers/956580/view.html
// I used strings as to not have to deal with rounding issues when passing through the server
[System.Serializable]
public struct SerializableVector3
{
   public string x;

   public string y;

   public string z;

   public SerializableVector3(float rX, float rY, float rZ)
   {
      x = rX.ToString("f3");
      y = rY.ToString("f3");
      z = rZ.ToString("f3");
   }

   // Returns a string representation of the object
   public override string ToString()
   {
      return String.Format("[{0}, {1}, {2}]", x, y, z);
   }

   // Automatic conversion from SerializableVector3 to Vector3
   public static implicit operator Vector3(SerializableVector3 rValue)
   {
      return new Vector3(NullCheckParse(rValue.x), NullCheckParse(rValue.y), NullCheckParse(rValue.z));
   }

   // Automatic conversion from Vector3 to SerializableVector3
   public static implicit operator SerializableVector3(Vector3 rValue)
   {
      return new SerializableVector3(rValue.x, rValue.y, rValue.z);
   }

   private static float NullCheckParse(string valueToCheck)
   {
      if (valueToCheck == null)
      {
         return 0f;
      }
      return float.Parse(valueToCheck);
   }
}
