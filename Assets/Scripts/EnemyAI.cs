using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

public class EnemyAI : MonoBehaviour
{
    public EnemyType enemy;
    public NavMeshAgent agent;
    public GameObject player;
    public PlayerTarget playerTarget;
    private Player playerMove;
    public LayerMask whatIsGround, whatIsPlayer;

    public bool isShot = false;

    //Patrolling
    public Vector3 walkPoint;
    bool walkPointSet;

    //Attacking
    bool alreadyAttacked;

    //States 
    public bool playerInSightRange, playerInAttackRange;

    private Animator animator;
    private Rig rig;
    [SerializeField] List<Transform> effectTransforms = new List<Transform>();

    private void Awake(){
        player = GameObject.Find("Player");
        playerTarget = player.GetComponent<PlayerTarget>();
        playerMove = player.GetComponent<Player>();
        agent = GetComponent<NavMeshAgent>();
        agent.speed = enemy.speed;
        animator = GetComponentInChildren<Animator>();
        rig = GetComponentInChildren<Rig>();
    }

    private void Update(){
        //Check for sight and attack range
        playerInSightRange = Physics.CheckSphere(transform.position, enemy.sightRange, whatIsPlayer); 
        playerInAttackRange = Physics.CheckSphere(transform.position, enemy.attackRange, whatIsPlayer); 

        if(!playerInSightRange && !playerInAttackRange && !isShot) Patroling();
        if((playerInSightRange && !playerInAttackRange) || isShot) Chasing();
        if(playerInAttackRange) Attacking();
    }

    private void Patroling(){
        rig.weight = 0;
        animator.SetBool("isMoving", true);
        if(!walkPointSet) SearchWalkPoint();

        if(walkPointSet){
            agent.SetDestination(walkPoint);
        }

        Vector3 distToWalkPoint = transform.position - walkPoint;
        
        //Walkpoint Reached
        if(distToWalkPoint.magnitude < 1){
            walkPointSet = false;
        }

        //Obstacle found
        if(Physics.Raycast(transform.position, transform.forward, 1f)){
            walkPointSet = false;
        }
    }

    private void SearchWalkPoint(){
        //Calculate random point in range
        float randomZ = Random.Range(-enemy.walkPointRange, enemy.walkPointRange);
        float randomX = Random.Range(-enemy.walkPointRange, enemy.walkPointRange);
        float randomY = Random.Range(-4f, 4f);

        walkPoint = new Vector3(transform.position.x + randomX, randomY, transform.position.z + randomZ);

        if(Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround)){
            walkPointSet = true;
        }
    }

    private void Chasing(){
        rig.weight = 0;
        animator.SetBool("isMoving", true);
        agent.SetDestination(player.transform.position);
    }

    private void Attacking(){
        rig.weight = 1;
        //Make sure enemy doesn't move
        animator.SetBool("isMoving", false);
        agent.SetDestination(transform.position);
        var lookPos = player.transform.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        if(!alreadyAttacked && playerMove.controller.isGrounded){

            //Attack
            if(enemy.ps != null){
                Vector3 particleOffset = new Vector3(0f, 0f, 0.1f);
                ParticleSystem muzzleFlash = Instantiate(enemy.ps, new Vector3(0, 0, 1), transform.rotation, transform);
                muzzleFlash.transform.localPosition = new Vector3(0, 0, 1);
                muzzleFlash.Play();
                StartCoroutine(DestroyAfterSeconds(enemy.psTime, muzzleFlash.gameObject));
            }

            if(enemy.lineRenderer != null){
                foreach(Transform tran in effectTransforms){
                    LineRenderer lr = Instantiate(enemy.lineRenderer, tran.position, transform.rotation, transform);
                    lr.transform.position = tran.position;
                    lr.SetPosition(1, tran.position);
                    StartCoroutine(DestroyAfterSeconds(enemy.psTime, lr.gameObject));
                
                }
            }

            if(Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, enemy.attackRange)){
                if(enemy.damageOverTime){
                    IEnumerator takedot =  playerTarget.TakeDamageOverTime(enemy.damage, enemy.hit_num);
                    StartCoroutine(takedot);
                    // if(playerTarget.currentHealth-enemy.damage<=0){
                    //     Destroy(playerTarget.gameObject);
                    // }
                }else{
                    playerTarget.TakeDamage(enemy.damage);
                }

                alreadyAttacked = true;
            }

            StartCoroutine(resetAttack());
        }
    }

    private IEnumerator resetAttack(){
        yield return new WaitForSeconds(enemy.timeBetweenAttacks);
        alreadyAttacked = false;
        isShot = false;
    }

    private IEnumerator DestroyAfterSeconds(float seconds, GameObject muzzleFlash){
        yield return new WaitForSeconds(seconds);
        Destroy(muzzleFlash.gameObject);
    }

}
