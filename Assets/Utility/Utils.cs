using System;
using UnityEngine;

public static class Utils
{
    public static int SystemTime
    {
        get
        {
            // Custom system time to be sure to have our own precision with the correct units
            var now = DateTime.Now;
            return now.Hour * 60 * 60 * 1000 + now.Minute * 60 * 1000 + now.Second * 1000 + now.Millisecond;
        }
    }
    
    // if 'readFromInput == true': Reads the inputs from user keyboard and populates 'msg' and 'shapeComponent' accordingly
    // else: Reads the inputs inside 'msg' and populates 'shapeComponent' accordingly
    public static bool GetUserInput(ref ReplicationMessage msg, ref ShapeComponent shapeComponent, bool readFromInput)
    {
        int speed = 4;
        bool inputDetected = false;
        shapeComponent.speed = Vector2.zero;
        if ((!readFromInput && msg.inputA == 1) || (readFromInput && Input.GetKey(KeyCode.A)))
        {
            if (readFromInput) msg.inputA = 1;
            shapeComponent.speed = Vector2.left * speed;
            inputDetected = true;
        }
        else if ((!readFromInput && msg.inputW == 1) || (readFromInput && Input.GetKey(KeyCode.W)))
        {
            if (readFromInput) msg.inputW = 1;
            shapeComponent.speed = Vector2.up * speed;
            inputDetected = true;
        }
        else if ((!readFromInput && msg.inputS == 1) || (readFromInput && Input.GetKey(KeyCode.S)))
        {
            if (readFromInput) msg.inputS = 1;
            shapeComponent.speed = Vector2.down * speed;
            inputDetected = true;
        }
        else if ((!readFromInput && msg.inputD == 1) || (readFromInput && Input.GetKey(KeyCode.D)))
        {
            if (readFromInput) msg.inputD = 1;
            shapeComponent.speed = Vector2.right * speed;
            inputDetected = true;
        }
        return inputDetected;
    }
}
