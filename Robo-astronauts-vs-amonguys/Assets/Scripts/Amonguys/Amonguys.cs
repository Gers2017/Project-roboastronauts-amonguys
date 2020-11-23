﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ObjectPooling;

namespace Amonguys
{
    public class Amonguys : MonoBehaviour, IDamagable, IPoolObject
    {
        [SerializeField] AmonguysStats[] amonguys_stats;
        int start_health = 60;
        int health;
        int damage_amount = 10;
        float attack_distance = 3f;
        bool is_alive = true;
        Transform target;
        int velocity, is_dead, attack;
        NavMeshAgent agent;
        Collider amonguys_collider;
        AudioSource audio_source;
        LayerMask target_layer;
        Vector3 start_scale = Vector3.one;
        [SerializeField] Animator animator;

        [Header("Audio clips")]
        [SerializeField] AudioClip[] spawn_audios;
        [SerializeField] AudioClip faint_audio;

        [Header("Meshes")]
        [SerializeField] SkinnedMeshRenderer amonguy_renderer;


        //Get gameObject components in Awake
        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            amonguys_collider = GetComponent<Collider>();
            audio_source = GetComponent<AudioSource>();
            agent.stoppingDistance = attack_distance;
        }

        void Start()
        {
            PlayerController player = FindObjectOfType<PlayerController>();

            if (player != null)
            {
                target = player.transform;
            }

            velocity = Animator.StringToHash("velocity");
            is_dead = Animator.StringToHash("is_dead");
            attack = Animator.StringToHash("attack");
            target_layer = LayerMask.GetMask("Player");
        }

        public void OnActivation()
        {
            SetUpAmonguy();
            ModifyAudioSource();
            animator.SetBool(is_dead, false);
            agent.isStopped = false;
            amonguys_collider.enabled = true;
            is_alive = true;
            transform.localScale = start_scale;
        }

        private void SetUpAmonguy()
        {
            var stats = amonguys_stats[Random.Range(0, amonguys_stats.Length)];

            start_health = stats.health;
            health = start_health;
            damage_amount = stats.damage;
            agent.speed = stats.speed;

            SetColor(stats.amonguy_color);

            InvokeRepeating("SearchTarget", 0f, stats.GetSearchTime());
            animator.SetFloat("m_speed", stats.GetAnimationSpeed());
        }

        private void ModifyAudioSource()
        {
            audio_source.pitch = Random.Range(0.7f, 1f);
            int audio_index = Random.Range(0, spawn_audios.Length);
            audio_source.PlayOneShot(spawn_audios[audio_index]);
        }

        private void SetColor(Color color)
        {
            amonguy_renderer.materials[0].SetColor("_Color", color);
        }
        
        private void SearchTarget()
        {
            if (target != null && is_alive)
            {
                agent.SetDestination(target.position);
            }
        }

        void LateUpdate()
        {
            if(is_alive)
            {
                animator.SetFloat(velocity, agent.velocity.magnitude);
                CheckDistance();
            }
            else
            {
                var scale = transform.localScale;
                
                if(scale.y > 0.1f)
                {
                    float amount = Time.deltaTime * 0.5f;
                    scale.y -= amount;
                    scale.z += amount;
                    scale.x += amount;
                    transform.localScale = scale;
                }
            }
        }

        void CheckDistance()
        {
            if(target == null) return;
            float distance = Vector3.Distance(transform.position, target.position);
            if(distance <= attack_distance)
            {
                ExecuteAttack();
                animator.SetTrigger(attack);
            }
        }

        void ExecuteAttack()
        {
            var position = transform.position + transform.forward * 2f + Vector3.up * 1f;
            var size = Vector3.one * attack_distance / 2;
            var colliders = Physics.OverlapBox(position, size, Quaternion.identity, target_layer);
            if(colliders == null) return;

            foreach (var collider in colliders)
            {
                IDamagable player;
                bool is_player = collider.TryGetComponent<IDamagable>(out player);
                if(is_player)
                {
                    player.TakeDamage(damage_amount);
                }
            }
        }

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var position = transform.position + transform.forward * 2f + Vector3.up * 1f;
            var size = Vector3.one * attack_distance / 2;
            Gizmos.color = new Color(1,0,0,0.5f);;
            Gizmos.DrawCube(position,size);
        }
        #endif
        public void TakeDamage(int amount)
        {
            if(!is_alive) return;

            health -= amount;

            //play damage sound

            if(health <= 0)
            {
                KillAmonguy();
            }
        }

        private void KillAmonguy()
        {
            //Stop the agent and disable the collider
            is_alive = false;
            agent.isStopped = true;
            amonguys_collider.enabled = false;
            CancelInvoke("SearchTarget");

            //Set the parameters to the animator
            animator.SetBool(is_dead, true);
            float faint_time = animator.GetCurrentAnimatorClipInfo(0)[0].clip.length;
            audio_source.PlayOneShot(faint_audio);
            StartCoroutine(OnAnimationEnd(faint_time + faint_audio.length * 0.5f));
        }

        IEnumerator OnAnimationEnd(float t)
        {
            yield return new WaitForSeconds(t);
            ReturnToPool();
        }
        void ReturnToPool()
        {
            gameObject.SetActive(false);
        }
    }
}