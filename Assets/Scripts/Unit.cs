﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{

    public enum Task
    {
        idle, move, follow, chase, attack
    }
    const string ANIMATOR_SPEED = "Speed",
        ANIMATOR_ALIVE = "Alive",
        ANIMATOR_ATTACK = "Attack";

    public bool IsAlive { get { return hp > 0; } }
    public bool IsAttacking { get { return task == Task.attack; } }
    public static List<ISelectable> SelectableUnits { get { return selectableUnits; } }
    static List<ISelectable> selectableUnits = new List<ISelectable>();

    public float HealthPercent { get { return hp / hpMax; } }
    [Header("Unit")]

    [SerializeField]
    GameObject hpBarPrefab;
    [SerializeField]
    GameObject selectionIndicatorPrefab;
    [SerializeField]
    float hp;
    [SerializeField]
    protected float attackDistance = 1,
        attackCooldown = 1,
        attackDamage = 0,
        stoppingDistance = 1;

    protected HealthBar healthBar;
    protected SelectionIndicator selectionIndicator;
    protected NavMeshAgent nav;
    protected Transform target;

    float attackTimer;
    protected Animator animator;
    protected Task task = Task.idle;

    //UNIT STATISTICS
    protected float attack;
    protected float defence;
    protected float dmgMin;
    protected float dmgMax;
    protected float hpMax = 50;
    protected float movementSpeed;
    protected float cost;

    protected virtual void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        hp = hpMax;
        healthBar = Instantiate(hpBarPrefab, transform).GetComponent<HealthBar>();
        selectionIndicator = Instantiate(selectionIndicatorPrefab, transform).GetComponent<SelectionIndicator>();
    }
    protected virtual void Start()
    {
        
        if (this is ISelectable)
        {
            selectableUnits.Add(this as ISelectable);
            (this as ISelectable).SetSelected(false);
        }
    }

    protected virtual void OnDestroy()
    {
        if (this is ISelectable) selectableUnits.Remove(this as ISelectable);
    }

    protected virtual void Update()
    {
        Animate();
        if (IsAlive)
        {
            switch (task)
            {
                case Task.idle: Idling(); break;
                case Task.move: Moving(); break;
                case Task.follow: Following(); break;
                case Task.chase: Chasing(); break;
                case Task.attack: Attacking(); break;
                default:
                    break;
            }
        }


    }
    protected virtual void OnTriggerEnter(Collider other)
    {

    }

    protected virtual void OnTriggerExit(Collider other)
    {

    }
    protected virtual void Idling()
    {
        nav.velocity = Vector3.zero;
    }
    protected virtual void Attacking()
    {
        if (target)
        {
            nav.velocity = Vector3.zero;
            transform.LookAt(target); //obracanie w strone targetu
            float distance = Vector3.Magnitude(target.position - transform.position);
            if (distance <= attackDistance)
            {
                if ((attackTimer -= Time.deltaTime) <= 0) Attack();
            }

            else
            {
                task = Task.chase;
            }
        }
        else
        {
            task = Task.idle;
        }

    }
    protected virtual void Moving()
    {
        float distance = Vector3.Magnitude(nav.destination - transform.position);
        if (distance < stoppingDistance)
        {
            task = Task.idle;
        }
    }
    protected virtual void Following()
    {
        if (target)
        {
            nav.SetDestination(target.position);
        }
        else
        {
            task = Task.idle;
        }
    }
    protected virtual void Chasing()
    {
        if (target)
        {
            nav.SetDestination(target.position);
            float distance = Vector3.Magnitude(nav.destination - transform.position);
            if (distance <= attackDistance)
            {
                task = Task.attack;
            }
        }
        else
        {
            task = Task.idle;
        }
    }


    protected virtual void Animate()
    {
        var speedVector = nav.velocity;
        speedVector.y = 0;
        float speed = speedVector.magnitude;
        animator.SetFloat(ANIMATOR_SPEED, speed);
        animator.SetBool(ANIMATOR_ALIVE, IsAlive);

    }

    public virtual void Attack()
    {
        Unit unit = target.GetComponent<Unit>();
        if (unit && unit.IsAlive)
        {
            animator.SetTrigger(ANIMATOR_ATTACK);
            attackTimer = attackCooldown;
        }
        else
        {
            target = null;
        }

        
    }

    public virtual void DealDamage()
    {
        if (target)
        {
            Unit unit = target.GetComponent<Unit>();
            if (unit)
            {
                unit.ReciveDamage(attackDamage, transform.position);
            }
        }
    }

    public virtual void ReciveDamage(float damage, Vector3 damageDealerPosition)
    {
        if(IsAlive) hp -= damage;

        if (!IsAlive)
        {
            healthBar.gameObject.SetActive(false);
            enabled = false;
            nav.enabled = false;
            foreach (var collider in GetComponents<Collider>())
            {
                collider.enabled = false;
            }
            if (this is ISelectable) selectableUnits.Remove(this as ISelectable);
            MouseBehaviour.mouseBehaviour.selectedUnits.Remove(this);
            Animate();
        }
    }
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}
