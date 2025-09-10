/*
 * MoveBox.cs
 * Created by Lautaro Dichio (ldichio.com.ar)
 * 
 * Simple demo script that moves a GameObject back and forth on the X axis.
 * Used in test scenes to provide predictable motion for audio triggers and zones.
 */


using UnityEngine;

public class MoveBox : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Distance to move from the starting position.")]
    public float moveDistance = 3f;

    [Tooltip("Time in seconds to move from one point to the other.")]
    public float moveTime = 2f;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private float timer;
    private bool movingToEnd = true;

    void Start()
    {
        // Save the starting position and calculate the end position
        startPosition = transform.position;
        endPosition = startPosition + new Vector3(moveDistance, 0f, 0f); // Move along X axis
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Calculate normalized time between 0 and 1
        float t = timer / moveTime;
        t = Mathf.Clamp01(t);

        // Move object
        if (movingToEnd)
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
        else
            transform.position = Vector3.Lerp(endPosition, startPosition, t);

        // Switch direction when reaching the target
        if (t >= 1f)
        {
            timer = 0f;
            movingToEnd = !movingToEnd;
 
        }
    }
}
