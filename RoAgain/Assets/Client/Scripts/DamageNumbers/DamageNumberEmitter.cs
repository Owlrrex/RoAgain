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

        [SerializeField]
        private AnimationCurve _sizeCurve;

        [SerializeField]
        private AnimationCurve _alphaCurve;

        private List<DamageNumberDisplay> _spawnedObjects = new();
        private List<float> _ages = new();

        // Start is called before the first frame update
        void Start()
        {
            OwlLogger.PrefabNullCheckAndLog(DamageNumberPrefab, "DamageNumberPrefab", this, GameComponent.UI);

            DamageNumberDisplay display = DamageNumberPrefab.GetComponentInChildren<DamageNumberDisplay>();
            if (display == null)
            {
                OwlLogger.LogError($"DamageNumberPrefab {DamageNumberPrefab.name} doesn't have display component!", GameComponent.UI);
                Destroy(this);
                return;
            }
        }

        private void Update()
        {
            for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (_ages[i] >= DamageNumberLifetime)
                {
                    Destroy(_spawnedObjects[i].gameObject);
                    _spawnedObjects.RemoveAt(i);
                    _ages.RemoveAt(i);
                }
                else
                {
                    _ages[i] += Time.deltaTime;
                    float normalizedAge = _ages[i] / DamageNumberLifetime;
                    float value = _sizeCurve.Evaluate(normalizedAge);
                    _spawnedObjects[i].transform.localScale = new Vector3(value, value, value);
                    _spawnedObjects[i].UpdateTextAlpha(_alphaCurve.Evaluate(normalizedAge));
                }
            }
        }

        public void DisplayDamageNumber(int damage, bool isSpDamage, bool isCrit, int chainCount, bool isLocalChar)
        {
            GameObject instance = Instantiate(DamageNumberPrefab, transform.position, Quaternion.identity);
            instance.transform.LookAt(PlayerMain.Instance.WorldUiCamera.transform, PlayerMain.Instance.WorldUiCamera.transform.up);

            DamageNumberDisplay display = instance.GetComponentInChildren<DamageNumberDisplay>();
            display.Initialize(damage, isSpDamage, isCrit, isLocalChar, chainCount);

            // TODO: Set Velocity here instead of in Initialize() so we can have texts in a chain be the same, or do other stuff

            // TODO: Display chains

            _spawnedObjects.Add(display);
            _ages.Add(0);
        }

        private void OnDisable()
        {
            //for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
            //{
            //    if (_spawnedObjects[i] != null)
            //        Destroy(_spawnedObjects[i].gameObject);
            //}

            _spawnedObjects.Clear();
            _ages.Clear();
        }
    }
}