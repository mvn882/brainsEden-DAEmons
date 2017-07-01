﻿using System;
using UnityEngine;

public class Patrol : MonoBehaviour
{
    [Serializable]
    public class Waypoint
    {
        public Transform Transform;
        public float pauzeTime = 0;
    }

    public Transform TransSelf;
    //private List<Vector3> _wayPoints = new List<Vector3>();
    public Waypoint[] Waypoints;
    public float Speed = 5f;
    public float CutCornerDistance = 5f;
    public int _nextWayPoint = 0;
    private bool _isDoingSmoothCorner = false;
    private int _currentWayPoint = 0;
    private int _previousWaypoint = 0;
    private Quaternion _lookRotation;
    private Vector3 _target;
    private Vector3 _direction;
    private bool _touched = false;

    private float _cutCornerFactor = 0;
    public float MaxCutCornerFactor = 5;
    public float CornerIncrementSpeed = 0.1f;
    private bool _hasSneezed = false;
    private Sneeze _sneeze;
    private float _timePauzed = 0;

    void Awake ()
    {
        _sneeze = GetComponentInChildren<Sneeze>();
    }
    void Start()
    {
        _previousWaypoint = Waypoints.Length - 1;
        // _wayPoints.Add(Vector3.zero);
        //for(int i = 0; i < Waypoints.Length)
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            _touched = true;
            Debug.Log("Don't touch me I'm Scared");
            Sneeze();
        }
    }

    void OnParticleCollision(GameObject other)
    {
        _touched = true;
        Debug.Log("ChainSneeze activated");
        Sneeze();
    }

    void Update()
    {
        for (int i = 0; i < Waypoints.Length; i++)
        //float factor = 250;
        if (!_touched)
        {
            int next = i + 1;
            if (next >= Waypoints.Length)
            {
                next = 0;
            }
            Debug.DrawLine(Waypoints[i].Transform.position, Waypoints[next].Transform.position,Color.black);
            
        }
        if (!_touched)
        {
            //if we are within cutcorner distance, start cutting corner
            if (Waypoints[_currentWayPoint].pauzeTime == 0 && ((Waypoints[_currentWayPoint].Transform.position - transform.position).magnitude < CutCornerDistance))
            {
                _isDoingSmoothCorner = true;
                _cutCornerFactor = 0;
                if (Waypoints.Length > 0)
                    AddCurrentWaypoint();
            }
            //if we are on the line to the next point, stop cutting corner
            if ((Waypoints[_currentWayPoint].Transform.position - transform.position).magnitude + (Waypoints[_previousWaypoint].Transform.position - transform.position).magnitude == (Waypoints[_currentWayPoint].Transform.position - Waypoints[_previousWaypoint].Transform.position).magnitude)
                _isDoingSmoothCorner = false;
            if (_isDoingSmoothCorner)
            {
                if (_cutCornerFactor < MaxCutCornerFactor)
                {
                    _cutCornerFactor += CornerIncrementSpeed;
                }
                Vector3 smoothDirection = (Waypoints[_currentWayPoint].Transform.position - Waypoints[_previousWaypoint].Transform.position);
                smoothDirection.Normalize();
                _target = (Waypoints[_previousWaypoint].Transform.position + (smoothDirection * _cutCornerFactor));
                if ((_target - transform.position).magnitude < 0.1f)
                {
                    _target = Waypoints[_currentWayPoint].Transform.position;
                }
                _direction = (_target - transform.position).normalized;
                Debug.DrawLine(transform.position, _target, Color.red);
            }
            else
            {
                _target = Waypoints[_currentWayPoint].Transform.position;
                _direction = (_target - transform.position).normalized;
            }

            // Only turn if we aren't on top of the target point
            if (_direction.magnitude > 0.0f)
            {
                _lookRotation = Quaternion.LookRotation(_direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, _lookRotation, Time.deltaTime * Speed);
            }

            //waypoint reached, select next waypoint
            if (TransSelf.position.Equals(Waypoints[_currentWayPoint].Transform.position))
            {
                _timePauzed += Time.deltaTime;
                if (_timePauzed < Waypoints[_currentWayPoint].pauzeTime)
                    return;
                if (Waypoints.Length > 0)
                {
                    _timePauzed = 0;
                    AddCurrentWaypoint();
                }
            }
            //Vector3 lerpValue = Vector3.Lerp(TransSelf.position + TransSelf.forward, Waypoints[_currentWayPoint].Transform.position, Time.deltaTime * 2);
            //TransSelf.LookAt(lerpValue/*Waypoints[_currentWayPoint].Transform.position*/);
            Debug.DrawLine(transform.position, _target, Color.green);
            TransSelf.position = Vector3.MoveTowards(TransSelf.position, _target, Speed * Time.deltaTime);
        }
    }

    void AddCurrentWaypoint()
    {
        ++_currentWayPoint;
        if (_currentWayPoint >= Waypoints.Length)
        {
            _currentWayPoint = 0;
        }
        _previousWaypoint = _currentWayPoint - 1;
        if (_currentWayPoint == 0)
        {
            _previousWaypoint = Waypoints.Length - 1;
        }
    }

    void Sneeze()
    {
        if (_hasSneezed) return;
        _hasSneezed = true;
        
        _sneeze.Play();
        GameManager.Camera.Shake();
        GameManager.AudioManager.PlaySound(AudioManager.Sound.HeadExplosion);
    }
}
