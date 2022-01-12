using UnityEngine;

namespace sandbox
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        public int maxEntitiesSpawnCount = 100000;
        public int maxExistingEntitiesCount = 10000;
        public int maxSpawnRatePerFrame = 60;

        public int EntitiesCount { get; set; }
        public TMPro.TextMeshProUGUI gameStatus;
        public UnitSpawner[] spawners;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private string GameStatus()
        {
            var fps = (int)(1f / Time.unscaledDeltaTime);

            return $"FPS: {fps}\nEntities Count: {EntitiesCount}/{maxExistingEntitiesCount}";
        }

        private void Update()
        {
            gameStatus.text = GameStatus();

            if (maxEntitiesSpawnCount <= 0) { return; }

            var maxTotalSpawnCount = Mathf.Min(maxSpawnRatePerFrame, maxExistingEntitiesCount - EntitiesCount);

            foreach (var spawner in spawners)
            {
                spawner.spawnCount = Random.Range(0, maxTotalSpawnCount / spawners.Length);
                maxEntitiesSpawnCount -= spawner.spawnCount;
            }
        }
    }
}