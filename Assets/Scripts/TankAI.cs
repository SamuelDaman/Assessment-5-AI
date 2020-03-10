using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class TankAI : MonoBehaviour
{
    public enum states
    {
        Patrol,
        Seek,
        Flee
    }

    public states state;

    public TankAI opponentAI;

    private NavMeshAgent agent;

    public Transform turret;

    public Vector3[] patrolLocations;
    private int patrolIndex = 0;

    public Transform target;
    public LayerMask sightMask;

    public Text scoreText;
    int score = 0;

    public LineRenderer line;

    public void Initialize()
    {
        state = states.Patrol;
        transform.position = new Vector3(Random.Range(-1, 11), 0, Random.Range(-10, 11));
        turret.rotation = transform.rotation;
        patrolIndex = 0;
        patrolLocations = new Vector3[Random.Range(3, 11)];
        for (int i = 0; i < patrolLocations.Length; i++)
        {
            patrolLocations[i] = new Vector3(Random.Range(-14, 15), 0, Random.Range(-14, 15));
        }
        scoreText.text = "" + score;
    }

    // Start is called before the first frame update
    void Start()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        line = gameObject.GetComponent<LineRenderer>();
        Initialize();
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        turret.position = new Vector3(transform.position.x, transform.position.y + 0.4f, transform.position.z);
        line.SetPosition(0, turret.position + turret.forward);
        if (Physics.Raycast(turret.position, turret.forward, out hit, 50))
        {
            line.SetPosition(1, hit.point);
        }
        else
        {
            line.SetPosition(1, turret.TransformDirection(0, 0, 50));
        }
        switch (state)
        {
            case states.Patrol:
                Patrol();
                break;
            case states.Seek:
                Seek();
                break;
            case states.Flee:
                Flee();
                break;
            default:
                Debug.Log("Invalid State");
                break;
        }
    }

    void FixedUpdate()
    {
        StartCoroutine("LineOfSight");
    }

    void Patrol()
    {
        agent.SetDestination(patrolLocations[patrolIndex]);
        turret.rotation = Quaternion.RotateTowards(turret.rotation, transform.rotation, 0.5f);

        if (Vector3.Distance(transform.position, patrolLocations[patrolIndex]) < 1)
        {
            if (patrolIndex != patrolLocations.Length - 1)
            {
                patrolIndex++;
            }
            else
            {
                patrolIndex = 0;
            }
        }
    }

    void Seek()
    {
        Vector3 v = (target.position - turret.position).normalized;
        float angle = Mathf.Atan2(v.x, v.z) * (180 / Mathf.PI);
        Quaternion targetAngle = Quaternion.Euler(0, angle, 0);
        turret.rotation = Quaternion.RotateTowards(turret.rotation, targetAngle, 0.25f);
    }

    void Flee()
    {
        agent.SetDestination(target.TransformDirection
            (
                (target.InverseTransformDirection(turret.position).x / Mathf.Abs(target.InverseTransformDirection(turret.position).x)) * 10,
                0,
                target.InverseTransformDirection(turret.position).z - 2
            ));
        Seek();
    }

    IEnumerator LineOfSight()
    {
        Vector3 v = (target.position - turret.position).normalized;
        float dot = Vector3.Dot(turret.TransformDirection(0, 0, 1), v);
        Debug.DrawRay(turret.position, turret.TransformDirection(0, 0, 30), Color.red);
        Debug.DrawRay(turret.position, turret.TransformDirection(-1, 0, 1).normalized * 30, Color.green);
        Debug.DrawRay(turret.position, turret.TransformDirection(1, 0, 1).normalized * 30, Color.green);
        RaycastHit obstruction;
        if (dot > 0.7f && !Physics.Linecast(turret.position, target.position, out obstruction, sightMask))
        {
            Debug.DrawLine(turret.position, target.position, Color.grey);
            RaycastHit hit;
            opponentAI.state = states.Flee;
            if (state != states.Flee)
            {
                agent.SetDestination(transform.position);
                state = states.Seek;
            }
            if (Physics.Raycast(turret.position, turret.TransformDirection(0, 0, 1), out hit, float.PositiveInfinity))
            {
                line.SetPosition(1, hit.point);
                if (hit.collider.CompareTag("AI"))
                {
                    Debug.Log("Bang");
                    opponentAI.Initialize();
                    score++;
                    Initialize();
                }
            }
        }
        else if (dot < 0.7f || Physics.Linecast(turret.position, target.position, out obstruction, sightMask))
        {
            if (opponentAI.state == states.Flee)
            {
                opponentAI.state = states.Patrol;
            }
            state = states.Patrol;
        }
        return null;
    }
}
