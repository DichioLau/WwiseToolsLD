using UnityEngine;
using UnityEngine.AI;

public class NPCPatrolStarterAssets : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator animator;
    public Transform[] waypoints;
    public float waitTime = 0.5f;
    public float arriveThreshold = 0.2f;

    private int currentIndex = 0;
    private bool waiting = false;
    private float waitCounter = 0f;

    void Start()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponent<Animator>();

        if (waypoints.Length > 0)
            agent.SetDestination(waypoints[currentIndex].position);
    }

    void Update()
    {
        animator.SetFloat("Speed", agent.velocity.magnitude);

        if (!waiting && agent.remainingDistance <= agent.stoppingDistance + arriveThreshold)
        {
            waiting = true;
            waitCounter = waitTime;
            agent.isStopped = true;
        }

        if (waiting)
        {
            waitCounter -= Time.deltaTime;
            if (waitCounter <= 0f)
            {
                waiting = false;
                agent.isStopped = false;
                currentIndex = (currentIndex + 1) % waypoints.Length;
                agent.SetDestination(waypoints[currentIndex].position);
            }
        }
    }
}
