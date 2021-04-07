using System;

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
}
