using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Health))]
public class Combatant : MonoBehaviour
{
    // Certain aspects are drawn from the character class and copied to the Combatant file because they might need modding.
    public ICharacterClass cClass;



    // Unity Assets
    public Animator animator;
    public AudioSource audioSource;
    public AudioClip meleeHit;

    // Stats
    public string className;
    public int team = 0;

    private Health health;

    public int level = 1;
    public int experience = 0;
    public int experienceNeededForNextLevel = 100;
    public float range = 2.0f;
    public float baseDamage = 20;
    public float maxHealth = 100;

    private float attackTimer = 0.0f;
    private float attackDelay = 2.0f;
    
    // Combat
    public List<Combatant> enemies;
    public Combatant target;
    public Projectile projectile;

    // Healing
    public List<HealerEffect> healerEffects;

    private void Start()
    {
        healerEffects = new List<HealerEffect>();

        experienceNeededForNextLevel = cClass.GetExperienceToLevel(level + 1);
        range = cClass.GetRange();

        baseDamage = cClass.GetBaseDamage();
        maxHealth = cClass.GetMaximumHealth();
        attackDelay = cClass.GetTimeBetweenAttacks();

        health = GetComponent<Health>();
        health.maximumHealth = maxHealth;
        health.currentHealth = maxHealth;

        if(cClass.GetProjectile() != null)
        {
            projectile = cClass.GetProjectile();
        }
    }

    void Update()
    {
        if(!health.dead)
        {
            // Process Healer Effects
            ProcessHealerEffects();

            // Look For Target
            if (target == null || target.health.dead) target = FindNearestEnemy();

            // Move
            Move();

            // Attack
            Attack();
        }
        
    }


    // Stick this in a utility class?
    private void ProcessHealerEffects()
    {
        if(healerEffects.Count > 0)
        {
            foreach (HealerEffect effect in healerEffects)
            {
                if (effect.directHealing > 0) health.AddHealth(effect.directHealing * (effect.level * 0.5f));
                    effect.directHealingApplied = true;
                    
                if (effect.durationLeft > 0)
                {
                    health.AddHealth(effect.healingOverTime);
                    effect.durationLeft -= 1;
                }

                if (health.currentHealth > health.maximumHealth) health.currentHealth = health.maximumHealth;
                 

                if(effect.directHealingApplied && effect.durationLeft <= 0)
                {
                    effect.expire = true;
                }
            }

            for (int i = 0; i < healerEffects.Count; i++)
            {
                if (healerEffects[i].expire)
                {
                    healerEffects.RemoveAt(i);
                }
            }
        }
    }

    // Utility?
    public void AddHealerEffect(HealerEffect effect)
    {
        healerEffects.Add(effect);
    }

    // Utility?his 
    public void RemoveHealerEffect(HealerEffect effect)
    {
        if(healerEffects.Contains(effect)) healerEffects.Remove(effect);
    }

    private void Attack()
    {
        attackTimer += Time.deltaTime;

        if (target != null && Mathf.Abs(target.transform.position.x - transform.position.x) <= range)
        {
            //Debug.Log("Attacking");
            if (attackTimer > attackDelay)
            {
                if(projectile != null && projectile.ready)
                {
                    DoRangedAttack();
                }
                else
                {
                    DoMeleeAttack();
                }
                attackTimer = 0.0f;
            }
        }
    }

    private void DoRangedAttack()
    {
        StartCoroutine(RangedAttackSpawn());
    }

    IEnumerator RangedAttackSpawn()
    {
        if (animator != null)
        {
            animator.SetTrigger("Crossbow Shoot Attack");
        }

        yield return new WaitForSeconds(0.65f);

        // Instantiate projectile
        Projectile p = Instantiate(projectile, new Vector3(transform.position.x, 0.5f, transform.position.z), transform.rotation);
        // Set the target
        p.target = target.transform;
        p.baseDamage = baseDamage;
        // Rotate projectile
        Vector3 targetDirection = target.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(targetDirection);
        p.transform.rotation = Quaternion.LookRotation(targetDirection);
    }

    private void DoMeleeAttack()
    {
        StartCoroutine(MeleeCo());
    }

    IEnumerator MeleeCo()
    {
        if (animator != null)
        {
            animator.SetTrigger("Melee Right Attack 01");
        }
        yield return new WaitForSeconds(0.5f);
        
        target.GetComponent<Health>().TakeDamage(baseDamage);

        if(audioSource != null && meleeHit != null) audioSource.PlayOneShot(meleeHit);
    }

    private void Move()
    {
        if (animator != null) animator.SetBool("Walk", false);

        if (team == 0)
        {
            transform.rotation = Quaternion.LookRotation(Vector3.right);
        } else
        {
            transform.rotation = Quaternion.LookRotation(-Vector3.right);
        }
        
        if (team == 0)
        {
            // we have a target
            if(target != null)
            {
                // the target is not in range
                if (Mathf.Abs(target.transform.position.x - transform.position.x) > range)
                {
                    // the target is not behind enemy lines ;)
                    if (target.transform.position.x < 16.0f)
                    {
                        // walk toward them
                        transform.position = Vector3.MoveTowards(transform.position, new Vector3(target.transform.position.x, transform.position.y, transform.position.z), 3.0f * Time.deltaTime);

                        if (animator != null) animator.SetBool("Walk", true);
                    } 
                    else  // They are behind enemy lines
                    {
                        // run away
                        //transform.Translate(Vector3.right * -3.0f * Time.deltaTime);
                        transform.position = Vector3.MoveTowards(transform.position, new Vector3(16.0f, transform.position.y, transform.position.z), 3.0f * Time.deltaTime);
                        //if (animator != null) animator.SetBool("Walk", true);
                    }
                    
                }
            } 
            /*
            else
            {
                transform.Translate(Vector3.right * 3.0f * Time.deltaTime);
                if (animator != null) animator.SetBool("Walk", true);
            }
            */
        }
        else
        {
            if (transform.position.x > -20.0f )
            {
                if((enemies.Count == 0 || target == null) || transform.position.x > 16.0f)
                {
                    transform.Translate(Vector3.forward * 3.0f * Time.deltaTime);
                    if (animator != null) animator.SetBool("Walk", true);
                } else
                {
                    
                    if(Mathf.Abs(target.transform.position.x - transform.position.x) > range)
                    {
                        transform.Translate(Vector3.forward * 3.0f * Time.deltaTime);
                        if (animator != null) animator.SetBool("Walk", true);
                    }
                }

            }
            else
            {
                Debug.Log("Game Over.");
            }
        }

        
    }

    public void SetEnemies(List<Combatant> e)
    {
        enemies = e;
    }

    protected Combatant FindNearestEnemy()
    {
        
        Combatant nearestEnemy = null;

        if (enemies.Count == 0)
        {
            return null;
        }
        else
        {
            nearestEnemy = enemies[0];
        }

        foreach (Combatant combatant in enemies)
        {
            if (nearestEnemy == null || Mathf.Abs(combatant.transform.position.x - transform.position.x) < Mathf.Abs(nearestEnemy.transform.position.x - transform.position.x)) nearestEnemy = combatant;
        }

        return nearestEnemy;
    }
}
