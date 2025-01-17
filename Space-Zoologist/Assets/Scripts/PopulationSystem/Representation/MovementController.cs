﻿using System;
using UnityEngine;
using System.Collections.Generic;

// TODO refactor MoveInDirection
/// <summary>
/// Takes in a path (List<Vector3>) and moves the attached gameobject through it or moves in a specified direction.
/// </summary>
public class MovementController : MonoBehaviour
{
    public bool DestinationReached { get; private set; }
    public bool DestinationCancelled { get; private set; }
    public bool HasPath { get; private set; }
    public bool IsPaused = false;

    private Animal Animal { get; set; }
    private List<Vector3> PathToDestination { get; set; }
    private int PathIndex = 0;
    public Vector3 NextPathTile { get; private set; }
    // Animal doesn't change direction until they've moved a certain distance in that direction
    private float ChangeDirectionThreshold = 0.5f;
    private float ChangeDirectionMovement = 0f;

    private float bufferedSpeed = -1;
    public void Awake()
    {
        this.Animal = this.gameObject.GetComponent<Animal>();
        this.DestinationReached = false;
        this.HasPath = false;
    }
    private float CalculateSpeed()
    {
        if (IsPaused)
        {
            return 0;
        }
        if (!this.Animal.MovementData.IsModifierChanged && bufferedSpeed != -1)
        {
            this.Animal.MovementData.Speed = bufferedSpeed;
            return bufferedSpeed;
        }
        bufferedSpeed = Animal.MovementData.CalculateModifiedSpeed();
        this.Animal.MovementData.Speed = bufferedSpeed;
        //HandleSpeedChange();
        return bufferedSpeed;
    }
    /// <summary>
    /// Called before update to assign a path.
    /// </summary>
    /// <param name="pathToDestination"></param>
    public void AssignPath(List<Vector3> pathToDestination, bool pathFound)
    {
        if (!pathFound)
        {
            //Debug.Log("Error path not found");
            return;
        }
        this.PathToDestination = pathToDestination;
        this.NextPathTile = new Vector3(this.PathToDestination[0].x + 0.5f, this.PathToDestination[0].y + 0.5f, 0);
        this.ResetPathfindingConditions();
        this.UpdateVisualLogic(this.NextPathTile);
        this.CalculateSpeed();
        this.HasPath = true;
        //Debug.Log(gameObject.name + " path assigned");
        //foreach (Vector3 pathTile in pathToDestination)
        //{
        //    Debug.Log(pathTile.x + ", " + pathTile.y);
        //}
    }

    public void ResetPathfindingConditions()
    {
        this.DestinationReached = false;
        this.DestinationCancelled = false;
        this.PathIndex = 0;
        this.HasPath = false;
    }

    public void TryToCancelDestination()
    {
        if (this.HasPath)
        {
            this.DestinationCancelled = true;
        }
    }

    /// <summary>
    /// Called in update to move towards destination. Returns returns the vector step moved.
    /// </summary>
    /// <returns></returns>
    public Vector3 MoveTowardsDestination()
    {
        if (IsPaused)
        {
            this.Animal.MovementData.MovementStatus = Movement.idle;
            return Vector3.zero;
        }
        if (this.PathToDestination.Count == 0)
        {
            this.PathIndex = 0;
            this.HasPath = false;
            this.DestinationReached = true;
            this.bufferedSpeed = -1;
            this.Animal.MovementData.Speed = 0;
            return Vector3.zero;
        }
        if (this.NextPathVectorReached(this.NextPathTile, this.transform.position))
        {
            this.PathIndex++;
            // Destination reached
            if (this.PathIndex == this.PathToDestination.Count)
            {
                this.PathIndex = 0;
                this.bufferedSpeed = -1;
                this.Animal.MovementData.Speed = 0;
                this.DestinationReached = true;
                this.HasPath = false;
                return Vector3.zero;
            }
            // Update to the next path tile and visual logic stuff
            else
            {
                // Need to translate back from grid to world
                this.NextPathTile = new Vector3(this.PathToDestination[this.PathIndex].x + 0.5f, this.PathToDestination[this.PathIndex].y + 0.5f, 0);
                // Debug.Log("("+this.NextPathTile.x+"),"+"("+this.NextPathTile.y+")");
                this.UpdateVisualLogic(this.NextPathTile);
            }
        }
        this.HasPath = true;
        Vector3 currentPos = transform.position;
        Vector3 newPos = this.MoveTowardsTile(this.transform.position, this.NextPathTile, this.CalculateSpeed());
        transform.position = newPos;
        return newPos - currentPos;
    }

    /// <summary>
    /// Move the animal by some vector step and do nothing else. Should probably not be used directly, unless in special cases.
    /// </summary>
    /// <param name="vector"></param>
    public void MoveVector(Vector3 vector)
    {
        transform.position += vector;
    }

