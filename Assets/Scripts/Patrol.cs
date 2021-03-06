﻿using System;
using UnityEngine;
using UnityEngine.AI;

public class Patrol : MonoBehaviour
{
    [Serializable]
    public class Waypoint
    {
        public Transform Transform;
        public float pauseTime = 0;
        public Actions pauseType;
    }

    public enum Actions
    {
        Freeze = 0,
        Rotate = 1
    }

    //private List<Vector3> _wayPoints = new List<Vector3>();
    public Waypoint[] Waypoints;
    public float Speed = 5f;
    public float CutCornerDistance = 5f;
    public int _nextWayPoint = 0;
    //private bool _isDoingSmoothCorner = false;
    private int _currentWayPoint = 0;
    private int _previousWaypoint = 0;
    private Quaternion _lookRotation;
    private Vector3 _target;
    private Vector3 _direction;
    private bool _touched = false;
    private Material enemyMat;

    //private float _cutCornerFactor = 0;
    public float MaxCutCornerFactor = 5;
    public float CornerIncrementSpeed = 0.1f;
    private Sneeze[] _sneezes;
    private float _timePaused = 0;

    private NavMeshAgent _navMeshAgent;
    private bool _shouldMove = true;
    //avoidance
    private GameObject Player;
    public float _DetectionRadius = 5f;
    public float _LookAtPlayerRotationSpeedMultiplier = 2;
    //public Material GreenMaterial;

    bool _wasStoppedBeforePause = false; // Set to our nav mesh agent's stopped value before pausing

    void Awake ()
    {
        _sneezes = GetComponentsInChildren<Sneeze>();
        if (_sneezes.Length == 0)
        {
            Debug.LogError("Enemy doesn't have any Sneeze components!");
        }

        _navMeshAgent = GetComponent<NavMeshAgent>();
        enemyMat = transform.GetChild(0).GetComponent<Renderer>().material;
        enemyMat.color = new Color(0.35f, 0.35f, 0.35f);
    }

    private void OnEnable()
    {
        GameManager.OnPause += OnPause;
    }

    private void OnDisable()
    {
        GameManager.OnPause -= OnPause;
    }

    void Start()
    {
        _previousWaypoint = Waypoints.Length - 1;
        Player = GameObject.Find("Player");
        // _wayPoints.Add(Vector3.zero);
        //for(int i = 0; i < Waypoints.Length)
    }

    void OnParticleCollision(GameObject other)
    {
        //Debug.Log("ChainSneeze activated");
        enemyMat.color = new Color(0,0.3f,0.3f);
        Sneeze();
        _touched = true;
        gameObject.GetComponent<ActorRotation>().SetTouched();
        //GetComponentInChildren<MeshRenderer>().material = GreenMaterial;
        GameManager.IsPlayerSneezing = true;
    }

