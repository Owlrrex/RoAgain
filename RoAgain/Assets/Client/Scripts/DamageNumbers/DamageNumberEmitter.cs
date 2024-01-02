using OwlLogging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    public class DamageNumberEmitter : MonoBehaviour
    {
        [SerializeField]
        private GameObject DamageNumberPrefab;

        [SerializeField]
        private float DamageNumberLifetime;

        private List<GameObject> _spawnedObjects = new();
        private List<float> _deathTimes = new();

        // Start is called before the first frame update
        void Start()
        {
            if (DamageNumberPrefab == null)
            {
                OwlLogger.LogError($"DamageNumberEmitter can't operate without DamageNumberPrefab", GameComponent.UI);
                Destroy(this);
                return;
            }

            DamageNumberDisplay display = DamageNumberPrefab.GetComponentInChildren<DamageNumberDisplay>();
            if (display == null)
            {
                OwlLogger.LogError($"DamageNumberPrefab {DamageNumberPrefab.name} doesn't have display component!", GameComponent.UI);
                Destroy(this);
                return;
            }

            // tmp testing
            //StartCoroutine(TestDamageCoroutine());
        }

        private IEnumerator TestDamageCoroutine()
        {
            int damage = 1;
            while (true)
            {
                DisplayDamageNumber(damage);
                damage++;
                yield return new WaitForSeconds(Random.Range(0.5f, 3));
            }
        }

        private void Update()
        {
            for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (Time.time >= _deathTimes[i])
                {
                    Destroy(_spawnedObjects[i]);
                    _spawnedObjects.RemoveAt(i);
                    _deathTimes.RemoveAt(i);
                }
                else
                {
                    _spawnedObjects[i].transform.localScale *= 1 - 0.5f * Time.deltaTime;
                }
            }
        }

        // TODO: support a more advanced damage number object for things like crits, chain-hits, etc
        public void DisplayDamageNumber(int damage)
        {
            GameObject instance = Instantiate(DamageNumberPrefab, transform.position, Quaternion.identity);
            instance.transform.LookAt(Camera.main.transform);

            DamageNumberDisplay display = instance.GetComponentInChildren<DamageNumberDisplay>();
            if (damage >= 0)
                display.Initialize(damage.ToString());
            else if (damage == -1)
                display.Initialize("Miss");
            else if (damage == -2)
                display.Initialize("PDodge");
            else
                display.Initialize("Unknown");

            // TODO: Set Velocity here instead of in Initialize() so we can have texts in a chain be the same, or do other stuff

            _spawnedObjects.Add(instance);
            _deathTimes.Add(Time.time + DamageNumberLifetime);
        }

        private void OnDisable()
        {
            for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
            {
                Destroy(_spawnedObjects[i]);
            }

            _spawnedObjects.Clear();
            _deathTimes.Clear();
        }
    }
}