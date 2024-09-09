using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsible for static variables and functions relevant to run functionality
/// </summary> 
public class RunUtility
{
    public const float METERS_PER_MILE = 1609.34f;


    /// <summary>
    /// http://www.simpsonassociatesinc.com/runningmath2.htm
    /// </summary>
    /// <param name="o2Cost">in mL/kg/min</param>
    /// <returns>Speed in miles per sec</returns>
    public static float CaclulateSpeedFromOxygenCost(float o2Cost)
    {
        const float a = 0.000104f;
        const float b = 0.182258f;
        float c = -4.6f - o2Cost;
        const float b_squared = b * b;
        float four_a_c = 4 * a * c;
        const float two_a = 2 * a;

        return (-b + Mathf.Sqrt(b_squared - four_a_c)) / (two_a * METERS_PER_MILE * 60f);
    }

    /// <summary>
    /// http://www.simpsonassociatesinc.com/runningmath2.htm
    /// </summary>
    /// <param name="speed">in miles per sec</param>
    /// <returns>in mL/kg/min</returns>
    public static float SpeedToOxygenCost(float speed)
    {
        const float a = 0.000104f;
        const float b = 0.182258f;
        const float c = -4.6f;

        speed = speed * METERS_PER_MILE * 60f;
        return a * Mathf.Pow(speed, 2) + b * speed + c;
    }

    /// <param name="milesPerSec">Speed in miles per second</param>
    /// <returns>A pretty mile pace string in the format of x:xx</returns>
    public static string SpeedToMilePaceString(float milesPerSec)
    {
        float minPerMile = 1f / (milesPerSec * 60f);
        int minutes = (int)minPerMile;
        int seconds = (int)((minPerMile - minutes) * 60);
        if (seconds < 10)
        {
            return $"{minutes}:0{seconds}";
        }
        else
        {
            return $"{minutes}:{seconds}";
        }
    }
}
