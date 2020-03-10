using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class TankAI : MonoBehaviour
{
    /// <summary>
    /// List of states used to decide how the AI should act in a given situation.
    /// </summary>
    public enum states
    {
        Patrol,
        Seek,
        Flee
    }

    public states state;    // The state the AI is currently in.

    public TankAI opponentAI;   // The AI script attached to the opposing AI.

    private NavMeshAgent agent; // Agent used to navigate around obstacles and across the level.

    public Transform turret;    // The tank's turret used for line-of-sight checks.

    public Vector3[] patrolLocations;   // List of locations that the AI moves between when patrolling.
    private int patrolIndex = 0;        // The index of the patrol location the AI is currently seeking.

    public Transform target;        // The AI's opponent.
    public LayerMask sightMask;     // A layer mask used to in line-of-sight checks.

    public Text scoreText;  // The tank's score display.
    private int score = 0;  // The tank's score.

    public LineRenderer line;   // Line renderer used to show where the tank is aiming.

    /// <summary>
    /// Function that initializes the AI's state, position, rotation, and patrol path. Also used to update the score.
    /// </summary>
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
        // This is used to update the line renderer's position.
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

        // Case switch checking what the AI's current state is.
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
        // Check line-of-sight every fixed update.
        LineOfSight();
    }

    /// <summary>
    /// Patrol behavior, the agent moves between a randomized list of points while looking for the enemy.
    /// </summary>
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


    /// <summary>
    /// Seek behavior, The AI turns it's turret to face the opponent.
    /// </summary>
    void Seek()
    {
        Vector3 v = (target.position - turret.position).normalized;
        float angle = Mathf.Atan2(v.x, v.z) * (180 / Mathf.PI);
        Quaternion targetAngle = Quaternion.Euler(0, angle, 0);
        turret.rotation = Quaternion.RotateTowards(turret.rotation, targetAngle, 0.25f);
    }

    /// <summary>
    /// Flee behavior, The agent moves away from the opponent's center of vision to avoid being shot.
    /// </summary>
    void Flee()
    {
        agent.SetDestination(target.TransformDirection
            (
                target.InverseTransformDirection(turret.position).x / Mathf.Abs(target.InverseTransformDirection(turret.position).x) * 10,
                0,
                target.InverseTransformDirection(turret.position).z / 1.1f
            ));
        Seek(); // While fleeing, the tank will still try to shoot the opponent before being shot.
    }

    /// <summary>
    /// Function that checks line-of-sight every fixed update.
    /// Line-of-sight is the primary method of deciding the AI's behavior.
    /// </summary>
    /// <returns></returns>
    void LineOfSight()
    {
        Vector3 v = (target.position - turret.position).normalized; // Direction to the target from the turret's position.
        float dot = Vector3.Dot(turret.forward, v);                 // Dot product of the turret forward direction and the direction to target.

        Debug.DrawRay(turret.position, turret.TransformDirection(-1, 0, 1).normalized * 30, Color.green);
        Debug.DrawRay(turret.position, turret.TransformDirection(1, 0, 1).normalized * 30, Color.green);

        RaycastHit obstruction;
        // If the target is in line of sight...
        if (dot > 0.7f && !Physics.Linecast(turret.position, target.position, out obstruction, sightMask))
        {
            Debug.DrawLine(turret.position, target.position, Color.grey);
            opponentAI.SetState(states.Flee);   // Set the enemy's state to flee.
            // If the AI is not currently fleeing...
            if (state != states.Flee)
            {
                agent.SetDestination(transform.position);   // Stop moving.
                state = states.Seek;                        // Start Seeking.
            }

            // If the enemy is straight ahead, this tank wins the round, scores, then starts the next round by re-initializing.
            RaycastHit hit;
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
        // If the opponent is out of sight, set the opponent to back to patrol and/or set itself back to patrol.
        else if (dot < 0.7f || Physics.Linecast(turret.position, target.position, out obstruction, sightMask))
        {
            if (opponentAI.state == states.Flee)
            {
                opponentAI.SetState(states.Patrol);
            }
            if (state == states.Seek)
            {
                state = states.Patrol;
            }
        }
    }

    /// <summary>
    /// Function used by the opponent AI to manipulate this one's behavior.
    /// </summary>
    /// <param name="desiredState"></param>
    public void SetState(states desiredState)
    {
        state = desiredState;
    }
}
