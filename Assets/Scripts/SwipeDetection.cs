using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SwipeDetection : MonoBehaviour
{
    [SerializeField] private float minimumDistance = .2f;
    [SerializeField] private float maximumTime = 1f;
    [SerializeField, Range(0f, 1f)] private float directionThreshold = .9f;

    private InputManager inputManager;

    private Vector2 startPosition;
    private float startTime;
    private Vector2 endPosition;
    private float endTime;

    private void Awake()
    {
        inputManager = InputManager.Instance;
    }

    private void OnEnable()
    {
        inputManager.OnStartTouch += SwipeStart;
        inputManager.OnEndTouch += SwipeEnd;
    }

    private void OnDisable()
    {
        inputManager.OnStartTouch -= SwipeStart;
        inputManager.OnEndTouch -= SwipeEnd;
    }

    private void SwipeStart(Vector2 position, float time)
    {
        startPosition = position;
        startTime = time;
    }

    private void SwipeEnd(Vector2 position, float time)
    {
        endPosition = position;
        endTime = time;
        DetectSwipe();
    }

    private void DetectSwipe()
    {
        if (Vector3.Distance(startPosition, endPosition) >= minimumDistance && endTime - startTime <= maximumTime)
        {
            Debug.DrawLine(startPosition, endPosition, Color.red, 5f);
            Vector3 direction = endPosition - startPosition;
            Vector2 direction2D = new Vector2(direction.x, direction.y).normalized;
            SwipeDirection(direction2D);
        }
    }

    private void SwipeDirection(Vector2 direction)
    {
        if (Vector2.Dot(Vector2.up, direction) > directionThreshold)
        {
            SwipeUpEvent();
        }
        else if (Vector2.Dot(Vector2.down, direction) > directionThreshold)
        {
            SwipeDownEvent();
        }
        else if (Vector2.Dot(Vector2.left, direction) > directionThreshold)
        {
            SwipeLeftEvent();
        }
        else if (Vector2.Dot(Vector2.right, direction) > directionThreshold)
        {
            SwipeRightEvent();
        }
    }

    protected virtual void SwipeUpEvent() { }
    protected virtual void SwipeDownEvent() { }
    protected virtual void SwipeLeftEvent() { }
    protected virtual void SwipeRightEvent() { }
}
