using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    /**
    * RigidBody2D component of the skeleton GameObject
    */
    private Rigidbody2D rigidBody2D;

    /**
    * Animator controller component of the skeleton GameObject
    */
    private Animator animator;

    /**
     * walking speed of the skeleton
     */
    public float speed = 2f;

    /**
     * should the skeleton walk towards the right (true) or the left (false) of the scene
     */
    private bool moveright = true;

    /**
     * is the skeleton facing right (true) or left (false)
     */
    public bool isFacingRight = false;

    /**
     * distance between the starting position (startpos) and ending position (endpos)
     */
    private readonly int unitsToMove = 8;

    /**
     * starting position of the skeleton's patrol when inactive
     */
    public double startPos = 0;

    /**
     * ending position of the skeleton's patrol when inactive
     */
    public double endPos = 0;

    /**
     * the player detection radius of the skeleton 
     */
    public float rangeDetect = 5;

    /**
     * is the skeleton idle (true) or not (false)
     */
    public bool isWaiting = false;

    /**
     * is the player in range (true) or not (false)
     */
    public bool inRange = false;

    /**
     * when the player is in range, corresponds to the x-coordinate of the player's position
     */
    private float targetPosition = 0f;

    /**
     * Vector towards the player's position
     */
    private Vector2 target = new(0, 0);

    public bool isAttacking = false;

    public AudioClip[] Skelehurt;

    public AudioClip Skeledeath;

    public AudioClip attack;

    private AudioSource audioSource;

    private GameObject sword;

    public int health;

    private bool playedOnceSkeleHurt = false;

    private bool playedOnceSkeleDeath = false;

    private bool isDead = false;

    public AudioClip skeleLaugh;

    private bool playedOnceSkeleLaugh = false;


    //Initalization
    public void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        rigidBody2D = GetComponent<Rigidbody2D>();
        startPos = transform.position.x;
        endPos = startPos + unitsToMove;
        isFacingRight = transform.localScale.x > 0;
        sword = GameObject.Find("playerAttackHitbox");
        health = 3;
    }

    //Executes every frame
    public void Update()
    {
        //map limits to avoid the skeleton falling
        if (startPos <= GameObject.Find("LimitLeft").transform.position.x)
        {
            startPos = GameObject.Find("LimitLeft").transform.position.x;
        }

        if (endPos >= GameObject.Find("LimitRight").transform.position.x)
        {
            endPos = GameObject.Find("LimitRight").transform.position.x;
        }

        //we check if the player is in range
        Range();

        //if the skeleton is idle, it will start a patrol
        if (!inRange && !isWaiting && !isAttacking && !isDead)
        {
            speed = 2;
            animator.SetBool("walk", true);
            if (moveright)
            {
                transform.position += speed * Time.deltaTime * transform.right;

                if (!isFacingRight)
                {
                    Flip();
                }
            }

            if (rigidBody2D.position.x >= endPos)
            {
                moveright = false;
                StartCoroutine(Wait());
            }

            if (!moveright)
            {
                transform.position += speed * Time.deltaTime * -transform.right;

                if (isFacingRight)
                {
                    Flip();
                }
            }

            if (rigidBody2D.position.x <= startPos)
            {
                moveright = true;
                StartCoroutine(Wait());
            }
        }

        //when the player is in range, the skeleton will run towards them
        if (inRange && !isAttacking && !isDead)
        {

            animator.SetBool("walk", true);
            speed = 4;
            //the patrol's borders move with the skeleton in it's center
            startPos = transform.position.x - (unitsToMove / 2);
            endPos = transform.position.x + (unitsToMove / 2);

            targetPosition = GameObject.Find("player").transform.position.x;

            if (targetPosition < transform.position.x && isFacingRight == true)
            {
                moveright = false;
                Flip();
            }

            if (targetPosition > transform.position.x && isFacingRight == false)
            {
                moveright = true;
                Flip();
            }

            //if the player's location is outside the boundaries of the map, the skeleton won't follow 
            if (targetPosition <= GameObject.Find("LimitLeft").transform.position.x)
            {
                targetPosition = GameObject.Find("LimitLeft").transform.position.x;
            }

            if (targetPosition >= GameObject.Find("LimitRight").transform.position.x)
            {
                targetPosition = GameObject.Find("LimitRight").transform.position.x;
            }

            target = new Vector2(targetPosition, transform.position.y);
            var step = speed * Time.deltaTime;
            transform.position = Vector2.MoveTowards(transform.position, target, step);

            if (Vector2.Distance(transform.position, GameObject.Find("player").transform.position) <= 2)
            {
                transform.position = transform.position;
                StartCoroutine(Attack());
            }

        }

        if (GetComponent<Collider2D>().IsTouching(sword.GetComponent<PolygonCollider2D>()))
        {
            audioSource.Stop();
            sword.SetActive(false);
            playedOnceSkeleHurt = false;
            if (health > 1)
            {
                StartCoroutine(SkeleHurt());
            }
            else
            {
                StartCoroutine(SkeleDeath());
            }
        }
    }

    //Flips the sprite on it's x axis
    public void Flip()
    {
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        isFacingRight = transform.localScale.x > 0;
        GetComponent<Rigidbody2D>().angularVelocity = 0;
    }

    //checks if the player is in the specified range
    public void Range()
    {
        if (Vector2.Distance(rigidBody2D.position, GameObject.Find("player").transform.position) <= rangeDetect)
        {
            inRange = true;
        }
        else
        {
            inRange = false;
        }
    }

    //Coroutine to use when the skeleton is idle
    IEnumerator Wait()
    {
        animator.SetBool("walk", false);
        isWaiting = true;
        yield return new WaitForSeconds(1f);
        isWaiting = false;
    }

    IEnumerator Attack()
    {
        isAttacking = true;
        speed = 0;
        animator.SetTrigger("attack");
        yield return new WaitForSeconds(1.05f);
        isAttacking = false;
    }

    IEnumerator SkeleHurt()
    {
        if (!playedOnceSkeleHurt)
        {
            audioSource.clip = Skelehurt[Random.Range(0, Skelehurt.Length)];
            audioSource.Play();
            playedOnceSkeleHurt = true;
        }
        if (sword.transform.parent.position.x > transform.position.x && health > 1)
        {
            animator.enabled = false;
            animator.enabled = true;
            animator.SetTrigger("hurt");
            yield return new WaitForSecondsRealtime(0.01f);
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(0.03f);
            Time.timeScale = 1;
            GetComponent<Rigidbody2D>().velocity = new Vector2(-6, 0);
        }
        else if (sword.transform.parent.position.x < transform.position.x && health > 1)
        {
            animator.enabled = false;
            animator.enabled = true;
            animator.SetTrigger("hurt");
            yield return new WaitForSecondsRealtime(0.01f);
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(0.03f);
            Time.timeScale = 1;
            GetComponent<Rigidbody2D>().velocity = new Vector2(6, 0);
        }
        health -= 1;
        yield return new WaitForSecondsRealtime(0.2f);
        if (health > 0)
        {
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        }
        yield return new WaitForSeconds(0.8f);
        sword.SetActive(true);

    }

    IEnumerator SkeleDeath()
    {
        playedOnceSkeleLaugh = false;
        isDead = true;
        if (!playedOnceSkeleDeath)
        {
            audioSource.clip = Skeledeath;
            audioSource.Play();
            playedOnceSkeleDeath = true;
        }
        rigidBody2D.bodyType = RigidbodyType2D.Static;
        GetComponent<Collider2D>().enabled = false;
        animator.enabled = false;
        animator.enabled = true;
        animator.Play("SkeleDeath");
        sword.SetActive(true);
        yield return new WaitForSecondsRealtime(5.02f);
        animator.Play("SkeleDeath 0");
        yield return new WaitForSecondsRealtime(1.02f);
        if (!playedOnceSkeleLaugh)
        {
            audioSource.clip = skeleLaugh;
            audioSource.Play();
            playedOnceSkeleLaugh = true;
        }
        rigidBody2D.bodyType = RigidbodyType2D.Dynamic;
        GetComponent<Collider2D>().enabled = true;
        health = 3;
        isDead = false;

    }

    public void AttackSound()
    {
        audioSource.clip = attack;
        audioSource.time = 0.05f;
        audioSource.Play();
    }
}




//18.02.2023 : made the code a little more  optimised, readable and fitting to conventions. Next : implement attack system