    public void MoveInDirection(Direction direction)
    {
        if (IsPaused)
        {
            this.Animal.MovementData.MovementStatus = Movement.idle;
            return;
        }
        Vector3 vectorDirection = this.transform.position;
        vectorDirection.z = 0;
        float speed = this.CalculateSpeed() * Time.deltaTime;
        switch (direction)
        {
            case Direction.up:
                {
                    vectorDirection.y += speed;
                    break;
                }
            case Direction.down:
                {
                    vectorDirection.y -= speed;
                    break;
                }
            case Direction.left:
                {
                    vectorDirection.x -= speed;
                    break;
                }
            case Direction.right:
                {
                    vectorDirection.x += speed;
                    break;
                }
            case Direction.upRight:
                {
                    vectorDirection.x += speed;
                    vectorDirection.y += speed;
                    break;
                }
            case Direction.upLeft:
                {
                    vectorDirection.x -= speed;
                    vectorDirection.y += speed;
                    break;
                }
            case Direction.downRight:
                {
                    vectorDirection.x += speed;
                    vectorDirection.y -= speed;
                    break;
                }
            case Direction.downLeft:
                {
                    vectorDirection.x -= speed;
                    vectorDirection.y -= speed;
                    break;
                }
        }
        if (this.ChangeDirectionMovement < this.ChangeDirectionThreshold)
        {
            this.ChangeDirectionMovement += Vector3.Distance(this.transform.position, vectorDirection);
        }
        else
        {
            this.UpdateVisualLogic(vectorDirection);
            this.ChangeDirectionMovement = 0f;
        }
        this.transform.position = vectorDirection;
    }

    public void ForceMoveInDirection(Direction direction)
    {
        Vector3 vectorDirection = new Vector3(0, 0, 0);
        float speed = this.Animal.MovementData.BaseSpeed * Time.deltaTime;
        switch (direction)
        {
            case Direction.up:
                {
                    vectorDirection = new Vector3(this.transform.position.x, this.transform.position.y + 0.05f, 0);
                    break;
                }
            case Direction.down:
                {
                    vectorDirection = new Vector3(this.transform.position.x, this.transform.position.y + -0.05f, 0);
                    break;
                }
            case Direction.left:
                {
                    vectorDirection = new Vector3(this.transform.position.x + -0.05f, this.transform.position.y, 0);
                    break;
                }
            case Direction.right:
                {
                    vectorDirection = new Vector3(this.transform.position.x + 0.05f, this.transform.position.y, 0);
                    break;
                }
        }
        this.UpdateVisualLogic(vectorDirection);
        this.transform.position = vectorDirection;
    }

    public void StandStill()
    {
        ResetPathfindingConditions();
        this.Animal.MovementData.MovementStatus = Movement.idle;
    }

    // Can modify pointReachedOffset to have more precise movement towards each destination point
    private bool NextPathVectorReached(Vector3 destination, Vector3 currentLocation)
    {
        float pointReachedOffsetX = 0.5f;
        float pointReachedOffsetY = 0.3f;
        return currentLocation.x < destination.x + pointReachedOffsetX && currentLocation.x > destination.x - pointReachedOffsetX &&
        currentLocation.y < destination.y + pointReachedOffsetY && currentLocation.y > destination.y - pointReachedOffsetY;
    }

    // Can be modified for different movements potentially
    private Vector3 MoveTowardsTile(Vector3 currentPosition, Vector3 pathTile, float movementSpeed)
    {
        float step = movementSpeed * Time.deltaTime;
        return Vector3.MoveTowards(currentPosition, pathTile, step);
    }

    public void UpdateVisualLogic(Vector3 destination)
    {
        this.HandleDirectionChange(this.transform.position, destination);/*
        if (this.CalculateSpeed() > this.Animal.MovementData.RunThreshold)
        {
            this.Animal.MovementData.MovementStatus = Movement.running;
        }
        else
        {
            this.Animal.MovementData.MovementStatus = Movement.walking;
        }*/
    }
    // Can be modified for different angles of direction change
    private void HandleDirectionChange(Vector3 currentPosition, Vector3 nextTile)
    {
        if (Animal == null)
        {
            Animal = this.GetComponent<Animal>();
        }
        Vector3 direction = (nextTile - currentPosition).normalized;
        int angle = (int)Vector3.Angle(Vector3.up, direction);
        // Moving left. Subtracting 360 and making the angle positive makes it easy to determine what the angle of direction is
        if (direction.x <= 0)
        {
            angle -= 360;
            if (angle < 0)
            {
                angle *= -1;
            }
            if (angle > 310)
            {
                this.Animal.MovementData.CurrentDirection = Direction.up;
            }
            else if (angle < 230)
            {
                this.Animal.MovementData.CurrentDirection = Direction.down;
            }
            else
            {
                this.Animal.MovementData.CurrentDirection = Direction.left;
            }
        }
        else if (direction.x > 0)
        {
            if (angle < 50)
            {
                this.Animal.MovementData.CurrentDirection = Direction.up;
            }
            else if (angle > 130)
            {
                this.Animal.MovementData.CurrentDirection = Direction.down;
            }
            else
            {
                this.Animal.MovementData.CurrentDirection = Direction.right;
            }
        }
    }

    #if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if (HasPath)
        {
            Gizmos.color = Color.green;
            Vector3 start = transform.position;
            Vector3 end = PathToDestination[PathIndex] + Vector3.one * 0.5f;
            start.z = -1f;
            end.z = -1f;
            Gizmos.DrawLine(transform.position, end);
        }
    }


#endif
    
}