    void Update()
    {
        if (!_navMeshAgent) return;

        if (GameManager.IsPlayerSneezing || GameManager.Paused)
        {
            _navMeshAgent.isStopped = true;
            return;
        }
        if (Waypoints.Length > 1)
        {
            for (int i = 0; i < Waypoints.Length; i++)
            {
                if (!_touched)
                {
                    int next = i + 1;
                    if (next >= Waypoints.Length)
                    {
                        next = 0;
                    }
                    Debug.DrawLine(Waypoints[i].Transform.position, Waypoints[next].Transform.position, Color.black);
                }
            }
        }
        GetComponent<Wiggle>().ShouldWiggle = false;

        if (!_touched)
        {
            _shouldMove = true;
            _navMeshAgent.isStopped = false;
            //*************
            //avoidance
            //*************
            checkIfWeShouldAvoidPlayer();
            if (!_shouldMove)
                lookTowardsPlayer();

            //*************
            //movement and looking
            //*************
            if (_shouldMove && Waypoints.Length > 0)
            {
                GetComponent<Wiggle>().ShouldWiggle = true;
                CheckIfWeShouldCutCorner();
                CalculateTargetAndDirection();
                //this check shouldn't be necessary anymore but w/e, gamejam code
                // Only turn if we aren't on top of the target point
                if (_direction.magnitude > 0.0f)
                {
                    _lookRotation = Quaternion.LookRotation(_direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, _lookRotation, Time.deltaTime * Speed);
                }

                //*************
                //waypoint actions
                //*************
                //waypoint reached, do waypoint action and select next waypoint
                float minRadius = 0.01f;
                Vector3 thisPosNoY = transform.position, waypointNoY = Waypoints[_currentWayPoint].Transform.position;
                thisPosNoY.y = 0;
                waypointNoY.y = 0;

                if (Vector3.Distance(thisPosNoY, waypointNoY) < minRadius)
                {
                    GetComponent<Wiggle>().ShouldWiggle = false;
                    _timePaused += Time.deltaTime;
                    switch (Waypoints[_currentWayPoint].pauseType)
                    {
                        case Actions.Freeze:
                            break;
                        case Actions.Rotate:
                            gameObject.GetComponent<ActorRotation>().SetShouldRotate(true);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    if (_timePaused >= Waypoints[_currentWayPoint].pauseTime)
                    {
                        if (Waypoints.Length > 0)
                        {
                            switch (Waypoints[_currentWayPoint].pauseType)
                            {
                                case Actions.Freeze:
                                    break;
                                case Actions.Rotate:
                                    gameObject.GetComponent<ActorRotation>().SetShouldRotate(false);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            _timePaused = 0;
                            AddCurrentWaypoint();
                        }
                    }
                }

                Debug.DrawLine(transform.position, _target, Color.green);

                _navMeshAgent.destination = _target;
                GetComponent<Wiggle>().WiggleMethod();
            }
        }
    }

    public void OnPause(bool paused)
    {
        if (_navMeshAgent)
        {
            if (paused)
            {
                _wasStoppedBeforePause = _navMeshAgent.isStopped;
                _navMeshAgent.isStopped = true;
            }
            else
            {
                _navMeshAgent.isStopped = _wasStoppedBeforePause;
            }
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
        if (_touched) return;
        if (_navMeshAgent) _navMeshAgent.isStopped = true;

        foreach (var sneeze in _sneezes)
        {
            sneeze.Play();
        }
        
        GameManager.Camera.Shake();
        GameManager.VibrationManager.vibrate();
        GameManager.AudioManager.PlaySound(AudioManager.Sound.Sneeze);
    }

    void CheckIfWeShouldCutCorner()
    {
        //if we are within cutcorner distance, start cutting corner
        if (Waypoints[_currentWayPoint].pauseTime == 0 && ((Waypoints[_currentWayPoint].Transform.position - transform.position).magnitude < CutCornerDistance))
        {
            //_isDoingSmoothCorner = true;
           // _cutCornerFactor = 0;
            if (Waypoints.Length > 0)
                AddCurrentWaypoint();
        }
        //if we are on the line to the next point, stop cutting corner
        //if ((Waypoints[_currentWayPoint].Transform.position - transform.position).magnitude + (Waypoints[_previousWaypoint].Transform.position - transform.position).magnitude == (Waypoints[_currentWayPoint].Transform.position - Waypoints[_previousWaypoint].Transform.position).magnitude)
            //_isDoingSmoothCorner = false;
    }

    void CalculateTargetAndDirection()
    {
        _target = Waypoints[_currentWayPoint].Transform.position;
        //_direction = (_target - transform.position).normalized;
        _direction = _navMeshAgent.velocity.normalized;
    }

    void checkIfWeShouldAvoidPlayer()
    {
        Debug.DrawLine(transform.forward, transform.forward * 5, Color.blue);
        if ((Player.transform.position - transform.position).magnitude < _DetectionRadius)
        {
            float test = Vector3.Angle((Player.transform.position - transform.position), transform.forward);
            test = Mathf.Repeat(test, 360);
            if (test < 60.0f)
            {
                RaycastHit hit;
                Vector3 direction = (Player.transform.position - transform.position).normalized;
                if (Physics.Raycast(transform.position, direction, out hit))
                {
                    if (hit.collider.gameObject.name == "Player")
                    {
                        _shouldMove = false;
                        _navMeshAgent.isStopped = true;
                    }
                }
            }
        }
    }

    void lookTowardsPlayer()
    {
        var lookPos = Player.transform.position - transform.position;
        lookPos.y = 0;
        var rotation = Quaternion.LookRotation(lookPos);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * _LookAtPlayerRotationSpeedMultiplier);

        /*Vector3 lerpValue = Vector3.Lerp(transform.position + transform.forward, Player.transform.position, Time.deltaTime * _LookAtPlayerRotationSpeedMultiplier);
        //lerpValue.x = 0;
        //lerpValue.z = 0;
        Debug.Log("looking at player");
        //transform.LookAt(lerpValue);*/
    }
}