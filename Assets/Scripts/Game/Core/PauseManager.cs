using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    private static float prevTimeScale = 1f;
    public static bool isPaused { get; private set; }

    [System.Obsolete]
    public static void Pase()
    {
        if (isPaused) return;
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;

        foreach (var anim in Object.FindObjectsOfType<Animator>())
        {
            anim.enabled = false;
        }
    }

    [System.Obsolete]
    public static void Resume()
    {
        if (!isPaused) return;
        Time.timeScale = prevTimeScale;
        isPaused = false;
        foreach (var anim in Object.FindObjectsOfType<Animator>())
        {
            anim.enabled = true;
        }
    }

}
